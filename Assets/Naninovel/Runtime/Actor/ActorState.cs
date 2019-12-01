// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents serializable state of a <see cref="IActor"/>.
    /// </summary>
    [System.Serializable]
    public abstract class ActorState
    {
        public string Id => id;

        [SerializeField] private string id = default;
        [SerializeField] private string appearance = default;
        [SerializeField] private bool isVisible = false;
        [SerializeField] private Vector3 position = Vector3.zero;
        [SerializeField] private Quaternion rotation = Quaternion.identity;
        [SerializeField] private Vector3 scale = Vector3.one;
        [SerializeField] private Color tintColor = Color.white;

        public string ToJson () => JsonUtility.ToJson(this);

        public void OverwriteFromJson (string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }

        public void OverwriteFromActor (IActor actor)
        {
            id = actor.Id;
            appearance = actor.Appearance;
            isVisible = actor.IsVisible;
            position = actor.Position;
            rotation = actor.Rotation;
            scale = actor.Scale;
            tintColor = actor.TintColor;
        }

        public void ApplyToActor (IActor actor)
        {
            actor.Appearance = appearance;
            actor.IsVisible = isVisible;
            actor.Position = position;
            actor.Rotation = rotation;
            actor.Scale = scale;
            actor.TintColor = tintColor;
        }
    }

    /// <summary>
    /// Represents serializable state of a <typeparamref name="TActor"/>.
    /// </summary>
    [System.Serializable]
    public abstract class ActorState<TActor> : ActorState
        where TActor : IActor
    {
        public virtual void OverwriteFromActor (TActor actor)
        {
            base.OverwriteFromActor(actor);
        }

        public virtual void ApplyToActor (TActor actor)
        {
            base.ApplyToActor(actor);
        }
    }
}
