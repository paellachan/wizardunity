// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine.UIElements;

namespace Naninovel
{
    public class ErrorLineView : ScriptLineView
    {
        public string CommandId { get; private set; }

        private readonly LineTextField valueField;

        public ErrorLineView (CommandScriptLine scriptLine, VisualElement container, string error = default)
            : base(scriptLine, container)
        {
            CommandId = scriptLine.CommandName.ToLowerInvariant();
            valueField = new LineTextField(value: scriptLine.Text);
            Content.Add(valueField);
            if (error != default)
                tooltip = "Error: " + error;
        }

        public override string GenerateLineText () => valueField.value;
    }
}
