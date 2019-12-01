// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading.Tasks;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages background actors in the ortho mode.
    /// </summary>
    [InitializeAtRuntime]
    public class BackgroundManager : OrthoActorManager<IBackgroundActor, BackgroundState, BackgroundMetadata, BackgroundsConfiguration>
    {
        /// <summary>
        /// ID of the background actor used by default.
        /// </summary>
        public const string MainActorId = "MainBackground";

        public int ZOffset => Configuration.ZOffset;
        public int TopmostZPosition => ZOffset - ManagedActors.Count;

        public BackgroundManager (BackgroundsConfiguration config, CameraManager orthoCamera)
            : base(config, orthoCamera) { }

        protected override async Task<IBackgroundActor> ConstructActorAsync (string actorId)
        {
            var actor = await base.ConstructActorAsync(actorId);

            // When adding new background place it at the topmost z position.
            actor.Position = new Vector3(0, 0, TopmostZPosition);

            return actor;
        }
    }
}
