// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel.Commands
{
    public abstract class ModifyActor<TActor, TState, TMeta, TConfig, TManager> : Command, Command.IPreloadable 
        where TActor : IActor
        where TState : ActorState<TActor>, new()
        where TMeta : ActorMetadata
        where TConfig : ActorManagerConfiguration<TMeta>
        where TManager : ActorManager<TActor, TState, TMeta, TConfig>
    {
        /// <summary>
        /// ID of the actor to modify.
        /// </summary>
        [CommandParameter(optional: true)]
        public virtual string Id { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Appearance to set for the modified actor.
        /// </summary>
        [CommandParameter(optional: true)]
        public virtual string Appearance { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Visibility status to set for the modified actor.
        /// </summary>
        [CommandParameter(optional: true)]
        public virtual bool? Visible { get => GetDynamicParameter<bool?>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Position (in world space) to set for the modified actor. 
        /// Use Z-component (third member) to move (sort) by depth while in ortho mode.
        /// </summary>
        [CommandParameter(optional: true)]
        public virtual float?[] Position { get => GetDynamicParameter<float?[]>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Rotation to set for the modified actor.
        /// </summary>
        [CommandParameter(optional: true)]
        public virtual float?[] Rotation { get => GetDynamicParameter<float?[]>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Scale to set for the modified actor.
        /// </summary>
        [CommandParameter(optional: true)]
        public virtual float?[] Scale { get => GetDynamicParameter<float?[]>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Tint color to set for the modified actor.
        /// <br/><br/>
        /// Strings that begin with `#` will be parsed as hexadecimal in the following way: 
        /// `#RGB` (becomes RRGGBB), `#RRGGBB`, `#RGBA` (becomes RRGGBBAA), `#RRGGBBAA`; when alpha is not specified will default to FF.
        /// <br/><br/>
        /// Strings that do not begin with `#` will be parsed as literal colors, with the following supported:
        /// red, cyan, blue, darkblue, lightblue, purple, yellow, lime, fuchsia, white, silver, grey, black, orange, brown, maroon, green, olive, navy, teal, aqua, magenta.
        /// </summary>
        [CommandParameter("tint", true)]
        public virtual string TintColor { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Name of the easing function to use for the modification.
        /// <br/><br/>
        /// Available options: Linear, SmoothStep, Spring, EaseInQuad, EaseOutQuad, EaseInOutQuad, EaseInCubic, EaseOutCubic, EaseInOutCubic, EaseInQuart, EaseOutQuart, EaseInOutQuart, EaseInQuint, EaseOutQuint, EaseInOutQuint, EaseInSine, EaseOutSine, EaseInOutSine, EaseInExpo, EaseOutExpo, EaseInOutExpo, EaseInCirc, EaseOutCirc, EaseInOutCirc, EaseInBounce, EaseOutBounce, EaseInOutBounce, EaseInBack, EaseOutBack, EaseInOutBack, EaseInElastic, EaseOutElastic, EaseInOutElastic.
        /// <br/><br/>
        /// When not specified, will use a default easing function set in the actor's manager configuration settings.
        /// </summary>
        [CommandParameter("easing", true)]
        public virtual string EasingTypeName { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }

        protected virtual TManager ActorManager => actorManagerCache ?? (actorManagerCache = Engine.GetService<TManager>());

        private TManager actorManagerCache;

        public virtual async Task HoldResourcesAsync ()
        {
            if (ActorManager is null || string.IsNullOrWhiteSpace(Id)) return;
            var actor = await ActorManager.GetOrAddActorAsync(Id);
            await actor.HoldResourcesAsync(this, Appearance);
        }

        public virtual void ReleaseResources ()
        {
            if (ActorManager is null || string.IsNullOrWhiteSpace(Id)) return;
            if (ActorManager.ActorExists(Id))
                ActorManager.GetActor(Id).ReleaseResources(this, Appearance);
        }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (ActorManager is null)
            {
                Debug.LogError("Can't resolve actors manager.");
                return;
            }

            if (string.IsNullOrEmpty(Id))
            {
                Debug.LogError("Actor ID was not provided.");
                return;
            }

            var actor = await ActorManager.GetOrAddActorAsync(Id);
            if (cancellationToken.IsCancellationRequested) return;

            var easingType = ActorManager.DefaultEasingType;
            if (!string.IsNullOrEmpty(EasingTypeName) && !Enum.TryParse(EasingTypeName, true, out easingType))
                Debug.LogWarning($"Failed to parse `{EasingTypeName}` easing.");
            await ApplyModificationsAsync(actor, easingType, cancellationToken);
        }

        protected virtual async Task ApplyModificationsAsync (TActor actor, EasingType easingType, CancellationToken cancellationToken)
        {
            // When visibility is not explicitly specified assume user would like to show the actor anyway.
            if (!Visible.HasValue) Visible = true;

            await Task.WhenAll(
                    ApplyAppearanceModificationAsync(actor, easingType, cancellationToken),
                    ApplyVisibilityModificationAsync(actor, easingType, cancellationToken),
                    ApplyPositionModificationAsync(actor, easingType, cancellationToken),
                    ApplyRotationModificationAsync(actor, easingType, cancellationToken),
                    ApplyScaleModificationAsync(actor, easingType, cancellationToken),
                    ApplyTintColorModificationAsync(actor, easingType, cancellationToken)
                );
        }

        protected virtual async Task ApplyAppearanceModificationAsync (TActor actor, EasingType easingType, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Appearance)) return;
            await actor.ChangeAppearanceAsync(Appearance, Duration, easingType, cancellationToken);
        }

        protected virtual async Task ApplyVisibilityModificationAsync (TActor actor, EasingType easingType, CancellationToken cancellationToken)
        {
            if (Visible is null) return;
            await actor.ChangeVisibilityAsync(Visible.Value, Duration, easingType, cancellationToken);
        }

        protected virtual async Task ApplyPositionModificationAsync (TActor actor, EasingType easingType, CancellationToken cancellationToken)
        {
            if (Position is null) return;
            await actor.ChangePositionAsync(new Vector3(
                    Position.ElementAtOrDefault(0) ?? actor.Position.x,
                    Position.ElementAtOrDefault(1) ?? actor.Position.y,
                    Position.ElementAtOrDefault(2) ?? actor.Position.z), Duration, easingType, cancellationToken);
        }

        protected virtual async Task ApplyRotationModificationAsync (TActor actor, EasingType easingType, CancellationToken cancellationToken)
        {
            if (Rotation is null) return;
            await actor.ChangeRotationAsync(Quaternion.Euler(
                    Rotation.ElementAtOrDefault(0) ?? actor.Rotation.eulerAngles.x,
                    Rotation.ElementAtOrDefault(1) ?? actor.Rotation.eulerAngles.y,
                    Rotation.ElementAtOrDefault(2) ?? actor.Rotation.eulerAngles.z), Duration, easingType, cancellationToken);
        }

        protected virtual async Task ApplyScaleModificationAsync (TActor actor, EasingType easingType, CancellationToken cancellationToken)
        {
            if (Scale is null) return;
            await actor.ChangeScaleAsync(new Vector3(
                    Scale.ElementAtOrDefault(0) ?? actor.Scale.x,
                    Scale.ElementAtOrDefault(1) ?? actor.Scale.y,
                    Scale.ElementAtOrDefault(2) ?? actor.Scale.z), Duration, easingType, cancellationToken);
        }

        protected virtual async Task ApplyTintColorModificationAsync (TActor actor, EasingType easingType, CancellationToken cancellationToken)
        {
            if (TintColor is null) return;
            if (!ColorUtility.TryParseHtmlString(TintColor, out var color))
            {
                Debug.LogError($"Failed to parse `{TintColor}` color to apply tint modification for `{actor.Id}` actor. See the API docs for supported color formats.");
                return;
            }
            await actor.ChangeTintColorAsync(color, Duration, easingType, cancellationToken);
        }
    } 
}
