// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages <typeparamref name="TActor"/> objects in orthographic scene space.
    /// </summary>
    public abstract class OrthoActorManager<TActor, TState, TMeta, TConfig> : ActorManager<TActor, TState, TMeta, TConfig>
        where TActor : IActor
        where TState : ActorState<TActor>, new()
        where TMeta : OrthoActorMetadata
        where TConfig : OrthoActorManagerConfiguration<TMeta>
    {
        /// <summary>
        /// Scene origin point position in world space.
        /// </summary>
        public Vector2 GlobalSceneOrigin => SceneToWorldSpace(Configuration.SceneOrigin);

        protected CameraManager OrthoCamera { get; private set; }

        public OrthoActorManager (TConfig config, CameraManager orthoCamera)
            : base(config)
        {
            OrthoCamera = orthoCamera;
        }

        /// <summary>
        /// Converts ortho scene space position to world position.
        /// Scene space described as follows: x0y0 is at the bottom left and x1y1 is at the top right corner of the screen.
        /// </summary>
        public Vector2 SceneToWorldSpace (Vector2 scenePosition)
        {
            var originPosition = -OrthoCamera.ReferenceSize / 2f;
            return originPosition + Vector2.Scale(scenePosition, OrthoCamera.ReferenceSize);
        }

        /// <summary>
        /// Changes provided actor y position so that it's bottom edge is alligned with the bottom of the screen.
        /// </summary>
        public void MoveActorToBottom (TActor actor)
        {
            var metadata = GetActorMetadata(actor.Id);
            var bottomY = (metadata.Pivot.y * actor.Scale.y) / metadata.PixelsPerUnit - OrthoCamera.MaxOrthoSize;
            actor.ChangePositionY(bottomY);
        }
    }
}
