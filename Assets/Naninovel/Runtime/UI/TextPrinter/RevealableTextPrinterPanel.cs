// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <summary>
    /// A <see cref="UITextPrinterPanel"/> implementation that uses <see cref="IRevealableText"/> to reveal text over time.
    /// </summary>
    /// <remarks>
    /// A <see cref="IRevealableText"/> component should be attached to the underlying gameobject or one of it's child objects.
    /// </remarks>
    public class RevealableTextPrinterPanel : UITextPrinterPanel
    {
        [System.Serializable]
        private class AuthorChangedEvent : UnityEvent<string> { }

        [System.Serializable]
        private class CharsToSfx
        {
            [Tooltip("The characters for which to trigger the SFX.")]
            public string Characters = default;
            [ResourcesPopup(AudioConfiguration.DefaultAudioPathPrefix, AudioConfiguration.DefaultAudioPathPrefix, "None (disabled)")]
            [Tooltip("The name (local path) of the SFX to trigger for the specified characters.")]
            public string SfxName = default;
        }

        [System.Serializable]
        private class CharsToCommand
        {
            [Tooltip("The characters for which to trigger the command.")]
            public string Characters = default;
            [Tooltip("The text of the script command to execute for the specified characters.")]
            public string CommandText = default;
            public Commands.Command Command { get; set; }
        }

        public override string PrintedText { get => RevealableText.Text; set => RevealableText.Text = value; }
        public override string ActorNameText { get => actorNamePanel ? actorNamePanel.Text : null; set => SetActorNameText(value); }
        public override float RevealProgress { get => RevealableText.RevealProgress; set => SetRevealProgress(value); }
        public override string Apperance { get => GetActiveAppearance(); set => SetActiveAppearance(value); }
        public IRevealableText RevealableText { get; private set; }

        protected string AuthorId { get; private set; }
        protected CharacterMetadata AuthorMeta { get; private set; }

        protected const string DefaultAppearanceName = "Default";

        [SerializeField] private ActorNamePanel actorNamePanel = default;
        [SerializeField] private RawImage actorAvatarImage = default;
        [FormerlySerializedAs("inputIndicatorPrefab"), Tooltip("Object to use as an indicator when player is supposed to activate a `Continue` input to progress further. Will instatiate a clone when an external prefab is assigned.")]
        [SerializeField] private WaitingForInputIndicator inputIndicator = default;
        [Tooltip("Whether to automatically move input indicator so it appears after the last revealed text character.")]
        [SerializeField] private bool positionIndicatorOverText = true;
        [Tooltip("Assigned canvas groups will represent printer appearances. Game object name of the canvas group represents the appearance name. Alpha of the group will be set to 1 when the appearance is activated and vice-versa.")]
        [SerializeField] private List<CanvasGroup> appearances = default;
        [ResourcesPopup(AudioConfiguration.DefaultAudioPathPrefix, AudioConfiguration.DefaultAudioPathPrefix, "None (disabled)")]
        [Tooltip ("If specified, SFX with the provided name (local path) will be played whenever a character is revealed. Can be overrided in the characters metadata to play character-specific SFXs.")]
        [SerializeField] private string RevealSfx = default;
        [Tooltip("Allows binding an SFX to play when specific characters are revealed.")]
        [SerializeField] private List<CharsToSfx> charsSfx = new List<CharsToSfx>();
        [Tooltip("Allows binding a script command to execute when specific characters are revealed.")]
        [SerializeField] private List<CharsToCommand> charsCommands = new List<CharsToCommand>();
        [Tooltip("Invoked when author (character ID) of the currently printed text is changed.")]
        [SerializeField] private AuthorChangedEvent onAuthorChanged = default;

        private static WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        private Color defaultMessageColor, defaultNameColor;
        private WaitingForInputIndicator inputIndicatorRef;
        private AudioManager audioManager;

        public override async Task InitializeAsync ()
        {
            await base.InitializeAsync();

            if (!string.IsNullOrEmpty(RevealSfx))
                await audioManager.HoldAudioResourcesAsync(this, RevealSfx);
            if (charsSfx != null && charsSfx.Count > 0)
            {
                var loadTasks = new List<Task>();
                foreach (var charSfx in charsSfx)
                    if (!string.IsNullOrEmpty(charSfx.SfxName))
                        loadTasks.Add(audioManager.HoldAudioResourcesAsync(this, charSfx.SfxName));
                await Task.WhenAll(loadTasks);
            }

            if (charsCommands != null && charsCommands.Count > 0)
            {
                foreach (var charsCommand in charsCommands)
                {
                    if (string.IsNullOrEmpty(charsCommand.CommandText)) continue;
                    var scriptLine = new CommandScriptLine(string.Empty, 0, charsCommand.CommandText, null, false);
                    if (scriptLine is null) continue;
                    var command = Commands.Command.FromScriptLine(scriptLine);
                    if (command is null) continue;
                    charsCommand.Command = command;
                }
            }
        }

        public override IEnumerator RevealPrintedTextOverTime (CancellationToken cancellationToken, float revealDelay)
        {
            if (revealDelay <= 0) { RevealableText.RevealProgress = 1f; yield break; }

            var lastRevealTime = Time.time;
            while (RevealableText.RevealProgress < 1)
            {
                var timeSinceLastReveal = Time.time - lastRevealTime;
                var charsToReveal = Mathf.FloorToInt(timeSinceLastReveal / revealDelay);
                if (charsToReveal > 0)
                {
                    lastRevealTime = Time.time; 
                    yield return RevealableText.RevealNextChar(charsToReveal, revealDelay, cancellationToken);
                    if (cancellationToken.IsCancellationRequested) break;

                    var lastRevealedChar = RevealableText.GetLastRevealedChar();
                    PlayRevealSfxForChar(lastRevealedChar);
                    if (charsCommands != null && charsCommands.Count > 0 && !cancellationToken.IsCancellationRequested)
                    {
                        var execStartTime = Time.time;
                        yield return ExecuteCommandForCharRoutine(lastRevealedChar, cancellationToken);
                        lastRevealTime += Time.time - execStartTime; // Prevent command execution time from affecting the reveal routine.
                    }
                    if (cancellationToken.IsCancellationRequested) break;
                }

                yield return waitForEndOfFrame;
                if (cancellationToken.IsCancellationRequested) break;
            }
        }

        public override void SetWaitForInputIndicatorVisible (bool isVisible)
        {
            if (isVisible)
            {
                inputIndicatorRef.Show();
                if (positionIndicatorOverText)
                    inputIndicatorRef.RectTransform.position = RevealableText.GetLastRevealedCharPosition();
            }
            else inputIndicatorRef.Hide();
        }

        public override void OnAuthorChanged (string authorId, CharacterMetadata authorMeta)
        {
            AuthorId = authorId;
            AuthorMeta = authorMeta;

            // Attempt to apply character-specific message text color.
            RevealableText.TextColor = authorMeta.UseCharacterColor ? authorMeta.MessageColor : defaultMessageColor;

            // Attempt to set character name color.
            if (actorNamePanel)
            {
                actorNamePanel.TextColor = authorMeta.UseCharacterColor ? authorMeta.NameColor : defaultNameColor;
            }

            // Attempt to set character-specific avatar texture.
            if (actorAvatarImage)
            {
                var avatarTexture = CharacterManager.GetAvatarTextureFor(authorId);
                actorAvatarImage.gameObject.SetActive(avatarTexture);
                actorAvatarImage.texture = avatarTexture;
            }

            onAuthorChanged?.Invoke(authorId);
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(inputIndicator);

            RevealableText = GetComponentInChildren<IRevealableText>();
            Debug.Assert(RevealableText != null, $"IRevealableText component not found on {gameObject.name} or it's descendants.");

            defaultMessageColor = RevealableText.TextColor;
            defaultNameColor = actorNamePanel ? actorNamePanel.TextColor : default;

            if (inputIndicator.transform.IsChildOf(transform))
                inputIndicatorRef = inputIndicator;
            else
            {
                inputIndicatorRef = Instantiate(inputIndicator);
                inputIndicatorRef.RectTransform.SetParent(RevealableText.GameObject.transform, false);
            }

            audioManager = Engine.GetService<AudioManager>();

            SetActorNameText(null); // Reset the name-related stuff.
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            CharacterManager.OnCharacterAvatarChanged += HandleAvatarChanged;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            CharacterManager.OnCharacterAvatarChanged -= HandleAvatarChanged;
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();

            if (!string.IsNullOrEmpty(RevealSfx))
                audioManager?.ReleaseAudioResources(this, RevealSfx);
            if (charsSfx != null && charsSfx.Count > 0)
            {
                foreach (var charSfx in charsSfx)
                    if (!string.IsNullOrEmpty(charSfx.SfxName))
                        audioManager?.ReleaseAudioResources(this, charSfx.SfxName);
            }
        }

        protected virtual string GetActiveAppearance ()
        {
            if (appearances is null || appearances.Count == 0)
                return DefaultAppearanceName;
            foreach (var grp in appearances)
                if (grp.alpha == 1f) return grp.gameObject.name;
            return DefaultAppearanceName;
        }

        protected virtual void SetActiveAppearance (string appearance)
        {
            if (appearances is null || appearances.Count == 0 || !appearances.Any(g => g.gameObject.name == appearance))
                return;

            foreach (var grp in appearances)
                grp.alpha = grp.gameObject.name == appearance ? 1 : 0;
        }

        protected virtual async void SetRevealProgress (float value)
        {
            RevealableText.RevealProgress = value;

            // A hack to wait for Unity's UI text to rebuilt; otherwise the input indicator will get a wrong position.
            // TMPro printers are not affected by this.
            await waitForEndOfFrame;

            if (inputIndicatorRef.IsVisible) // Update indicator position.
                SetWaitForInputIndicatorVisible(true);
        }

        protected virtual void SetActorNameText (string text)
        {
            if (!actorNamePanel) return;

            var isActive = !string.IsNullOrWhiteSpace(text);
            actorNamePanel.gameObject.SetActive(isActive);
            if (!isActive) return;

            actorNamePanel.Text = text;
        }

        protected virtual void HandleAvatarChanged (CharacterAvatarChangedArgs args)
        {
            if (!actorAvatarImage || args.CharacterId != AuthorId) return;

            actorAvatarImage.gameObject.SetActive(args.AvatarTexture);
            actorAvatarImage.texture = args.AvatarTexture;
        }

        protected virtual void PlayRevealSfxForChar (char character)
        {
            if (charsSfx != null && charsSfx.Count > 0)
            {
                foreach (var charSfx in charsSfx)
                {
                    var index = charSfx.Characters.IndexOf(character);
                    if (index < 0) continue;

                    if (!string.IsNullOrEmpty(charSfx.SfxName))
                        audioManager.PlaySfxFast(charSfx.SfxName);
                    return;
                }
            }

            if (AuthorMeta != null && !string.IsNullOrEmpty(AuthorMeta.MessageSound))
                audioManager.PlaySfxFast(AuthorMeta.MessageSound);
            else if (!string.IsNullOrEmpty(RevealSfx))
                audioManager.PlaySfxFast(RevealSfx);
        }

        protected virtual IEnumerator ExecuteCommandForCharRoutine (char character, CancellationToken cancellationToken)
        {
            if (charsCommands is null || charsCommands.Count == 0) yield break;

            foreach (var charsCommand in charsCommands)
            {
                var index = charsCommand.Characters.IndexOf(character);
                if (index < 0) continue;

                if (charsCommand.Command != null && charsCommand.Command.ShouldExecute)
                {
                    var task = charsCommand.Command.ExecuteAsync(cancellationToken);
                    while (Application.isPlaying && !task.IsCompleted && !cancellationToken.IsCancellationRequested)
                        yield return waitForEndOfFrame;
                }
                yield break;
            }
        }
    }
}
