// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Closes an [`@if`](/api/#if) conditional execution block.
    /// For usage examples see [conditional execution](/guide/naninovel-scripts.md#conditional-execution) guide.
    /// </summary>
    public class EndIf : Command
    {
        public override Task ExecuteAsync (CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
