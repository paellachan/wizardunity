// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Plays or modifies currently played SFX (sound effect) track with the provided name.
    /// </summary>
    /// <remarks>
    /// Sound effect tracks are not looped by default.
    /// When sfx track name (SfxPath) is not specified, will affect all the currently played tracks.
    /// When invoked for a track that is already playing, the playback won't be affected (track won't start playing from the start),
    /// but the specified parameters (volume and whether the track is looped) will be applied.
    /// </remarks>
    /// <example>
    /// ; Plays an SFX with the name `Explosion` once
    /// @sfx Explosion
    /// 
    /// ; Plays an SFX with the name `Rain` in a loop
    /// @sfx Rain loop:true
    /// 
    /// ; Changes volume of all the played SFX tracks to 75% over 2.5 seconds and disables looping for all of them
    /// @sfx volume:0.75 loop:false time:2.5
    /// </example>
    [CommandAlias("sfx")]
    public class PlaySfx : Command, Command.IPreloadable
    {
        /// <summary>
        /// Path to the sound effect asset to play.
        /// </summary>
        [CommandParameter(NamelessParameterAlias, true)]
        public string SfxPath { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Volume of the sound effect.
        /// </summary>
        [CommandParameter(optional: true)]
        public float Volume { get => GetDynamicParameter(1f); set => SetDynamicParameter(value); }
        /// <summary>
        /// Whether to play the sound effect in a loop.
        /// </summary>
        [CommandParameter(optional: true)]
        public bool Loop { get => GetDynamicParameter(false); set => SetDynamicParameter(value); }

        public async Task HoldResourcesAsync ()
        {
            if (string.IsNullOrWhiteSpace(SfxPath)) return;
            await Engine.GetService<AudioManager>()?.HoldAudioResourcesAsync(this, SfxPath);
        }

        public void ReleaseResources ()
        {
            if (string.IsNullOrWhiteSpace(SfxPath)) return;
            Engine.GetService<AudioManager>()?.ReleaseAudioResources(this, SfxPath);
        }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var manager = Engine.GetService<AudioManager>();
            if (string.IsNullOrWhiteSpace(SfxPath))
                await Task.WhenAll(manager.PlayedSfx.Select(s => PlayOrModifyTrackAsync(manager, s.Path, Volume, Loop, Duration, cancellationToken)));
            else await PlayOrModifyTrackAsync(manager, SfxPath, Volume, Loop, Duration, cancellationToken);
        }

        private static async Task PlayOrModifyTrackAsync (AudioManager mngr, string path, float volume, bool loop, float time, CancellationToken cancellationToken)
        {
            if (mngr.IsSfxPlaying(path)) await mngr.ModifySfxAsync(path, volume, loop, time, cancellationToken);
            else await mngr.PlaySfxAsync(path, volume, time, loop, cancellationToken);
        }
    } 
}
