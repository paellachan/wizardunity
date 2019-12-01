// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents serializable state of <see cref="ICharacterActor"/>.
    /// </summary>
    [System.Serializable]
    public class CharacterState : ActorState<ICharacterActor>
    {
        [SerializeField] private CharacterLookDirection lookDirection = default;

        public override void OverwriteFromActor (ICharacterActor actor)
        {
            base.OverwriteFromActor(actor);

            lookDirection = actor.LookDirection;
        }

        public override void ApplyToActor (ICharacterActor actor)
        {
            base.ApplyToActor(actor);

            actor.LookDirection = lookDirection;
        }
    }
}
