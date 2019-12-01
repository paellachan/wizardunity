// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Stops playing an SFX (sound effect) track with the provided name.
    /// </summary>
    /// <remarks>
    /// When sound effect track name (SfxPath) is not specified, will stop all the currently played tracks.
    /// </remarks>
    /// <example>
    /// ; Stop playing an SFX with the name `Rain`, fading-out for 15 seconds.
    /// @stopSfx Rain time:15
    /// 
    /// ; Stops all the currently played sound effect tracks
    /// @stopSfx
    /// </example>
    public class StopSfx : Command
    {
        /// <summary>
        /// Path to the sound effect to stop.
        /// </summary>
        [CommandParameter(NamelessParameterAlias, true)]
        public string SfxPath { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var manager = Engine.GetService<AudioManager>();

            if (string.IsNullOrWhiteSpace(SfxPath))
                await manager.StopAllSfxAsync(Duration, cancellationToken);
            else await manager.StopSfxAsync(SfxPath, Duration, cancellationToken);
        }
    } 
}
