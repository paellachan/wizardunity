using SpriteDicing;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="SpriteDicing.DicedSpriteRenderer"/> to represent an actor.
    /// </summary>
    public class DicedSpriteCharacter : MonoBehaviourActor, ICharacterActor
    {
        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool IsVisible { get => isVisible; set => SetVisibility(value); }
        public CharacterLookDirection LookDirection { get => GetLookDirection(); set => SetLookDirection(value); }

        protected DicedSpriteRenderer DicedSpriteRenderer { get; }

        /// <summary>
        /// Identifies look direction baked on the actors sprites.
        /// </summary>
        private static readonly CharacterLookDirection initialLookDirection = CharacterLookDirection.Left;
        private string appearance;
        private bool isVisible;
        private LocalizableResourceLoader<DicedSpriteAtlas> appearanceLoader;
        private Tweener<ColorTween> fadeTweener;

        public DicedSpriteCharacter (string id, CharacterMetadata metadata)
            : base(id, metadata)
        {
            // Only project provider is supported.
            metadata.LoaderConfiguration.ProviderTypes = new List<ResourceProviderType> { ResourceProviderType.Project };

            DicedSpriteRenderer = GameObject.AddComponent<DicedSpriteRenderer>();
            fadeTweener = new Tweener<ColorTween>(ActorBehaviour);

            var providerMngr = Engine.GetService<ResourceProviderManager>();
            var localeMngr = Engine.GetService<LocalizationManager>();
            appearanceLoader = new LocalizableResourceLoader<DicedSpriteAtlas>(metadata.LoaderConfiguration, providerMngr, localeMngr);

            SetVisibility(false);
        }

        public override async Task ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default)
        {
            this.appearance = appearance;

            if (string.IsNullOrEmpty(appearance)) return;

            var atlasResource = await appearanceLoader.LoadAsync(Id);
            if (!atlasResource.IsValid) return;

            // In case user stored source sprites in folders, the diced sprites will have dots in their names.
            var spriteName = appearance.Replace("/", ".");
            var dicedSprite = atlasResource.Object.GetSprite(spriteName);
            // TODO: Support crossfading diced sprite.
            DicedSpriteRenderer.SetDicedSprite(dicedSprite);
        }

        public override async Task ChangeVisibilityAsync (bool isVisible, float duration, EasingType easingType = default)
        {
            this.isVisible = isVisible;

            if (fadeTweener.IsRunning)
                fadeTweener.CompleteInstantly();
            var opacity = isVisible ? 1 : 0;
            var tween = new ColorTween(DicedSpriteRenderer.Color, new Color(0, 0, 0, opacity), ColorTweenMode.Alpha,
                duration, value => DicedSpriteRenderer.Color = value, false, easingType);
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

        public Task ChangeLookDirectionAsync (CharacterLookDirection lookDirection, float duration, EasingType easingType = default)
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
            DicedSpriteRenderer.Color = new Color(DicedSpriteRenderer.Color.r, DicedSpriteRenderer.Color.g, DicedSpriteRenderer.Color.b, isVisible ? 1 : 0);
        }

        protected override Color GetBehaviourTintColor () => DicedSpriteRenderer.Color;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!IsVisible) // Handle visibility-controlled alpha of the tint color.
                tintColor.a = DicedSpriteRenderer.Color.a;
            DicedSpriteRenderer.Color = tintColor;
        }

        protected virtual void SetLookDirection (CharacterLookDirection lookDirection)
        {
            if (initialLookDirection == CharacterLookDirection.Center) return;
            if (lookDirection == CharacterLookDirection.Center)
            {
                DicedSpriteRenderer.FlipX = false;
                return;
            }
            if (lookDirection != LookDirection)
                DicedSpriteRenderer.FlipX = !DicedSpriteRenderer.FlipX;
        }

        protected virtual CharacterLookDirection GetLookDirection ()
        {
            switch (initialLookDirection)
            {
                case CharacterLookDirection.Center:
                    return CharacterLookDirection.Center;
                case CharacterLookDirection.Left:
                    return DicedSpriteRenderer.FlipX ? CharacterLookDirection.Right : CharacterLookDirection.Left;
                case CharacterLookDirection.Right:
                    return DicedSpriteRenderer.FlipX ? CharacterLookDirection.Left : CharacterLookDirection.Right;
                default:
                    return default;
            }
        }
    }
}
