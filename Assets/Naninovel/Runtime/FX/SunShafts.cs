// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel.FX
{
    [RequireComponent(typeof(ParticleSystem))]
    public class SunShafts : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected float Intensity { get; private set; }
        protected float FadeInTime { get; private set; }
        protected float FadeOutTime { get; private set; }

        [SerializeField] private float defaultIntensity = .85f;
        [SerializeField] private float defaultFadeInTime = 3f;
        [SerializeField] private float defaultFadeOutTime = 3f;

        private ParticleSystem particles;
        private ParticleSystem.MainModule module;
        private Tweener<ColorTween> intensityTweener;

        public virtual void SetSpawnParameters (string[] parameters)
        {
            Intensity = parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultIntensity;
            FadeInTime = Mathf.Abs(parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultFadeInTime);
        }

        public async Task AwaitSpawnAsync (CancellationToken cancellationToken = default)
        {
            if (intensityTweener.IsRunning)
                intensityTweener.CompleteInstantly();

            var tween = new ColorTween(module.startColor.color, new Color(0, 0, 0, Intensity), ColorTweenMode.Alpha, FadeInTime, SetIntensity);
            await intensityTweener.RunAsync(tween, cancellationToken);
        }

        public void SetDestroyParameters (string[] parameters)
        {
            FadeOutTime = Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultFadeOutTime);
        }

        public async Task AwaitDestroyAsync (CancellationToken cancellationToken = default)
        {
            if (intensityTweener.IsRunning)
                intensityTweener.CompleteInstantly();

            var tween = new ColorTween(module.startColor.color, Color.clear, ColorTweenMode.Alpha, FadeOutTime, SetIntensity);
            await intensityTweener.RunAsync(tween, cancellationToken);
        }

        private void Awake ()
        {
            particles = GetComponent<ParticleSystem>();
            module = particles.main;
            intensityTweener = new Tweener<ColorTween>(this);
        }

        private void Start ()
        {
            // Position before the first background.
            var backgroundMngr = Engine.GetService<BackgroundManager>();
            transform.position = new Vector3(0, 0, backgroundMngr.ZOffset - 1);
        }

        private void SetIntensity (Color value) => module.startColor = value;
    }
}
