// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Shows a text printer.
    /// </summary>
    /// <example>
    /// ; Show a default printer.
    /// @showPrinter
    /// ; Show printer with ID `Wide`.
    /// @showPrinter Wide
    /// </example>
    public class ShowPrinter : PrinterCommand
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

            await printer.ChangeVisibilityAsync(true, Duration, cancellationToken: cancellationToken);
        }
    } 
}
