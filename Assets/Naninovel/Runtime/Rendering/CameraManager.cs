// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages cameras and other systems required for scene rendering.
    /// </summary>
    [InitializeAtRuntime(-1)] // Add camera at the start, so the user could see something while waiting for the engine init.
    public class CameraManager : IStatefulService<GameStateMap>
    {
        [Serializable]
        private class GameState
        {
            [Serializable]
            public struct CameraComponent
            {
                public string TypeName;
                public bool Enabled;

                public CameraComponent (MonoBehaviour comp)
                {
                    TypeName = comp.GetType().Name;
                    Enabled = comp.enabled;
                }
            }

            public float OrthoSize = -1f;
            public Vector3 Offset = Vector3.zero;
            public float Rotation = 0f;
            public float Zoom = 0f;
            public bool Orthographic = true;
            public CameraLookController.State LookMode = default;
            public CameraComponent[] CameraComponents;
        }

        public event Action<float> OnAspectChanged;

        public Camera Camera { get; private set; }
        public Camera UICamera { get; private set; }
        public float ScreenAspect => (float)Screen.width / Screen.height;
        /// <summary>
        /// Whether the UI is being rendered by <see cref="UICamera"/>.
        /// </summary>
        public bool UsingUICamera => config.UseUICamera;
        public bool RenderUI
        {
            get => UsingUICamera ? UICamera.enabled : MaskUtils.GetLayer(Camera.cullingMask, uiLayer);
            set { if (UsingUICamera) UICamera.enabled = value; else Camera.cullingMask = MaskUtils.SetLayer(Camera.cullingMask, uiLayer, value); }
        }
        public FullScreenMode ScreenMode => Screen.fullScreenMode;
        public Vector2Int Resolution => new Vector2Int(Screen.width, Screen.height);
        public int RefreshRate => Screen.currentResolution.refreshRate;
        public int ResolutionIndex => GetCurrentResolutionIndex();
        public Vector2 ReferenceResolution => config.ReferenceResolution;
        public float ReferenceAspect => ReferenceResolution.x / ReferenceResolution.y;
        public float MaxOrthoSize => ReferenceResolution.x / ReferenceAspect / 200f;
        public float PixelsPerUnit => ReferenceResolution.y / (MaxOrthoSize * 2f);
        public Vector2 ReferenceSize => ReferenceResolution / PixelsPerUnit;
        public EasingType DefaultEasintType => config.DefaultEasing;
        public Vector3 InitialPosition => config.InitialPosition;
        /// <summary>
        /// Local camera position offset in units by X and Y axis relative to the initial position set in the configuration.
        /// </summary>
        public Vector3 Offset
        {
            get => offset;
            set { CompleteOffsetTween(); offset = value; ApplyOffset(value); }
        }
        /// <summary>
        /// Local camera rotation by Z-axis in angle degrees.
        /// </summary>
        public float Rotation
        {
            get => rotation;
            set { CompleteRotationTween(); rotation = value; ApplyRotation(value); }
        }
        /// <summary>
        /// Relatize camera zoom (orthographic size or FOV depending on <see cref="Orthographic"/>), in 0.0 to 1.0 range.
        /// </summary>
        public float Zoom
        {
            get => zoom;
            set { CompleteZoomTween(); zoom = value; ApplyZoom(value); }
        }
        /// <summary>
        /// Whether the camera should render in orthographic (true) or perspective (false) mode.
        /// </summary>
        public bool Orthographic
        {
            get => Camera.orthographic;
            set { Camera.orthographic = value; Zoom = Zoom; }
        }
        /// <summary>
        /// Current camera orthographic size. Use setter of this property instead of the camera's ortho size to respect current zoom level.
        /// </summary>
        public float OrthoSize { get => orthoSize; set { ApplyOrthoSizeZoomAware(value, Zoom); orthoSize = value; } }

        private readonly CameraConfiguration config;
        private readonly InputManager inputManager;
        private readonly IEngineBehaviour engineBehaviour;
        private readonly RenderTexture thumbnailRenderTexture;
        private readonly List<MonoBehaviour> cameraComponentsCache = new List<MonoBehaviour>();
        private CameraLookController lookController;
        private ProxyBehaviour proxyBehaviour;
        private float lastAspect;
        private float orthoSize;
        private Vector3 offset = Vector3.zero;
        private float rotation = 0f, zoom = 0f;
        private Tweener<VectorTween> offsetTweener;
        private Tweener<FloatTween> rotationTweener, zoomTweener;
        private int uiLayer;

        public CameraManager (CameraConfiguration config, InputManager inputManager, IEngineBehaviour engineBehaviour)
        {
            this.config = config;
            this.inputManager = inputManager;
            this.engineBehaviour = engineBehaviour;

            thumbnailRenderTexture = new RenderTexture(config.ThumbnailResolution.x, config.ThumbnailResolution.y, 24);
        }

        public Task InitializeServiceAsync ()
        {
            // Do it here and not in ctor to allow camera initialize first.
            // Otherwise, when starting the game, for a moment, no cameras will be available for render.
            uiLayer = Engine.GetService<UIManager>().ObjectLayer;

            if (ObjectUtils.IsValid(config.CustomCameraPrefab))
                Camera = Engine.Instantiate(config.CustomCameraPrefab);
            else
            {
                Camera = Engine.CreateObject<Camera>(nameof(CameraManager));
                Camera.depth = 0;
                Camera.backgroundColor = new Color32(35, 31, 32, 255);
                Camera.orthographic = config.Orthographic;
                if (!UsingUICamera)
                    Camera.allowHDR = false; // Otherwise text artifacts appear when printing.
                if (Engine.OverrideObjectsLayer) // When culling is enabled, render only the engine object and UI (when not using UI camera) layers.
                    Camera.cullingMask = UsingUICamera ? (1 << Engine.ObjectsLayer) : ((1 << Engine.ObjectsLayer) | (1 << uiLayer));
                else if (UsingUICamera) Camera.cullingMask = ~(1 << uiLayer);
            }
            Camera.transform.position = config.InitialPosition;

            if (UsingUICamera)
            {
                if (ObjectUtils.IsValid(config.CustomUICameraPrefab))
                    UICamera = Engine.Instantiate(config.CustomUICameraPrefab);
                else
                {
                    UICamera = Engine.CreateObject<Camera>("UICamera");
                    UICamera.depth = 1;
                    UICamera.orthographic = true;
                    UICamera.allowHDR = false; // Otherwise text artifacts appear when printing.
                    UICamera.cullingMask = 1 << uiLayer;
                    UICamera.clearFlags = CameraClearFlags.Depth;
                }
                UICamera.transform.position = config.InitialPosition;
            }

            proxyBehaviour = Camera.gameObject.AddComponent<ProxyBehaviour>();
            offsetTweener = new Tweener<VectorTween>(proxyBehaviour);
            rotationTweener = new Tweener<FloatTween>(proxyBehaviour);
            zoomTweener = new Tweener<FloatTween>(proxyBehaviour);

            lastAspect = ScreenAspect;
            if (config.AutoCorrectOrthoSize)
                CorrectOrthoSize(lastAspect);
            else OrthoSize = config.DefaultOrthoSize;

            lookController = new CameraLookController(this, inputManager.CameraLookX, inputManager.CameraLookY);

            engineBehaviour.OnBehaviourLateUpdate += MonitorAspect;
            engineBehaviour.OnBehaviourUpdate += lookController.Update;

            return Task.CompletedTask;
        }

        public void ResetService ()
        {
            lookController.Enabled = false;
            Offset = Vector3.zero;
            Rotation = 0f;
            Zoom = 0f;
            Orthographic = config.Orthographic;
        }

        public void DestroyService ()
        {
            engineBehaviour.OnBehaviourLateUpdate -= MonitorAspect;
            engineBehaviour.OnBehaviourUpdate -= lookController.Update;

            ObjectUtils.DestroyOrImmediate(thumbnailRenderTexture);
            if (ObjectUtils.IsValid(Camera))
                ObjectUtils.DestroyOrImmediate(Camera.gameObject);
            if (ObjectUtils.IsValid(UICamera))
                ObjectUtils.DestroyOrImmediate(UICamera.gameObject);
        }

        public Task SaveServiceStateAsync (GameStateMap stateMap)
        {
            Camera.gameObject.GetComponents(cameraComponentsCache);
            var gameState = new GameState() {
                OrthoSize = OrthoSize,
                Offset = Offset,
                Rotation = Rotation,
                Zoom = Zoom,
                Orthographic = Orthographic,
                LookMode = lookController.GetState(),
                // Why one? Camera is not a MonoBehaviour, so only count ProxyBehaviour; others are considered to be custom effect.
                CameraComponents = cameraComponentsCache.Count > 1 ? cameraComponentsCache.Select(c => new GameState.CameraComponent(c)).ToArray() : null
            };
            stateMap.SetState(gameState);
            return Task.CompletedTask;
        }

        public Task LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            if (state is null)
            {
                ResetService();
                return Task.CompletedTask;
            }

            if (state.OrthoSize > 0) OrthoSize = state.OrthoSize;
            Offset = state.Offset;
            Rotation = state.Rotation;
            Zoom = state.Zoom;
            Orthographic = state.Orthographic;
            SetLookMode(state.LookMode.Enabled, state.LookMode.Zone, state.LookMode.Speed, state.LookMode.Gravity);

            if (state.CameraComponents != null)
                foreach (var compState in state.CameraComponents)
                {
                    var comp = Camera.gameObject.GetComponent(compState.TypeName) as MonoBehaviour;
                    if (!comp) continue;
                    comp.enabled = compState.Enabled;
                }

            return Task.CompletedTask;
        }

        public void SetResolution (Vector2Int resolution, FullScreenMode screenMode, int refreshRate)
        {
            Screen.SetResolution(resolution.x, resolution.y, screenMode, refreshRate);
        }

        /// <summary>
        /// Activates/disables camera look mode, when player can offset the main camera with input devices 
        /// (eg, by moving a mouse or using gamepad analog stick).
        /// </summary>
        public void SetLookMode (bool enabled, Vector2 lookZone, Vector2 lookSpeed, bool gravity)
        {
            lookController.Enabled = enabled;
            lookController.LookZone = lookZone;
            lookController.LookSpeed = lookSpeed;
            lookController.Gravity = gravity;
        }

        public Texture2D CaptureThumbnail ()
        {
            if (config.HideUIInThumbnails)
                RenderUI = false;

            // Hide the save-load menu in case it's visible.
            var saveLoadUI = Engine.GetService<UIManager>()?.GetUI<UI.ISaveLoadUI>();
            var saveLoadUIWasVisible = saveLoadUI?.IsVisible;
            if (saveLoadUIWasVisible.HasValue && saveLoadUIWasVisible.Value)
                saveLoadUI.IsVisible = false;

            // Confirmation UI may still be visible here (due to a fade-out time); force-hide it.
            var confirmUI = Engine.GetService<UIManager>()?.GetUI<UI.IConfirmationUI>();
            var confirmUIWasVisible = confirmUI?.IsVisible ?? false;
            if (confirmUI != null) confirmUI.IsVisible = false;

            var initialRenderTexture = Camera.targetTexture;
            Camera.targetTexture = thumbnailRenderTexture;
            Camera.Render();
            Camera.targetTexture = initialRenderTexture;

            if (RenderUI && UsingUICamera)
            {
                initialRenderTexture = UICamera.targetTexture;
                UICamera.targetTexture = thumbnailRenderTexture;
                UICamera.Render();
                UICamera.targetTexture = initialRenderTexture;
            }

            var thumbnail = thumbnailRenderTexture.ToTexture2D();

            // Restore the save-load menu and confirmation UI in case we hid them.
            if (saveLoadUIWasVisible.HasValue && saveLoadUIWasVisible.Value)
                saveLoadUI.IsVisible = true;
            if (confirmUIWasVisible)
                confirmUI.IsVisible = true;

            if (config.HideUIInThumbnails)
                RenderUI = true;

            return thumbnail;
        }

        public async Task ChangeOffsetAsync (Vector3 offset, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            CompleteOffsetTween();

            if (duration > 0)
            {
                var currentOffset = this.offset;
                this.offset = offset;
                var tween = new VectorTween(currentOffset, offset, duration, ApplyOffset, false, easingType);
                await offsetTweener.RunAsync(tween, cancellationToken);
            }
            else Offset = offset;
        }

        public async Task ChangeRotationAsync (float rotation, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            CompleteRotationTween();

            if (duration > 0)
            {
                var currentRotation = this.rotation;
                this.rotation = rotation;
                var tween = new FloatTween(currentRotation, rotation, duration, ApplyRotation, false, easingType);
                await rotationTweener.RunAsync(tween, cancellationToken);
            }
            else Rotation = rotation;
        }

        public async Task ChangeZoomAsync (float zoom, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            CompleteZoomTween();

            if (duration > 0)
            {
                var currentZoom = this.zoom;
                this.zoom = zoom;
                var tween = new FloatTween(currentZoom, zoom, duration, ApplyZoom, false, easingType);
                await zoomTweener.RunAsync(tween, cancellationToken);
            }
            else Zoom = zoom;
        }

        private void MonitorAspect ()
        {
            if (lastAspect != ScreenAspect)
            {
                OnAspectChanged?.Invoke(ScreenAspect);
                lastAspect = ScreenAspect;
                if (config.AutoCorrectOrthoSize)
                    CorrectOrthoSize(lastAspect);
            }
        }

        /// <summary>
        /// Changes current <see cref="OrthoSize"/> to accommodate the provided aspect ratio. 
        /// </summary>
        private void CorrectOrthoSize (float aspect)
        {
            OrthoSize = Mathf.Clamp(ReferenceResolution.x / aspect / 200f, 0f, MaxOrthoSize);
        }

        /// <summary>
        /// Sets the provided ortho size to the camera respecting the provided zoom level.
        /// </summary>
        private void ApplyOrthoSizeZoomAware (float size, float zoom)
        {
            Camera.orthographicSize = size * (1f - Mathf.Clamp(zoom, 0, .99f));
        }

        /// <summary>
        /// Finds index of the closest to the real (current) available (native to display) resolution.
        /// </summary>
        private int GetCurrentResolutionIndex ()
        {
            var currentResolution = new Resolution() { width = Resolution.x, height = Resolution.y, refreshRate = RefreshRate };
            var closestResolution = Screen.resolutions.Aggregate((x, y) => ResolutionDiff(x, currentResolution) < ResolutionDiff(y, currentResolution) ? x : y);
            return Array.IndexOf(Screen.resolutions, closestResolution);
        }

        private int ResolutionDiff (Resolution a, Resolution b)
        {
            return Mathf.Abs(a.width - b.width) + Mathf.Abs(a.height - b.height) + Mathf.Abs(a.refreshRate - b.refreshRate);
        }

        private void ApplyOffset (Vector3 offset)
        {
            Camera.transform.position = config.InitialPosition + offset;
        }

        private void ApplyRotation (float rotation)
        {
            Camera.transform.eulerAngles = new Vector3(0, 0, rotation);
        }

        private void ApplyZoom (float zoom)
        {
            if (Orthographic) ApplyOrthoSizeZoomAware(OrthoSize, zoom);
            else Camera.fieldOfView = Mathf.Lerp(5f, 60f, 1f - zoom);
        }

        private void CompleteOffsetTween ()
        {
            if (offsetTweener.IsRunning)
                offsetTweener.CompleteInstantly();
        }

        private void CompleteRotationTween ()
        {
            if (rotationTweener.IsRunning)
                rotationTweener.CompleteInstantly();
        }

        private void CompleteZoomTween ()
        {
            if (zoomTweener.IsRunning)
                zoomTweener.CompleteInstantly();
        }
    } 
}
