// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Resets (clears) the contents of a text printer.
    /// </summary>
    /// <example>
    /// ; Clear the content of a default printer.
    /// @resetText
    /// ; Clear the content of a printer with ID `Fullscreen`.
    /// @resetText Fullscreen
    /// </example>
    public class ResetText : PrinterCommand
    {
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [CommandParameter(NamelessParameterAlias, true)]
        public override string PrinterId { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var printer = await GetOrAddPrinterAsync();
            if (cancellationToken.IsCancellationRequested) return;

            printer.Text = string.Empty;
            printer.RevealProgress = 0f;
        }
    } 
}
