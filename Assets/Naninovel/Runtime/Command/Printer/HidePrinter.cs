// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Hides a text printer.
    /// </summary>
    /// <example>
    /// ; Hide a default printer.
    /// @hidePrinter
    /// ; Hide printer with ID `Wide`.
    /// @hidePrinter Wide
    /// </example>
    public class HidePrinter : PrinterCommand
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
            await printer.ChangeVisibilityAsync(false, Duration, cancellationToken: cancellationToken);
        }
    } 
}
