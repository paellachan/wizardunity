// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine.UIElements;

namespace Naninovel
{
    public class LabelLineView : ScriptLineView
    {
        private readonly LineTextField valueField;

        public LabelLineView (LabelScriptLine scriptLine, VisualElement container)
            : base(scriptLine, container)
        {
            valueField = new LineTextField(LabelScriptLine.IdentifierLiteral, scriptLine.LabelText);
            Content.Add(valueField);
        }

        public override string GenerateLineText () => $"{LabelScriptLine.IdentifierLiteral} {valueField.value}";
    }
}
