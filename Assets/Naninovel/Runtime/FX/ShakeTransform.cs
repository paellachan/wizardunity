// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="Transform"/>.
    /// </summary>
    public abstract class ShakeTransform : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable
    {
        protected string SpawnedPath { get; private set; }
        protected string ObjectName { get; private set; }
        protected int ShakesCount { get; private set; }
        protected float ShakeDuration { get; private set; }
        protected float DurationVariation { get; private set; }
        protected float ShakeAmplitude { get; private set; }
        protected float AmplitudeVariation { get; private set; }
        protected bool ShakeHorizontally { get; private set; }
        protected bool ShakeVertically { get; private set; }
        protected SpawnManager SpawnManager => spawnManagerCache ?? (spawnManagerCache = Engine.GetService<SpawnManager>());

        [SerializeField] private int defaultShakesCount = 3;
        [SerializeField] private float defaultShakeDuration = .15f;
        [SerializeField] private float defaultDurationVariation = .25f;
        [SerializeField] private float defaultShakeAmplitude = .5f;
        [SerializeField] private float defaultAmplitudeVariation = .5f;
        [SerializeField] private bool defaultShakeHorizontally = false;
        [SerializeField] private bool defaultShakeVertically = true;

        private Tweener<VectorTween> positionTweener;

        private string spawnedPath;
        private Vector3 deltaPos, initialPos;
        private Transform shakedTransform;
        private bool loop;
        private SpawnManager spawnManagerCache;

        public virtual void SetSpawnParameters (string[] parameters)
        {
            SpawnedPath = gameObject.name;
            ObjectName = parameters?.ElementAtOrDefault(0);
            ShakesCount = Mathf.Abs(parameters?.ElementAtOrDefault(1)?.AsInvariantInt() ?? defaultShakesCount);
            ShakeDuration = Mathf.Abs(parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultShakeDuration);
            DurationVariation = Mathf.Clamp01(parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultDurationVariation);
            ShakeAmplitude = Mathf.Abs(parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultShakeAmplitude);
            AmplitudeVariation = Mathf.Clamp01(parameters?.ElementAtOrDefault(5)?.AsInvariantFloat() ?? defaultAmplitudeVariation);
            ShakeHorizontally = bool.Parse(parameters?.ElementAtOrDefault(6) ?? defaultShakeHorizontally.ToString());
            ShakeVertically = bool.Parse(parameters?.ElementAtOrDefault(7) ?? defaultShakeVertically.ToString());

            positionTweener = new Tweener<VectorTween>(this);
            loop = ShakesCount <= 0;
        }

        public async Task AwaitSpawnAsync (CancellationToken cancellationToken = default)
        {
            shakedTransform = GetShakedTransform();
            if (shakedTransform == null)
            {
                SpawnManager.DestroySpawnedObject(SpawnedPath);
                Debug.LogWarning($"Failed to apply `{GetType().Name}` FX to `{ObjectName}`: game object not found.");
                return;
            }

            initialPos = shakedTransform.position;
            deltaPos = new Vector3(ShakeHorizontally ? ShakeAmplitude : 0, ShakeVertically ? ShakeAmplitude : 0, 0);

            if (loop)
            {
                while (loop && Application.isPlaying && !cancellationToken.IsCancellationRequested)
                    await ShakeSequenceAsync(cancellationToken);
            }
            else
            {
                for (int i = 0; i < ShakesCount; i++)
                    await ShakeSequenceAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;

                if (SpawnManager.IsObjectSpawned(SpawnedPath))
                    SpawnManager.DestroySpawnedObject(SpawnedPath);
            }

            await new WaitForEndOfFrame(); // Otherwise a consequent shake won't work.
        }

        protected abstract Transform GetShakedTransform ();

        protected virtual async Task ShakeSequenceAsync (CancellationToken cancellationToken)
        {
            var amplitude = deltaPos + deltaPos * Random.Range(-AmplitudeVariation, AmplitudeVariation);
            var duration = ShakeDuration + ShakeDuration * Random.Range(-DurationVariation, DurationVariation);

            await MoveAsync(initialPos - amplitude * .5f, duration * .25f, cancellationToken);
            await MoveAsync(initialPos + amplitude, duration * .5f, cancellationToken);
            await MoveAsync(initialPos, duration * .25f, cancellationToken);
        }

        private async Task MoveAsync (Vector3 position, float duration, CancellationToken cancellationToken)
        {
            var tween = new VectorTween(shakedTransform.position, position, duration, pos => shakedTransform.position = pos, false, EasingType.SmoothStep);
            await positionTweener.RunAsync(tween, cancellationToken);
        }

        private void OnDestroy ()
        {
            loop = false;

            if (shakedTransform != null)
                shakedTransform.position = initialPos;

            if (SpawnManager.IsObjectSpawned(SpawnedPath))
                SpawnManager.DestroySpawnedObject(SpawnedPath);
        }
    }
}
