// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages objects spawned with <see cref="Commands.Spawn"/> and <see cref="Commands.SpawnFx"/> commands.
    /// </summary>
    [InitializeAtRuntime]
    public class SpawnManager : IStatefulService<GameStateMap>
    {
        public class SpawnedObject { public GameObject Object; public SpawnedObjectState State; }

        [System.Serializable]
        private class GameState { public List<SpawnedObjectState> SpawnedObjects; }

        private List<SpawnedObject> spawnedObjects;
        private ResourceProviderManager providersManager;
        private ResourceLoader<GameObject> loader;

        public SpawnManager (ResourceProviderManager providersManager)
        {
            spawnedObjects = new List<SpawnedObject>();
            this.providersManager = providersManager;
        }

        public Task InitializeServiceAsync ()
        {
            loader = new ResourceLoader<GameObject>(new[] { providersManager.GetProvider(ResourceProviderType.Project) });
            return Task.CompletedTask;
        }

        public void ResetService ()
        {
            DestroyAllSpawnedObjects();
        }

        public void DestroyService ()
        {
            DestroyAllSpawnedObjects();
        }

        public Task SaveServiceStateAsync (GameStateMap stateMap)
        {
            var state = new GameState() {
                SpawnedObjects = spawnedObjects.Select(o => o.State).ToList()
            };
            stateMap.SetState(state);
            return Task.CompletedTask;
        }

        public Task LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            if (state?.SpawnedObjects?.Count > 0)
            {
                if (spawnedObjects.Count > 0)
                    foreach (var obj in spawnedObjects.ToList())
                        if (!state.SpawnedObjects.Exists(o => o.Path.EqualsFast(obj.State.Path)))
                            DestroySpawnedObject(obj.State.Path);

                foreach (var objState in state.SpawnedObjects)
                    if (!IsObjectSpawned(objState.Path))
                        SpawnAsync(objState.Path, default, objState.Params).WrapAsync();
            }
            else if (spawnedObjects.Count > 0) DestroyAllSpawnedObjects();
            return Task.CompletedTask;
        }

        public async Task HoldResourcesAsync (object holder, string path)
        {
            var resource = await loader.LoadAsync(path);
            if (resource.IsValid)
                resource.Hold(holder);
        }

        public void ReleaseResources (object holder, string path)
        {
            if (!loader.IsLoaded(path)) return;

            var resource = loader.GetLoadedOrNull(path);
            resource.Release(holder, false);
            if (resource.HoldersCount == 0)
            {
                if (IsObjectSpawned(path))
                    DestroySpawnedObject(path);
                resource.Provider.UnloadResource(resource.Path);
            }
        }

        /// <summary>
        /// Attempts to spawn a <see cref="GameObject"/> based on the prefab stored at the provided path.
        /// Used by <see cref="Commands.Spawn"/> command.
        /// </summary>
        /// <returns>Spawned object or null if not spawned.</returns>
        public async Task<SpawnedObject> SpawnAsync (string path, CancellationToken cancellationToken = default, params string[] parameters)
        {
            if (IsObjectSpawned(path))
            {
                Debug.LogWarning($"Object `{path}` is already spawned and can't be spawned again before it's destroyed.");
                return null;
            }

            var prefabResource = await loader.LoadAsync(path);
            if (!prefabResource.IsValid)
            {
                Debug.LogWarning($"Failed to spawn '{path}': resource is not valid.");
                return null;
            }

            prefabResource.Hold(this);

            var obj = Engine.Instantiate(prefabResource.Object, path);

            var spawnedObj = new SpawnedObject { Object = obj, State = new SpawnedObjectState(path, parameters) };
            spawnedObjects.Add(spawnedObj);

            var parameterized = obj.GetComponent<Commands.Spawn.IParameterized>();
            if (parameterized != null) parameterized.SetSpawnParameters(parameters);

            var awaitable = obj.GetComponent<Commands.Spawn.IAwaitable>();
            if (awaitable != null) await awaitable.AwaitSpawnAsync(cancellationToken);

            return spawnedObj;
        }

        /// <summary>
        /// Attempts to destroy a previously spawned <see cref="GameObject"/> with the provided path.
        /// Used by <see cref="Commands.DestroySpawned"/> command.
        /// </summary>
        /// <returns>Whether the object was found and destroyed.</returns>
        public async Task<bool> DestroySpawnedAsync (string path, CancellationToken cancellationToken = default, params string[] parameters)
        {
            var spawnedObj = GetSpawnedObject(path);
            if (spawnedObj is null)
            {
                Debug.LogWarning($"Failed to destroy spawned object '{path}': the object is not found.");
                return false;
            }

            var parameterized = spawnedObj.Object.GetComponent<Commands.DestroySpawned.IParameterized>();
            if (parameterized != null) parameterized.SetDestroyParameters(parameters);

            var awaitable = spawnedObj.Object.GetComponent<Commands.DestroySpawned.IAwaitable>();
            if (awaitable != null) await awaitable.AwaitDestroyAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested) return false;

            return DestroySpawnedObject(path);
        }

        public bool DestroySpawnedObject (string path)
        {
            var spawnedObj = GetSpawnedObject(path);
            if (spawnedObj is null)
            {
                Debug.LogWarning($"Failed to destroy spawned object '{path}': the object is not found.");
                return false;
            }

            var removed = spawnedObjects?.Remove(spawnedObj);
            ObjectUtils.DestroyOrImmediate(spawnedObj.Object);

            loader.GetLoadedOrNull(path)?.Release(this);

            return removed ?? false;
        }

        public void DestroyAllSpawnedObjects ()
        {
            foreach (var spawnedObj in spawnedObjects)
                ObjectUtils.DestroyOrImmediate(spawnedObj.Object);
            spawnedObjects.Clear();

            loader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
        }

        public bool IsObjectSpawned (string path)
        {
            return spawnedObjects?.Exists(o => o.State.Path.EqualsFast(path)) ?? false;
        }

        public SpawnedObject GetSpawnedObject (string path)
        {
            return spawnedObjects?.FirstOrDefault(o => o.State.Path.EqualsFast(path));
        }
    }
}
