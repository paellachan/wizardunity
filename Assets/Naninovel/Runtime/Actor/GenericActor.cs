// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IActor"/> implementation using <typeparamref name="TBehaviour"/> to represent the actor.
    /// </summary>
    /// <remarks>
    /// Resource prefab should have a <typeparamref name="TBehaviour"/> component attached to the root object.
    /// Apperance and other property changes changes are routed to the events of the <typeparamref name="TBehaviour"/> component.
    /// </remarks>
    public abstract class GenericActor<TBehaviour> : MonoBehaviourActor
        where TBehaviour : GenericActorBehaviour
    {
        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool IsVisible { get => isVisible; set => SetVisibility(value); }

        protected TBehaviour Behaviour { get; private set; }

        private ActorMetadata metadata;
        private string appearance;
        private bool isVisible;
        private Color tintColor = Color.white;

        public GenericActor (string id, ActorMetadata metadata)
            : base(id, metadata)
        {
            this.metadata = metadata;
        }

        public override async Task InitializeAsync ()
        {
            await base.InitializeAsync();

            var providerMngr = Engine.GetService<ResourceProviderManager>();
            var localeMngr = Engine.GetService<LocalizationManager>();
            var prefabLoader = new LocalizableResourceLoader<GameObject>(metadata.LoaderConfiguration, providerMngr, localeMngr);
            var prefabResource = await prefabLoader.LoadAsync(Id);

            Behaviour = Engine.Instantiate(prefabResource.Object).GetComponent<TBehaviour>();
            Behaviour.transform.SetParent(Transform);

            SetVisibility(false);
        }

        public override Task ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            SetAppearance(appearance);
            return Task.CompletedTask;
        }

        public override Task ChangeVisibilityAsync (bool isVisible, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            SetVisibility(isVisible);
            return Task.CompletedTask;
        }

        protected virtual void SetAppearance (string appearance)
        {
            this.appearance = appearance;

            if (string.IsNullOrEmpty(appearance))
                return;

            Behaviour.InvokeAppearanceChangedEvent(appearance);
        }

        protected virtual void SetVisibility (bool isVisible)
        {
            this.isVisible = isVisible;

            Behaviour.InvokeVisibilityChangedEvent(isVisible);
        }

        protected override Color GetBehaviourTintColor () => tintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            this.tintColor = tintColor;

            Behaviour.InvokeTintColorChangedEvent(tintColor);
        }
    }
}
