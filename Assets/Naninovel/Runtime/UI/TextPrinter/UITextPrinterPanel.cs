// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Used by <see cref="UITextPrinter"/> to control the printed text.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UITextPrinterPanel : ScriptableUIBehaviour, IManagedUI
    {
        /// <summary>
        /// Contents of the printer to be used for transformations.
        /// </summary>
        public RectTransform Content => content;
        /// <summary>
        /// The text to be printed inside the printer panel. 
        /// Note that the visibility of the text is controlled independently.
        /// </summary>
        public abstract string PrintedText { get; set; }
        /// <summary>
        /// Text representing name of the author of the currently printed text.
        /// </summary>
        public abstract string ActorNameText { get; set; }
        /// <summary>
        /// Which part of the assigned text message is currently revealed, in 0.0 to 1.0 range.
        /// </summary>
        public abstract float RevealProgress { get; set; }
        /// <summary>
        /// Current appearance of the printer.
        /// </summary>
        public abstract string Apperance { get; set; }
        /// <summary>
        /// Object that should trigger continue input when interacted with.
        /// </summary>
        public GameObject ContinueInputTrigger => continueInputTrigger;

        protected RectTransform PrinterContent => content;
        protected CharacterManager CharacterManager { get; private set; }

        [Tooltip("Transform used for printer position, scale and rotation external manipulations.")]
        [SerializeField] private RectTransform content = default;
        [Tooltip("Object that should trigger continue input when interacted with. Make sure the object is a raycast target and is not blocked by other raycast target objects.")]
        [SerializeField] private GameObject continueInputTrigger = default;

        private InputManager inputManager;
        private ScriptPlayer scriptPlayer;

        public virtual Task InitializeAsync ()
        {
            inputManager?.Continue?.AddObjectTrigger(ContinueInputTrigger);
            scriptPlayer.OnWaitingForInput += SetWaitForInputIndicatorVisible;
            return Task.CompletedTask;
        }

        /// <summary>
        /// A coroutine to reveal the <see cref="PrintedText"/> char by char over time.
        /// </summary>
        /// <param name="cancellationToken">The coroutine will break when cancellation of the provided token is requested.</param>
        /// <param name="revealDelay">Delay (in seconds) between revealing consequent characters.</param>
        /// <returns>Coroutine enumerator.</returns>
        public abstract IEnumerator RevealPrintedTextOverTime (CancellationToken cancellationToken, float revealDelay);
        /// <summary>
        /// Controls visibility of the wait for input indicator.
        /// </summary>
        public abstract void SetWaitForInputIndicatorVisible (bool isVisible);
        /// <summary>
        /// Invoked by <see cref="UITextPrinter"/> when author meta of the printed text changes.
        /// </summary>
        /// <param name="authorId">Acotr ID of the new author.</param>
        /// <param name="authorMeta">Metadata of the new author.</param>
        public abstract void OnAuthorChanged (string authorId, CharacterMetadata authorMeta);

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(content, continueInputTrigger);

            inputManager = Engine.GetService<InputManager>();
            scriptPlayer = Engine.GetService<ScriptPlayer>();

            CharacterManager = Engine.GetService<CharacterManager>();
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();

            inputManager?.Continue?.RemoveObjectTrigger(ContinueInputTrigger);
            if (scriptPlayer != null)
                scriptPlayer.OnWaitingForInput -= SetWaitForInputIndicatorVisible;
        }
    } 
}
