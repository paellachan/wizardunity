// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine.UIElements;

namespace Naninovel
{
    public class CommentLineView : ScriptLineView
    {
        private readonly LineTextField valueField;

        public CommentLineView (CommentScriptLine scriptLine, VisualElement container)
            : base(scriptLine, container)
        {
            valueField = new LineTextField(CommentScriptLine.IdentifierLiteral, scriptLine.CommentText);
            Content.Add(valueField);
        }

        public override string GenerateLineText () => $"{CommentScriptLine.IdentifierLiteral} {valueField.value}";
    }
}
