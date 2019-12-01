// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System.Collections.Generic;
using System.Linq;
using UnityCommon;
using UnityEngine;
using UnityEngine.UIElements;

namespace Naninovel
{
    public class CommandLineView : ScriptLineView
    {
        public string CommandId { get; private set; }

        private readonly List<LineTextField> parameterFields = new List<LineTextField>();

        private bool hideParameters;

        private CommandLineView (CommandScriptLine scriptLine, VisualElement container)
            : base(scriptLine, container) { }

        public static ScriptLineView CreateOrError (CommandScriptLine scriptLine, VisualElement container, bool hideParameters, bool @default = false)
        {
            ErrorLineView Error (string error) => new ErrorLineView(scriptLine, container, error);

            if (!scriptLine.Valid)
                return Error("Incorrect syntax.");

            var commandType = Command.FindCommandType(scriptLine.CommandName);
            if (commandType is null) return Error($"Unknown command `{scriptLine.CommandName}`.");

            var commandLineView = new CommandLineView(scriptLine, container);
            commandLineView.hideParameters = hideParameters;
            commandLineView.CommandId = scriptLine.CommandName;
            var nameLabel = new Label(scriptLine.CommandName);
            nameLabel.name = "InputLabel";
            nameLabel.AddToClassList("Inlined");
            commandLineView.Content.Add(nameLabel);

            var paramaterFieldInfos = commandType.GetProperties()
                .Where(property => property.IsDefined(typeof(Command.CommandParameterAttribute), false)).ToList();
            var parameterAttributes = paramaterFieldInfos
                .Select(f => f.GetCustomAttributes(typeof(Command.CommandParameterAttribute), false).First() as Command.CommandParameterAttribute).ToList();
            Debug.Assert(paramaterFieldInfos.Count == parameterAttributes.Count);

            for (int i = 0; i < paramaterFieldInfos.Count; i++)
            {
                var paramFieldInfo = paramaterFieldInfos[i];
                var paramAttribute = parameterAttributes[i];

                var paramName = paramAttribute.Alias != null && scriptLine.CommandParameters.ContainsKey(paramAttribute.Alias) ? paramAttribute.Alias : paramFieldInfo.Name;
                if (!scriptLine.CommandParameters.ContainsKey(paramName) && !paramAttribute.Optional && !@default)
                    return Error($"Missing `{paramName}` parameter.");

                scriptLine.CommandParameters.TryGetValue(paramName, out var paramValue);
                var textField = new LineTextField(paramAttribute.Alias ?? char.ToLowerInvariant(paramName[0]) + paramName.Substring(1), paramValue);
                // Show parameter ID of the nameless parameters via tooltip.
                if (string.IsNullOrEmpty(textField.label))
                    textField.tooltip = paramFieldInfo.Name;
                else textField.AddToClassList("NamedParameterLabel");
                commandLineView.parameterFields.Add(textField);
                // Show the un-assigned named parameters only when hovered or focused.
                if (string.IsNullOrEmpty(textField.label) || !hideParameters || !string.IsNullOrEmpty(textField.value)) 
                    commandLineView.Content.Add(textField);
            }

            foreach (var paramId in scriptLine.CommandParameters.Keys)
            {
                if (parameterAttributes.Exists(a => a.Alias?.EqualsFastIgnoreCase(paramId) ?? false)) continue;
                if (paramaterFieldInfos.Exists(f => f.Name.EqualsFastIgnoreCase(paramId))) continue;
                return Error($"Unsupported `{paramId}` parameter.");
            }

            return commandLineView;
        }

        public override string GenerateLineText ()
        {
            var result = $"{CommandScriptLine.IdentifierLiteral}{CommandId}";
            var namelessParamField = parameterFields.FirstOrDefault(f => string.IsNullOrEmpty(f.label));
            if (namelessParamField != null)
                result += $" {HandleQuotes(namelessParamField.value)}";

            foreach (var field in parameterFields)
                if (!string.IsNullOrEmpty(field.label) && !string.IsNullOrWhiteSpace(field.value))
                    result += $" {field.label}:{HandleQuotes(field.value)}";
            return result;
        }

        protected override void ApplyFocusedStyle ()
        {
            base.ApplyFocusedStyle();

            if (DragManipulator.Active) return;
            ShowUnAssignedNamedFields();
        }

        protected override void ApplyNotFocusedStyle ()
        {
            base.ApplyNotFocusedStyle();

            HideUnAssignedNamedFields();
        }

        protected override void ApplyHoveredStyle ()
        {
            base.ApplyHoveredStyle();

            if (DragManipulator.Active) return;
            ShowUnAssignedNamedFields();
        }

        protected override void ApplyNotHoveredStyle ()
        {
            base.ApplyNotHoveredStyle();

            if (FocusedLine == this) return;
            HideUnAssignedNamedFields();
        }

        private void ShowUnAssignedNamedFields ()
        {
            if (!hideParameters) return;

            foreach (var field in parameterFields)
                if (!Content.Contains(field))
                    Content.Add(field);
        }

        private void HideUnAssignedNamedFields ()
        {
            if (!hideParameters) return;

            foreach (var field in parameterFields)
                if (!string.IsNullOrEmpty(field.label) && string.IsNullOrWhiteSpace(field.value) && Content.Contains(field))
                    Content.Remove(field);
        }

        private string HandleQuotes (string value)
        {
            // We're doing the reverse in command script line parsing. 
            if (value.Contains(" ")) return $"\"{value.Replace("\"", "\\\"")}\"";
            return value;
        }
    }
}
