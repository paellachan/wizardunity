// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public abstract class OrthoActorManagerConfiguration<TMeta> : ActorManagerConfiguration<TMeta>
        where TMeta : ActorMetadata
    {
        [Tooltip("Origin point used for reference when positioning actors on scene.")]
        public Vector2 SceneOrigin = new Vector2(.5f, 0f);
    }
}
