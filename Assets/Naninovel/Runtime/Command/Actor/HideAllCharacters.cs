// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Hides (removes) all the visible characters on scene.
    /// </summary>
    /// <example>
    /// @hideChars
    /// </example>
    [CommandAlias("hideChars")]
    public class HideAllCharacters : Command
    {
        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var manager = Engine.GetService<CharacterManager>();
            await Task.WhenAll(manager.GetAllActors().Select(a => a.ChangeVisibilityAsync(false, Duration, cancellationToken: cancellationToken)));
        }
    }
}
