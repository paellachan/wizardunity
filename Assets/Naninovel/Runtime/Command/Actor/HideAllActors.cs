// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Hides (removes) all the actors (eg characters, backgrounds, text printers, choice handlers, etc) on scene.
    /// </summary>
    /// <example>
    /// @hideAll
    /// </example>
    [CommandAlias("hideAll")]
    public class HideAllActors : Command
    {
        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var managers = Engine.GetAllServices<IActorManager>();
            await Task.WhenAll(managers.SelectMany(m => m.GetAllActors()).Select(a => a.ChangeVisibilityAsync(false, Duration, cancellationToken: cancellationToken)));
        }
    } 
}
