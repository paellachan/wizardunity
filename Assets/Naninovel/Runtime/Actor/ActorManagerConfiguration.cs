// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public abstract class ActorManagerConfiguration : Configuration
    {
        [Tooltip("Eeasing function to use by default for all the actor modifications (changing appearance, position, tint, etc).")]
        public EasingType DefaultEasing = EasingType.Linear;
    }

    public abstract class ActorManagerConfiguration<TMeta> : ActorManagerConfiguration
        where TMeta : ActorMetadata
    {
        public abstract TMeta DefaultActorMetadata { get; }
        public abstract ActorMetadataMap<TMeta> ActorMetadataMap { get; }
    }
}
