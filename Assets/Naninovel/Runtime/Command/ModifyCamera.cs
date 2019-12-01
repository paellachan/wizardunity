// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Modifies the main camera, changing offset, zoom level and rotation over time.
    /// Check [this video](https://youtu.be/zy28jaMss8w) for a quick demonstration of the command effect.
    /// </summary>
    /// <example>
    /// ; Offset over X-axis (pan) the camera by -3 units and offset over Y-axis by 1.5 units
    /// @camera offset:-3,1.5
    /// 
    /// ; Set camera in perspective mode, zoom-in by 50% and move back by 5 units
    /// @camera ortho:false offset:,,-5 zoom:0.5
    /// 
    /// ; Set camera in orthographic mode and rotate by 10 degrees clock-wise
    /// @camera ortho:true rotation:10
    /// 
    /// ; Offset, zoom and rotate simultaneously animated over 5 seconds
    /// @camera offset:-3,1.5 zoom:0.5 rotation:10 time:5
    /// 
    /// ; Instantly reset camera to the default state
    /// @camera offset:0,0 zoom:0 rotation:0 time:0
    /// 
    /// ; Toggle `FancyCameraFilter` and `Bloom` components attached to the camera
    /// @camera toggle:FancyCameraFilter,Bloom
    /// </example>
    [CommandAlias("camera")]
    public class ModifyCamera : Command
    {
        /// <summary>
        /// Local camera position offset in units by X,Y,Z axes.
        /// </summary>
        [CommandParameter(optional: true)]
        public float?[] Offset { get => GetDynamicParameter<float?[]>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Local camera rotation by Z-axis in angle degrees (0.0 to 360.0 or -180.0 to 180.0).
        /// </summary>
        [CommandParameter(optional: true)]
        public float Rotation { get => GetDynamicParameter(0f); set => SetDynamicParameter(value); }
        /// <summary>
        /// Relatize camera zoom (orthographic size or field of view, depending on the render mode), in 0.0 to 1.0 range.
        /// </summary>
        [CommandParameter(optional: true)]
        public float Zoom { get => GetDynamicParameter(0f); set => SetDynamicParameter(value); }
        /// <summary>
        /// Whether the camera should render in orthographic (true) or perspective (false) mode.
        /// </summary>
        [CommandParameter("ortho", true)]
        public bool? Orthographic { get => GetDynamicParameter<bool?>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Names of the components to toggle (enable if disabled and vice-versa). The components should be attached to the same gameobject as the camera.
        /// This can be used to toggle [custom post-processing effects](/guide/special-effects.md#camera-effects).
        /// </summary>
        [CommandParameter("toggle", true)]
        public string[] ToggleTypeNames { get => GetDynamicParameter<string[]>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Name of the easing function to use for the modification.
        /// <br/><br/>
        /// Available options: Linear, SmoothStep, Spring, EaseInQuad, EaseOutQuad, EaseInOutQuad, EaseInCubic, EaseOutCubic, EaseInOutCubic, EaseInQuart, EaseOutQuart, EaseInOutQuart, EaseInQuint, EaseOutQuint, EaseInOutQuint, EaseInSine, EaseOutSine, EaseInOutSine, EaseInExpo, EaseOutExpo, EaseInOutExpo, EaseInCirc, EaseOutCirc, EaseInOutCirc, EaseInBounce, EaseOutBounce, EaseInOutBounce, EaseInBack, EaseOutBack, EaseInOutBack, EaseInElastic, EaseOutElastic, EaseInOutElastic.
        /// <br/><br/>
        /// When not specified, will use a default easing function set in the camera configuration settings.
        /// </summary>
        [CommandParameter("easing", true)]
        public string EasingTypeName { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var camera = Engine.GetService<CameraManager>();

            var easingType = camera.DefaultEasintType;
            if (!string.IsNullOrEmpty(EasingTypeName) && !Enum.TryParse(EasingTypeName, true, out easingType))
                Debug.LogWarning($"Failed to parse `{EasingTypeName}` easing.");

            if (Orthographic.HasValue)
                camera.Camera.orthographic = Orthographic.Value;

            if (ToggleTypeNames != null)
                foreach (var name in ToggleTypeNames)
                    ToggleComponent(name, camera.Camera.gameObject);

            await Task.WhenAll(
                    camera.ChangeOffsetAsync(ArrayUtils.ToVector3(Offset, Vector3.zero), Duration, easingType, cancellationToken),
                    camera.ChangeRotationAsync(Rotation, Duration, easingType, cancellationToken),
                    camera.ChangeZoomAsync(Zoom, Duration, easingType, cancellationToken)
                );
        }

        private void ToggleComponent (string componentName, GameObject obj)
        {
            var cmp = obj.GetComponent(componentName) as MonoBehaviour;
            if (!cmp)
            {
                Debug.LogWarning($"Naninovel script `{ScriptName}` at #{LineNumber}: Failed to toggle `{componentName}` camera component; the component is not found on the camera's gameobject.");
                return;
            }
            cmp.enabled = !cmp.enabled;
        }
    }
}
