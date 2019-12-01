// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Stops playing a BGM (background music) track with the provided name.
    /// </summary>
    /// <remarks>
    /// When music track name (BgmPath) is not specified, will stop all the currently played tracks.
    /// </remarks>
    /// <example>
    /// ; Fades-out the `Promenade` music track over 10 seconds and stops the playback
    /// @stopBgm Promenade time:10
    /// 
    /// ; Stops all the currently played music tracks
    /// @stopBgm 
    /// </example>
    public class StopBgm : Command
    {
        /// <summary>
        /// Path to the music track to stop.
        /// </summary>
        [CommandParameter(NamelessParameterAlias, true)]
        public string BgmPath { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var manager = Engine.GetService<AudioManager>();

            if (string.IsNullOrWhiteSpace(BgmPath))
                await manager.StopAllBgmAsync(Duration, cancellationToken);
            else await manager.StopBgmAsync(BgmPath, Duration, cancellationToken);
        }
    } 
}
