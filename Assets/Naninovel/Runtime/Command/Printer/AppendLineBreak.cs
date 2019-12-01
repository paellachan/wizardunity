// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Adds a line break to a text printer.
    /// </summary>
    /// <example>
    /// ; Second sentence will be printed on a new line
    /// Lorem ipsum dolor sit amet.[br]Consectetur adipiscing elit.
    /// 
    /// ; Second sentence will be printer two lines under the first one
    /// Lorem ipsum dolor sit amet.[br 2]Consectetur adipiscing elit.
    /// </example>
    [CommandAlias("br")]
    public class AppendLineBreak : PrinterCommand
    {
        /// <summary>
        /// Number of line breaks to add.
        /// </summary>
        [CommandParameter(alias: NamelessParameterAlias, optional: true)]
        public int Count { get => GetDynamicParameter(1); set => SetDynamicParameter(value); }
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [CommandParameter("printer", true)]
        public override string PrinterId { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var printer = await GetOrAddPrinterAsync();
            if (cancellationToken.IsCancellationRequested) return;
            var backlogUI = Engine.GetService<UIManager>()?.GetUI<UI.IBacklogUI>();

            for (int i = 0; i < Count; i++)
            {
                printer.Text += "\n";
                backlogUI?.AppendMessage("\n");
            }
        }
    } 
}
