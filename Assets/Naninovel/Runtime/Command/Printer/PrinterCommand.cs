// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading.Tasks;

namespace Naninovel.Commands
{
    public abstract class PrinterCommand : Command
    {
        public abstract string PrinterId { get; set; }

        protected TextPrinterManager PrinterManager => printerManagerCache ?? (printerManagerCache = Engine.GetService<TextPrinterManager>());

        private TextPrinterManager printerManagerCache;
        private ITextPrinterActor heldPrinterActor;

        public virtual async Task HoldResourcesAsync ()
        {
            heldPrinterActor = await GetOrAddPrinterAsync();
            await heldPrinterActor.HoldResourcesAsync(this, null);
        }

        public virtual void ReleaseResources ()
        {
            heldPrinterActor?.ReleaseResources(this, null);
        }

        protected virtual async Task<ITextPrinterActor> GetOrAddPrinterAsync ()
        {
            return await PrinterManager.GetOrAddActorAsync(PrinterId ?? PrinterManager.DefaultPrinterId);
        }
    }
}
