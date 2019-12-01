// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Searcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityCommon;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Naninovel
{
    public class ScriptView : VisualElement
    {
        public static StyleSheet StyleSheet { get; private set; }
        public static StyleSheet CustomStyleSheet { get; private set; }
        public static bool ScriptModified { get; set; }

        public IntRange ViewRange { get; private set; }
        public int LinesCount => lines.Count;

        private const int showLoadAt = 100;
        private const string playedLineClass = "PlayedScriptLine";
        private static readonly List<SearcherItem> searchItems;
        private static Action lastGenerateDelayedAction;

        private readonly Action saveAssetAction;
        private readonly ScriptsConfiguration config;
        private readonly VisualElement linesContainer;
        private readonly Label infoLabel;
        private readonly PaginationView paginationView;
        private readonly List<ScriptLineView> lines = new List<ScriptLineView>();

        private ScriptAsset scriptAsset;
        private int page = 1;
        private int lastGeneratedTextHash = default;

        static ScriptView ()
        {
            var styleSheetPath = PathUtils.AbsoluteToAssetPath(PathUtils.Combine(PackagePath.EditorResourcesPath, "VisualEditor.uss"));
            StyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);

            var commentItem = new SearcherItem("Comment");
            var labelItem = new SearcherItem("Label");
            var genericTextItem = new SearcherItem("Generic Text");
            var defineItem = new SearcherItem("Define");
            var commandsItem = new SearcherItem("Commands");
            foreach (var commandId in Commands.Command.CommandTypes.Keys.OrderBy(k => k))
                commandsItem.AddChild(new SearcherItem(char.ToLowerInvariant(commandId[0]) + commandId.Substring(1)));
            searchItems = new List<SearcherItem> { commandsItem, genericTextItem, labelItem, commentItem, defineItem };
        }

        public ScriptView (ScriptsConfiguration config, Action drawHackGuiAction, Action saveAssetAction)
        {
            ScriptModified = false;
            this.config = config;
            this.saveAssetAction = saveAssetAction;
            CustomStyleSheet = config.CustomStyleSheet;
            ViewRange = new IntRange(0, config.VisualEditorPageLength - 1);
            styleSheets.Add(StyleSheet);
            if (CustomStyleSheet != null)
                styleSheets.Add(CustomStyleSheet);
            Add(new IMGUIContainer(drawHackGuiAction));
            Add(new IMGUIContainer(() => MonitorKeys(null)));
            RegisterCallback<KeyDownEvent>(MonitorKeys);

            linesContainer = new VisualElement();
            Add(linesContainer);

            paginationView = new PaginationView(SelectNextPage, SelectPreviousPage);
            paginationView.style.display = DisplayStyle.None;
            Add(paginationView);

            infoLabel = new Label("Loading, please wait...");
            infoLabel.name = "InfoLabel";
            Add(infoLabel);

            new ContextualMenuManipulator(ContextMenu).target = this;
        }

        public void GenerateForScript (ScriptAsset scriptAsset)
        {
            this.scriptAsset = scriptAsset;
            ScriptModified = false;

            // Prevent re-generating the editor after saving the script (applying the changes done in the editor).
            if (lastGeneratedTextHash == scriptAsset.ScriptText.GetHashCode()) return;

            lastGenerateDelayedAction = GenerateForScriptDelayed;
            EditorApplication.delayCall -= GenerateForScriptDelayed;
            EditorApplication.delayCall += GenerateForScriptDelayed;

            void GenerateForScriptDelayed () // Otherwise, it's invoked twice when entering playmode.
            {
                EditorApplication.delayCall -= GenerateForScriptDelayed;
                if (lastGenerateDelayedAction != GenerateForScriptDelayed) return;

                linesContainer.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
                infoLabel.style.display = EditorApplication.isPlayingOrWillChangePlaymode ? DisplayStyle.None : DisplayStyle.Flex;

                lines.Clear();
                linesContainer.Clear();
                var scriptLinesText = scriptAsset.ScriptText?.TrimFull()?.SplitByNewLine() ?? new[] { string.Empty };
                for (int i = 0; i < scriptLinesText.Length; i++)
                {
                    if (scriptLinesText.Length > showLoadAt && (i % showLoadAt) == 0) // Update bar for each n processed items.
                    {
                        if (scriptLinesText is null || EditorUtility.DisplayCancelableProgressBar("Generating Visual Editor", "Processing naninovel script...", i / (float)scriptLinesText.Length))
                        {
                            EditorUtility.ClearProgressBar();
                            Add(new IMGUIContainer(() => EditorGUILayout.HelpBox("Visual editor generation has been canceled.", MessageType.Error)));
                            return;
                        }
                    }
                    var scriptLineText = scriptLinesText[i].TrimFull();
                    if (string.IsNullOrEmpty(scriptLineText)) { lines.Add(null); continue; } // Skip empty lines.
                    var lineView = CreateLineView(scriptLineText, i);
                    lines.Add(lineView);
                    if (ViewRange.Contains(i))
                        linesContainer.Add(lineView);
                }
                EditorUtility.ClearProgressBar();

                if (lines.Count > config.VisualEditorPageLength)
                {
                    paginationView.style.display = DisplayStyle.Flex;
                    UpdatePaginationLabel();
                }
                else paginationView.style.display = DisplayStyle.None;

                var hotKeyInfo = config.InsertLineKey == KeyCode.None ? string.Empty : $" or {config.InsertLineKey}";
                var modifierInfo = (config.InsertLineKey == KeyCode.None || config.InsertLineModifier == EventModifiers.None) ? string.Empty : $"{config.InsertLineModifier}+";
                if (!string.IsNullOrEmpty(modifierInfo)) hotKeyInfo = hotKeyInfo.Insert(4, modifierInfo);
                infoLabel.text = $"Right-click{hotKeyInfo} to insert a new line.";

                Engine.OnInitializationFinished -= HandleEngineInitialized;
                if (Engine.IsInitialized) HandleEngineInitialized();
                else Engine.OnInitializationFinished += HandleEngineInitialized;

                if (scriptLinesText.Length > showLoadAt)
                    EditorUtility.DisplayProgressBar("Generating Visual Editor", "Building layout...", .5f);
                EditorApplication.delayCall += ClearProgressBarDelayed;
            }
        }

        public string GenerateText ()
        {
            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                if (line is null) { builder.AppendLine(); continue; }
                var lineText = line.GenerateLineText().Replace("\n", string.Empty).Replace("\r", string.Empty);
                builder.AppendLine(lineText);
            }
            var result = builder.ToString();
            lastGeneratedTextHash = result.GetHashCode();
            return result;
        }

        public ScriptLineView CreateLineView (string scriptLineText, int lineIndex, bool @default = false)
        {
            var lineType = Script.ResolveLineType(scriptLineText);
            var lineView = default(ScriptLineView);
            switch (lineType.Name)
            {
                case nameof(CommentScriptLine):
                    var commentScriptLine = new CommentScriptLine(null, lineIndex, scriptLineText, null, true);
                    lineView = new CommentLineView(commentScriptLine, linesContainer);
                    break;
                case nameof(LabelScriptLine):
                    var labelScriptLine = new LabelScriptLine(null, lineIndex, scriptLineText, null, true);
                    lineView = new LabelLineView(labelScriptLine, linesContainer);
                    break;
                case nameof(DefineScriptLine):
                    var defineScriptLine = new DefineScriptLine(null, lineIndex, scriptLineText, null, true);
                    lineView = new DefineLineView(defineScriptLine, linesContainer);
                    break;
                case nameof(CommandScriptLine):
                    var commandScriptLine = new CommandScriptLine(null, lineIndex, scriptLineText, null, true);
                    lineView = CommandLineView.CreateOrError(commandScriptLine, linesContainer, config.HideUnusedParameters, @default);
                    break;
                case nameof(GenericTextScriptLine):
                    var genericTextScriptLine = new GenericTextScriptLine(null, lineIndex, scriptLineText, null, true);
                    lineView = new GenericTextLineView(genericTextScriptLine, linesContainer);
                    break;
            }
            return lineView;
        }

        public void InsertLine (ScriptLineView lineView, int index, int? viewIndex = default)
        {
            if (ViewRange.Contains(index))
            {
                var insertViewIndex = viewIndex ?? index - ViewRange.StartIndex;
                linesContainer.Insert(insertViewIndex, lineView);
                ViewRange = new IntRange(ViewRange.StartIndex, ViewRange.EndIndex + 1);
                HandleLineReordered(lineView);
                UpdatePaginationLabel();
            }
            else
            {
                lines.Insert(index, lineView);
                SyncLineIndexes();
                ScriptModified = true;
            }
        }

        public void RemoveLine (ScriptLineView scriptLineView)
        {
            lines.Remove(scriptLineView);

            if (linesContainer.Contains(scriptLineView))
            {
                linesContainer.Remove(scriptLineView);
                ViewRange = new IntRange(ViewRange.StartIndex, ViewRange.EndIndex - 1);
                UpdatePaginationLabel();
            }

            ScriptModified = true;
        }

        public void HandleLineReordered (ScriptLineView lineView)
        {
            var viewIndex = linesContainer.IndexOf(lineView);
            var insertIndex = ViewToGlobaIndex(viewIndex);
            lines.Remove(lineView);
            lines.Insert(insertIndex, lineView);
            SyncLineIndexes();

            ScriptModified = true;
        }

        public int ViewToGlobaIndex (int viewIndex)
        {
            var curViewIndex = 0;
            var globalIndex = ViewRange.StartIndex;
            for (; globalIndex < Mathf.Min(ViewRange.EndIndex, lines.Count); globalIndex++)
            {
                if (lines[globalIndex] is null) continue; // Skip empty lines.
                if (curViewIndex >= viewIndex) break;
                curViewIndex++;
            }
            return globalIndex;
        }

        private void SyncLineIndexes ()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line != null)
                    line.LineIndex = i;
            }
        }

        private void ContextMenu (ContextualMenuPopulateEvent evt)
        {
            var worldPos = evt.mousePosition;
            var localPos = linesContainer.WorldToLocal(evt.mousePosition);
            var nearLine = linesContainer.Children().OrderBy(v => Vector2.Distance(localPos, v.layout.center)).FirstOrDefault() as ScriptLineView;
            var nearLineViewIndex = linesContainer.IndexOf(nearLine);
            var insertViewIndex = nearLine is null ? 0 : (nearLine.layout.center.y > localPos.y ? nearLineViewIndex : nearLineViewIndex + 1);
            var insertIndex = ViewToGlobaIndex(insertViewIndex);
            var hoveringLine = nearLine != null && nearLine.ContainsPoint(new Vector2(nearLine.transform.position.x, nearLine.WorldToLocal(evt.mousePosition).y));

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                evt.menu.AppendAction("Insert...", _ => ShowSearcher(worldPos, insertIndex, insertViewIndex));
                evt.menu.AppendAction("Remove", _ => { RemoveLine(nearLine); focusable = true; Focus(); focusable = false; }, hoveringLine ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
            else if (hoveringLine && (nearLine is CommandLineView || nearLine is GenericTextLineView))
            {
                var player = Engine.GetService<ScriptPlayer>();
                if (player != null && player.PlayedScript?.Name == scriptAsset.name)
                {
                    var rewindIndex = ViewToGlobaIndex(nearLineViewIndex);
                    evt.menu.AppendAction("Rewind", _ => player?.RewindAsync(rewindIndex).WrapAsync());
                }
            }
            if (hoveringLine) evt.menu.AppendAction("Help", _ => OpenHelpFor(nearLine));
        }

        private void ClearProgressBarDelayed ()
        {
            EditorApplication.delayCall -= ClearProgressBarDelayed;
            EditorUtility.ClearProgressBar();
        }

        private void SelectNextPage ()
        {
            if (ViewRange.EndIndex >= (lines.Count - 1)) return;
            page++;

            EditorUtility.DisplayProgressBar("Generating Visual Editor", "Building layout...", .5f);
            EditorApplication.delayCall += ClearProgressBarDelayed;

            ViewRange = new IntRange((page - 1) * config.VisualEditorPageLength, page * config.VisualEditorPageLength - 1);

            linesContainer.Clear();
            for (int i = ViewRange.StartIndex; i <= Mathf.Min(ViewRange.EndIndex, lines.Count - 1); i++)
            {
                var line = lines[i];
                if (line is null) continue;
                linesContainer.Add(line);
            }

            UpdatePaginationLabel();
        }

        private void SelectPreviousPage ()
        {
            if (page == 1) return;
            page--;

            EditorUtility.DisplayProgressBar("Generating Visual Editor", "Building layout...", .5f);
            EditorApplication.delayCall += ClearProgressBarDelayed;

            ViewRange = new IntRange((page - 1) * config.VisualEditorPageLength, page * config.VisualEditorPageLength - 1);

            linesContainer.Clear();
            for (int i = ViewRange.StartIndex; i <= Mathf.Min(ViewRange.EndIndex, lines.Count - 1); i++)
            {
                var line = lines[i];
                if (line is null) continue;
                linesContainer.Add(line);
            }

            UpdatePaginationLabel();
        }

        private void UpdatePaginationLabel ()
        {
            paginationView?.SetLabel($" {ViewRange.StartIndex + 1}-{Mathf.Min(lines.Count, ViewRange.EndIndex + 1)} / {lines.Count} ");
        }

        private void OpenHelpFor (ScriptLineView line)
        {
            var url = @"https://naninovel.com/";
            switch (line)
            {
                case CommentLineView _: url += "guide/naninovel-scripts.html#comment-lines"; break;
                case LabelLineView _: url += "guide/naninovel-scripts.html#label-lines"; break;
                case GenericTextLineView _: url += "guide/naninovel-scripts.html#generic-text-lines"; break;
                case DefineLineView _: url += "guide/naninovel-scripts.html#define-lines"; break;
                case CommandLineView commandLine: url += $"api/#{commandLine.CommandId.ToLowerInvariant()}"; break;
                case ErrorLineView errorLine: url += $"api/#{errorLine.CommandId}"; break;
                default: url += "guide/naninovel-scripts.html"; break;
            }
            Application.OpenURL(url);
        }

        private void ShowSearcher (Vector2 position, int insertIndex, int insertViewIndex)
        {
            SearcherWindow.Show(EditorWindow.focusedWindow, searchItems, "Insert Line", item => {
                if (item is null) return true; // Prevent nullref when focus is lost before item is selected.
                var lineText = string.Empty;
                var lineView = default(ScriptLineView);
                switch (item.Name)
                {
                    case "Commands": return false; // Do nothing.
                    case "Comment": lineText = CommentScriptLine.IdentifierLiteral; break;
                    case "Label": lineText = LabelScriptLine.IdentifierLiteral; break;
                    case "Generic Text":
                        var genericTextScriptLine = new GenericTextScriptLine(null, -1, string.Empty, null, true);
                        lineView = new GenericTextLineView(genericTextScriptLine, linesContainer);
                        break;
                    case "Define": lineText = DefineScriptLine.IdentifierLiteral; break;
                    default: lineText = CommandScriptLine.IdentifierLiteral + item.Name; break;
                }
                if (lineView is null) lineView = CreateLineView(lineText, -1, true);
                InsertLine(lineView, insertIndex, insertViewIndex);
                ScriptLineView.SetFocused(lineView);
                lineView.Q<TextField>().Q<VisualElement>(TextInputBaseField<string>.textInputUssName).Focus();
                return true;
            }, position);
        }

        private void MonitorKeys (KeyDownEvent evt)
        {
            void ShowResearcher ()
            {
                var insertViewIndex = linesContainer.childCount;
                var insertIndex = ViewToGlobaIndex(insertViewIndex);
                ShowSearcher(Event.current.mousePosition, insertIndex, insertViewIndex);
            }

            if (evt != null)
            {
                if (evt.keyCode == config.InsertLineKey && (evt.modifiers & config.InsertLineModifier) != 0) ShowResearcher();
                else if (evt.keyCode == config.SaveScriptKey && (evt.modifiers & config.SaveScriptModifier) != 0) saveAssetAction?.Invoke();
                evt.StopImmediatePropagation();
            }
            else if (Event.current != null && Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == config.InsertLineKey && Event.current.modifiers == config.InsertLineModifier) ShowResearcher();
                else if (Event.current.keyCode == config.SaveScriptKey && Event.current.modifiers == config.SaveScriptModifier) saveAssetAction?.Invoke();
            }
        }

        private void HandleEngineInitialized ()
        {
            if (!(Engine.Behaviour is RuntimeBehaviour)) return;

            var player = Engine.GetService<ScriptPlayer>();
            player.OnCommandExecutionStart += HighlightPlayedCommand;
            var stateManager = Engine.GetService<StateManager>();
            stateManager.OnRollbackFinished += () => HighlightPlayedCommand(player.PlayedCommand);
            if (player.PlayedCommand != null)
                HighlightPlayedCommand(player.PlayedCommand);
        }

        private void HighlightPlayedCommand (Commands.Command command)
        {
            if (!ObjectUtils.IsValid(scriptAsset) || command is null || command.ScriptName != scriptAsset.name || !lines.IsIndexValid(command.LineIndex) || !ViewRange.Contains(command.LineIndex))
                return;

            var prevPlayedLine = linesContainer.Q<ScriptLineView>(className: playedLineClass);
            prevPlayedLine?.RemoveFromClassList(playedLineClass);

            var playedLine = lines[command.LineIndex];
            playedLine.AddToClassList(playedLineClass);
        }
    }
}
