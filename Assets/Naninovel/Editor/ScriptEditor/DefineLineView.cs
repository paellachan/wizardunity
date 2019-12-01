// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine.UIElements;

namespace Naninovel
{
    public class DefineLineView : ScriptLineView
    {
        private readonly LineTextField keyField;
        private readonly LineTextField valueField;

        public DefineLineView (DefineScriptLine scriptLine, VisualElement container)
            : base(scriptLine, container)
        {
            valueField = new LineTextField("Define", scriptLine.DefineValue);
            keyField = new LineTextField("as", scriptLine.DefineKey);
            Content.Add(valueField);
            Content.Add(keyField);
        }

        public override string GenerateLineText () => $"{DefineScriptLine.IdentifierLiteral}{keyField.value} {valueField.value}";
    }
}
