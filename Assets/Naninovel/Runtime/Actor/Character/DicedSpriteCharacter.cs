// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using SpriteDicing;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="SpriteDicing.DicedSpriteRenderer"/> to represent the actor.
    /// </summary>
    public class DicedSpriteCharacter : MonoBehaviourActor, ICharacterActor
    {
        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool IsVisible { get => isVisible; set => SetVisibility(value); }
        public CharacterLookDirection LookDirection { get => GetLookDirection(); set => SetLookDirection(value); }

        private readonly CharacterMetadata metadata;
        private readonly DicedSpriteRenderer dicedSpriteRenderer;
        private readonly Tweener<ColorTween> fadeTweener;
        private LocalizableResourceLoader<DicedSpriteAtlas> appearanceLoader;
        private string appearance;
        private bool isVisible;

        public DicedSpriteCharacter (string id, CharacterMetadata metadata)
            : base(id, metadata)
        {
            this.metadata = metadata;

            dicedSpriteRenderer = GameObject.AddComponent<DicedSpriteRenderer>();
            fadeTweener = new Tweener<ColorTween>(ActorBehaviour);

            SetVisibility(false);
        }

        public override async Task InitializeAsync ()
        {
            await base.InitializeAsync();

            var providerMngr = Engine.GetService<ResourceProviderManager>();
            var localeMngr = Engine.GetService<LocalizationManager>();
            appearanceLoader = new LocalizableResourceLoader<DicedSpriteAtlas>(metadata.LoaderConfiguration, providerMngr, localeMngr);
        }

        public override async Task ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            this.appearance = appearance;

            if (string.IsNullOrEmpty(appearance)) return;

            var atlasResource = await appearanceLoader.LoadAsync(Id);
            if (!atlasResource.IsValid) return;

            // In case user stored source sprites in folders, the diced sprites will have dots in their names.
            var spriteName = appearance.Replace("/", ".");
            var dicedSprite = atlasResource.Object.GetSprite(spriteName);
            // TODO: Support crossfading diced sprite.
            dicedSpriteRenderer.SetDicedSprite(dicedSprite);
        }

        public override async Task ChangeVisibilityAsync (bool isVisible, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            this.isVisible = isVisible;

            if (fadeTweener.IsRunning)
                fadeTweener.CompleteInstantly();
            var opacity = isVisible ? 1 : 0;
            var tween = new ColorTween(dicedSpriteRenderer.Color, new Color(0, 0, 0, opacity), ColorTweenMode.Alpha,
                duration, value => dicedSpriteRenderer.Color = value, false, easingType);
            await fadeTweener.RunAsync(tween);
        }

        public override async Task HoldResourcesAsync (object holder, string appearance)
        {
            if (string.IsNullOrEmpty(appearance)) return;

            var resource = await appearanceLoader.LoadAsync(appearance);
            if (resource.IsValid)
                resource.Hold(holder);
        }

        public override void ReleaseResources (object holder, string appearance)
        {
            if (string.IsNullOrEmpty(appearance)) return;

            appearanceLoader.GetLoadedOrNull(appearance)?.Release(holder);
        }

        public override void Dispose ()
        {
            base.Dispose();

            appearanceLoader?.UnloadAll();
        }

        public Task ChangeLookDirectionAsync (CharacterLookDirection lookDirection, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            SetLookDirection(lookDirection);
            return Task.CompletedTask;
        }

        protected virtual void SetAppearance (string appearance) => ChangeAppearanceAsync(appearance, 0).WrapAsync();

        protected virtual void SetVisibility (bool isVisible)
        {
            this.isVisible = isVisible;

            if (fadeTweener.IsRunning)
                fadeTweener.CompleteInstantly();
            dicedSpriteRenderer.Color = new Color(dicedSpriteRenderer.Color.r, dicedSpriteRenderer.Color.g, dicedSpriteRenderer.Color.b, isVisible ? 1 : 0);
        }

        protected override Color GetBehaviourTintColor () => dicedSpriteRenderer.Color;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!IsVisible) // Handle visibility-controlled alpha of the tint color.
                tintColor.a = dicedSpriteRenderer.Color.a;
            dicedSpriteRenderer.Color = tintColor;
        }

        protected virtual void SetLookDirection (CharacterLookDirection lookDirection)
        {
            if (metadata.BakedLookDirection == CharacterLookDirection.Center) return;
            if (lookDirection == CharacterLookDirection.Center)
            {
                dicedSpriteRenderer.FlipX = false;
                return;
            }
            if (lookDirection != LookDirection)
                dicedSpriteRenderer.FlipX = !dicedSpriteRenderer.FlipX;
        }

        protected virtual CharacterLookDirection GetLookDirection ()
        {
            switch (metadata.BakedLookDirection)
            {
                case CharacterLookDirection.Center:
                    return CharacterLookDirection.Center;
                case CharacterLookDirection.Left:
                    return dicedSpriteRenderer.FlipX ? CharacterLookDirection.Right : CharacterLookDirection.Left;
                case CharacterLookDirection.Right:
                    return dicedSpriteRenderer.FlipX ? CharacterLookDirection.Left : CharacterLookDirection.Right;
                default:
                    return default;
            }
        }
    }
}
