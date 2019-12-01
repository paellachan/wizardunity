// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityCommon;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.UIElements;

namespace Naninovel
{
    [CustomEditor(typeof(ScriptImporter))]
    public class ScriptImporterEditor : ScriptedImporterEditor
    {
        public override bool showImportedObject => false;

        private const int previewLengthLimit = 5000;

        private static readonly MethodInfo drawHeaderMethod;
        private static ScriptsConfiguration config;

        private ScriptAsset scriptAsset;
        private ScriptView visualEditor;
        private string previewContent;
        private float labelsHeight, gotosHeight;
        private GUIContent[] labelTags;
        private GUIContent[] gotoTags;

        static ScriptImporterEditor ()
        {
            drawHeaderMethod = typeof(Editor).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                ?.Where(m => m.Name == "DrawHeaderGUI" && m.GetParameters().Length == 2)?.FirstOrDefault();
        }

        public override void OnEnable ()
        {
            base.OnEnable();

            if (config is null)
                config = Configuration.LoadOrDefault<ScriptsConfiguration>();
            scriptAsset = assetTarget as ScriptAsset;

            if (config.EnableVisualEditor)
            {
                visualEditor = new ScriptView(config, ApplyRevertHackGUI, ApplyAndImport);
                visualEditor.GenerateForScript(scriptAsset);

                ScriptImporter.OnModified += visualEditor.GenerateForScript;
                return;
            }

            previewContent = scriptAsset.ScriptText;
            if (previewContent.Length > previewLengthLimit)
            {
                previewContent = previewContent.Substring(0, previewLengthLimit);
                previewContent += $"{System.Environment.NewLine}<...>";
            }

            var script = new Script(scriptAsset.name, scriptAsset.ScriptText, ignoreErrors: true);
            labelTags = script.LabelLines.Select(l => new GUIContent($"# {l.LabelText}")).ToArray();
            gotoTags = script.CommandLines
                .Where(c => c.CommandName.EqualsFastIgnoreCase("goto") && c.CommandParameters.TryGetValue(string.Empty, out var path) && !path.StartsWithFast("."))
                .Select(c => new GUIContent($"@goto {c.CommandParameters[string.Empty]}")).ToArray();
        }

        public override void OnDisable ()
        {
            base.OnDisable();

            if (visualEditor != null)
                ScriptImporter.OnModified -= visualEditor.GenerateForScript;
            scriptAsset = null;
        }

        public override VisualElement CreateInspectorGUI () => visualEditor;

        public override bool HasModified () => ScriptView.ScriptModified;

        protected override void Apply ()
        {
            base.Apply();

            if (visualEditor is null) return;

            var scriptText = visualEditor.GenerateText();
            var scriptPath = AssetDatabase.GetAssetPath(scriptAsset);
            File.WriteAllText(scriptPath, scriptText, Encoding.UTF8);
            ScriptView.ScriptModified = false;
        }

        public override void OnInspectorGUI ()
        {
            ApplyRevertHackGUI();

            var editorWasEnabled = GUI.enabled;
            GUI.enabled = true;

            if (labelTags != null && labelTags.Length > 0)
                DrawTags(labelTags, GUIStyles.ScriptLabelTag, ref labelsHeight);
            if (gotoTags != null && gotoTags.Length > 0)
                DrawTags(gotoTags, GUIStyles.ScriptGotoTag, ref gotosHeight);

            GUI.enabled = false;
            EditorGUILayout.LabelField(previewContent, EditorStyles.wordWrappedMiniLabel);

            GUI.enabled = editorWasEnabled;
        }

        public void ApplyRevertHackGUI ()
        {
            GUILayout.Space(-150); // Hide the apply-revert buttons.
            ApplyRevertGUI(); // Required to prevent errors in the editor.
            GUILayout.Space(125); // Compensate the above space hack.
        }

        protected override void OnHeaderGUI ()
        {
            if (scriptAsset != null && drawHeaderMethod != null)
                drawHeaderMethod.Invoke(null, new object[] { this, scriptAsset.name + (ScriptView.ScriptModified ? "*" : string.Empty) });
            else base.OnHeaderGUI();
        }

        private void DrawTags (GUIContent[] tags, GUIStyle style, ref float heightBuffer)
        {
            const float horPadding = 5f;
            const float verPadding = 5f;

            if (Event.current.type == EventType.Repaint)
                heightBuffer = 0;

            GUILayout.Space(verPadding);

            // Create a rect to test how wide the label list can be.
            var widthProbeRect = GUILayoutUtility.GetRect(0, 10240, 0, 0);
            var rect = EditorGUILayout.GetControlRect();
            var controlHorPadding = rect.x;
            for (int i = 0; i < tags.Length; i++)
            {
                var content = tags[i];
                var labelSize = style.CalcSize(content);

                if (Event.current.type == EventType.Repaint && (rect.x + labelSize.x) >= widthProbeRect.xMax)
                {
                    var addHeight = GUIStyles.TagIcon.fixedHeight + verPadding;
                    rect.y += GUIStyles.TagIcon.fixedHeight + verPadding;
                    rect.x = controlHorPadding;
                    heightBuffer += addHeight;
                }

                var labelRect = new Rect(rect.x, rect.y, labelSize.x, labelSize.y);
                GUI.Label(labelRect, content, style);

                rect.x += labelSize.x + horPadding;
            }

            GUILayoutUtility.GetRect(0, heightBuffer);
        }
    }
}
