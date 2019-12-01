// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages text printer actors.
    /// </summary>
    [InitializeAtRuntime]
    public class TextPrinterManager : OrthoActorManager<ITextPrinterActor, TextPrinterState, TextPrinterMetadata, TextPrintersConfiguration>, IStatefulService<SettingsStateMap>
    {
        [System.Serializable]
        private class Settings
        {
            public float BaseRevealSpeed = .5f;
        }

        [System.Serializable]
        private class GameState
        {
            public string DefaultPrinterId = null;
        }

        /// <summary>
        /// Invoked when a print text operation (<see cref="PrintTextAsync(string, string, string, float, CancellationToken)"/>) is started.
        /// </summary>
        public event Action<PrintTextArgs> OnPrintTextStarted;
        /// <summary>
        /// Invoked when a print text operation (<see cref="PrintTextAsync(string, string, string, float, CancellationToken)"/>) is finished.
        /// </summary>
        public event Action<PrintTextArgs> OnPrintTextFinished;

        /// <summary>
        /// ID of the printer actor to use by default when a specific one is not specified.
        /// </summary>
        public string DefaultPrinterId { get; set; }
        /// <summary>
        /// Base speed for revealing text messages as per the game settings, in 0 to 1 range.
        /// </summary>
        public float BaseRevealSpeed { get; set; }

        private readonly WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        private readonly ScriptPlayer scriptPlayer;

        public TextPrinterManager (TextPrintersConfiguration config, CameraManager cameraManager, ScriptPlayer scriptPlayer)
            : base(config, cameraManager)
        {
            this.scriptPlayer = scriptPlayer;
            DefaultPrinterId = config.DefaultPrinterId;
        }

        public override void ResetService ()
        {
            base.ResetService();
            DefaultPrinterId = Configuration.DefaultPrinterId;
        }

        public Task SaveServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                BaseRevealSpeed = BaseRevealSpeed
            };
            stateMap.SetState(settings);
            return Task.CompletedTask;
        }

        public Task LoadServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>() ?? new Settings();
            BaseRevealSpeed = settings.BaseRevealSpeed;
            return Task.CompletedTask;
        }


        public override async Task SaveServiceStateAsync (GameStateMap stateMap)
        {
            await base.SaveServiceStateAsync(stateMap);

            var gameState = new GameState() {
                DefaultPrinterId = DefaultPrinterId ?? Configuration.DefaultPrinterId
            };
            stateMap.SetState(gameState);
        }

        public override async Task LoadServiceStateAsync (GameStateMap stateMap)
        {
            await base.LoadServiceStateAsync(stateMap);

            var state = stateMap.GetState<GameState>() ?? new GameState();
            DefaultPrinterId = state.DefaultPrinterId ?? Configuration.DefaultPrinterId;
        }

        /// <summary>
        /// Prints (reveals) provided text message over time using a managed text printer with the provided ID.
        /// </summary>
        /// <param name="printerId">ID of the managed text printer which should print the message.</param>
        /// <param name="text">Text of the message to print.</param>
        /// <param name="authorId">ID of a character actor to which the printed text belongs (if any).</param>
        /// <param name="speed">Text reveal speed (<see cref="BaseRevealSpeed"/> modifier).</param>
        /// <param name="cancellationToken">Token for task cancellation. The text will be revealed instantly when cancelled.</param>
        public async Task PrintTextAsync (string printerId, string text, string authorId = default, float speed = 1, CancellationToken cancellationToken = default)
        {
            var printer = await GetOrAddActorAsync(printerId);

            OnPrintTextStarted?.Invoke(new PrintTextArgs(printer, text, authorId, speed));

            printer.AuthorId = authorId;
            printer.Text += text;

            var revealDelay = scriptPlayer.IsSkipActive ? 0 : Mathf.Lerp(Configuration.MaxRevealDelay, 0, BaseRevealSpeed * speed);
            await printer.RevealTextAsync(revealDelay, cancellationToken);

            printer.RevealProgress = 1f; // Make sure all the text is always revealed.

            if (scriptPlayer.IsAutoPlayActive)
            {
                var autoPlayDelay = revealDelay * text.Count(char.IsLetterOrDigit);
                var waitUntilTime = Time.time + autoPlayDelay;
                while (Time.time < waitUntilTime && !cancellationToken.IsCancellationRequested)
                    await waitForEndOfFrame;
            }
            else await waitForEndOfFrame; // Always wait at least one frame to prevent instant skipping.

            OnPrintTextFinished?.Invoke(new PrintTextArgs(printer, text, authorId, speed));
        }
    }
}
