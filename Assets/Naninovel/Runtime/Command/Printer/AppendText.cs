// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Appends provided text to a text printer.
    /// </summary>
    /// <remarks>
    /// The entire text will be appended immediately, without triggering reveal effect or any other side-effects.
    /// </remarks>
    /// <example>
    /// ; Print first part of the sentence as usual (gradually revealing the message),
    /// ; then append the end of the sentence at once.
    /// Lorem ipsum
    /// @append " dolor sit amet."
    /// </example>
    [CommandAlias("append")]
    public class AppendText : PrinterCommand, Command.ILocalizable
    {
        /// <summary>
        /// The text to append.
        /// </summary>
        [CommandParameter(NamelessParameterAlias)]
        public string Text { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// ID of the printer actor to use. Will use a a default one when not provided.
        /// </summary>
        [CommandParameter("printer", true)]
        public override string PrinterId { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var printer = await GetOrAddPrinterAsync();
            if (cancellationToken.IsCancellationRequested) return;

            printer.Text += Text;
            printer.RevealProgress = 1f;

            Engine.GetService<UIManager>().GetUI<UI.IBacklogUI>()?.AppendMessage(Text);
        }
    } 
}
