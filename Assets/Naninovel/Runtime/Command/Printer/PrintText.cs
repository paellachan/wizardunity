// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;
using UnityCommon;

namespace Naninovel.Commands
{
    /// <summary>
    /// Prints (reveals over time) specified text message using a text printer actor.
    /// </summary>
    /// <remarks>
    /// This command is used under the hood when processing generic text lines, eg generic line `Kohaku: Hello World!` will be 
    /// automatically tranformed into `@print "Hello World!" author:Kohaku` when parsing the naninovel scripts.<br/>
    /// Will reset (clear) the printer before printing the new message by default; set `reset` parameter to *false* to prevent that and append the text instead.<br/>
    /// Will wait for user input before finishing the task by default; set `waitInput` parameter to *false* to return as soon as the text is fully revealed.<br/>
    /// Will cancel the printing (reveal the text at once) on `Continue` and `Skip` inputs.<br/>
    /// </remarks>
    /// <example>
    /// ; Will print the phrase with a default printer
    /// @print "Lorem ipsum dolor sit amet."
    /// ; To include quotes in the text itself, escape them
    /// @print "Saying \"Stop the car\" was a mistake."
    /// </example>
    [CommandAlias("print")]
    public class PrintText : PrinterCommand, Command.IPreloadable, Command.ILocalizable
    {
        /// <summary>
        /// Text of the message to print.
        /// When the text contain spaces, wrap it in double quotes (`"`). 
        /// In case you wish to include the double quotes in the text itself, escape them.
        /// </summary>
        [CommandParameter(NamelessParameterAlias)]
        public string Text { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [CommandParameter("printer", true)]
        public override string PrinterId { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// ID of the actor, which should be associated with the printed message.
        /// </summary>
        [CommandParameter("author", true)]
        public string AuthorId { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Text reveal speed multiplier; should be positive or zero. Setting to one will yield the default speed.
        /// </summary>
        [CommandParameter("speed", true)]
        public float RevealSpeed { get => GetDynamicParameter(1); set => SetDynamicParameter(value); }
        /// <summary>
        /// Whether to reset text of the printer before executing the printing task.
        /// </summary>
        [CommandParameter("reset", true)]
        public bool ResetPrinter { get => GetDynamicParameter(true); set => SetDynamicParameter(value); }
        /// <summary>
        /// Whether to wait for user input after finishing the printing task.
        /// </summary>
        [CommandParameter("waitInput", true)]
        public bool WaitForInput { get => GetDynamicParameter(true); set => SetDynamicParameter(value); }

        public string AutoVoicePath => AudioManager.GetAutoVoiceClipPath(PlaybackSpot);

        protected bool AutoVoicingEnabled => IsFromScriptLine && (AudioManager?.AutoVoicingEnabled ?? false);
        protected AudioManager AudioManager => audioManagerCache ?? (audioManagerCache = Engine.GetService<AudioManager>());

        private AudioManager audioManagerCache;

        public override async Task HoldResourcesAsync ()
        {
            await base.HoldResourcesAsync();

            if (AutoVoicingEnabled)
                await AudioManager.HoldVoiceResourcesAsync(this, AutoVoicePath);
        }

        public override void ReleaseResources ()
        {
            base.ReleaseResources();

            if (AutoVoicingEnabled)
                AudioManager.ReleaseVoiceResources(this, AutoVoicePath);
        }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (AutoVoicingEnabled)
                AudioManager.PlayVoiceAsync(AutoVoicePath).WrapAsync();

            var printer = await GetOrAddPrinterAsync();

            if (ResetPrinter)
            {
                printer.Text = string.Empty;
                printer.RevealProgress = 0f;
            }

            var inputMngr = Engine.GetService<InputManager>();
            var continueInputCts = inputMngr?.Continue?.GetInputStartCancellationToken();
            var skipInputCts = inputMngr?.Skip?.GetInputStartCancellationToken();
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(continueInputCts ?? default, skipInputCts ?? default))
                await PrinterManager.PrintTextAsync(printer.Id, Text, AuthorId, RevealSpeed, cts.Token);

            if (WaitForInput) Engine.GetService<ScriptPlayer>()?.EnableWaitingForInput();

            if (ResetPrinter) Engine.GetService<UIManager>()?.GetUI<UI.IBacklogUI>()?.AddMessage(Text, AuthorId, AutoVoicingEnabled ? AutoVoicePath : null);
            else Engine.GetService<UIManager>()?.GetUI<UI.IBacklogUI>()?.AppendMessage(Text, AutoVoicingEnabled ? AutoVoicePath : null);
        }
    } 
}
