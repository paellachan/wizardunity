// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents serializable state of a <see cref="ITextPrinterActor"/>.
    /// </summary>
    [System.Serializable]
    public class TextPrinterState : ActorState<ITextPrinterActor>
    {
        [SerializeField] private string text = default;
        [SerializeField] private string authorId = default;
        [SerializeField] private List<string> richTextTags = new List<string>();
        [SerializeField] private float revealProgress = 0f;

        public override void OverwriteFromActor (ITextPrinterActor actor)
        {
            base.OverwriteFromActor(actor);

            text = actor.Text;
            authorId = actor.AuthorId;
            richTextTags.Clear();
            richTextTags.AddRange(actor.RichTextTags);
            revealProgress = actor.RevealProgress;
        }

        public override void ApplyToActor (ITextPrinterActor actor)
        {
            base.ApplyToActor(actor);

            actor.Text = text;
            actor.AuthorId = authorId;
            actor.RichTextTags = new List<string>(richTextTags);
            actor.RevealProgress = revealProgress;
        }
    }
}
