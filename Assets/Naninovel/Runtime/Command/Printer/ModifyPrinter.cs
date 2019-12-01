// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Modifies a [printer actor](/guide/text-printers.md).
    /// </summary>
    /// <example>
    /// ; Will make `Wide` printer default and hide any other visible printers.
    /// @printer Wide
    /// 
    /// ; Will assign `Right` appearance to `Bubble` printer, make is default,
    /// ; position at the center of the screen and won't hide other printers.
    /// @printer Bubble.Right pos:50,50 hideOther:false
    /// </example>
    [CommandAlias("printer")]
    public class ModifyPrinter : PrinterCommand, Command.IPreloadable
    {
        /// <summary>
        /// ID of the printer to modify and the appearance to set. 
        /// When ID or appearance are not provided, will use default ones.
        /// </summary>
        [CommandParameter(NamelessParameterAlias, true)]
        public Named<string> IdAndAppearance { get => GetDynamicParameter<Named<string>>(null); set => SetDynamicParameter(value); }
        public override string PrinterId { get => IdAndAppearance?.Item1; set { } }
        /// <summary>
        /// Whether to make the printer the default one.
        /// Default printer will be subject of all the printer-related commands when `printer` parameter is not specified.
        /// </summary>
        [CommandParameter("default", true)]
        public bool MakeDefault { get => GetDynamicParameter(true); set => SetDynamicParameter(value); }
        /// <summary>
        /// Whether to hide all the other printers.
        /// </summary>
        [CommandParameter(optional: true)]
        public bool HideOther { get => GetDynamicParameter(true); set => SetDynamicParameter(value); }
        /// <summary>
        /// Position (relative to the screen borders, in percents) to set for the modified printer.
        /// Position is described as follows: `0,0` is the bottom left, `50,50` is the center and `100,100` is the top right corner of the screen.
        /// </summary>
        [CommandParameter("pos", true)]
        public float?[] ScenePosition { get => GetDynamicParameter<float?[]>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Whether to show or hide the printer.
        /// </summary>
        [CommandParameter(optional: true)]
        public bool? Visible { get => GetDynamicParameter<bool?>(null); set => SetDynamicParameter(value); }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (MakeDefault && !string.IsNullOrEmpty(PrinterId))
                PrinterManager.DefaultPrinterId = PrinterId;

            if (HideOther)
                foreach (var prntr in PrinterManager.GetAllActors())
                    if (prntr.Id != (PrinterId ?? PrinterManager.DefaultPrinterId) && prntr.IsVisible)
                        prntr.ChangeVisibilityAsync(false, Duration).WrapAsync();

            var printer = default(ITextPrinterActor);

            var appearance = IdAndAppearance?.Item2;
            if (!string.IsNullOrEmpty(appearance))
            {
                if (printer is null) printer = await GetOrAddPrinterAsync();
                await printer.ChangeAppearanceAsync(appearance, Duration, cancellationToken: cancellationToken);
            }

            if (ScenePosition != null)
            {
                if (printer is null) printer = await GetOrAddPrinterAsync();
                var position = new Vector3(
                    ScenePosition.ElementAtOrDefault(0) != null ? PrinterManager.SceneToWorldSpace(new Vector2(ScenePosition[0].Value / 100f, 0)).x : printer.Position.x,
                    ScenePosition.ElementAtOrDefault(1) != null ? PrinterManager.SceneToWorldSpace(new Vector2(0, ScenePosition[1].Value / 100f)).y : printer.Position.y,
                    ScenePosition.ElementAtOrDefault(2) ?? printer.Position.z
                );
                await printer.ChangePositionAsync(position, Duration, cancellationToken: cancellationToken);
            }

            if (Visible.HasValue)
            {
                if (printer is null) printer = await GetOrAddPrinterAsync();
                await printer.ChangeVisibilityAsync(Visible.Value, Duration, cancellationToken: cancellationToken);
            }
        }
    } 
}
