// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine.UIElements;

namespace Naninovel
{
    public class GenericTextLineView : ScriptLineView
    {
        private readonly LineTextField valueField;

        public GenericTextLineView (GenericTextScriptLine scriptLine, VisualElement container)
            : base(scriptLine, container)
        {
            valueField = new LineTextField(value: scriptLine.Text);
            Content.Add(valueField);
        }

        public override string GenerateLineText () => valueField.value;
    }
}
