// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections;
using System.Threading;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Implementation is able to gradually reveal text.
    /// </summary>
    public interface IRevealableText
    {
        /// <summary>
        /// Actual text stored by the implementation.
        /// </summary>
        string Text { get; set; }
        /// <summary>
        /// Current text color.
        /// </summary>
        Color TextColor { get; set; }
        /// <summary>
        /// Object that hosts the implementation.
        /// </summary>
        GameObject GameObject { get; }
        /// <summary>
        /// Progress (in 0.0 to 1.0 range) of the <see cref="Text"/> reveal process.
        /// </summary>
        float RevealProgress { get; set; }

        /// <summary>
        /// Implementation should reveal next visible (not-a-tag) <see cref="Text"/> character.
        /// </summary>
        /// <param name="charCount">Number of chars to reveal.</param>
        /// <param name="revealDuration">Duration of the reveal.</param>
        IEnumerator RevealNextChar (int charCount, float revealDuration, CancellationToken cancellationToken);
        /// <summary>
        /// Implementation should return position (in world space) of the last revealed <see cref="Text"/> character.
        /// </summary>
        Vector2 GetLastRevealedCharPosition ();
        /// <summary>
        /// Implementation should return the last revealed <see cref="Text"/> character.
        /// </summary>
        char GetLastRevealedChar ();
    }
}
