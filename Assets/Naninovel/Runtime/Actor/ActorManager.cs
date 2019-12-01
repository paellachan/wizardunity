// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages <typeparamref name="TActor"/> objects.
    /// </summary>
    /// <typeparam name="TActor">Type of managed actors.</typeparam>
    /// <typeparam name="TState">Type of state describing managed actors.</typeparam>
    /// <typeparam name="TMeta">Type of metadata required to construct managed actors.</typeparam>
    /// <typeparam name="TConfig">Type of the service configuration.</typeparam>
    public abstract class ActorManager<TActor, TState, TMeta, TConfig> : IActorManager, IStatefulService<GameStateMap>
        where TActor : IActor
        where TState : ActorState<TActor>, new()
        where TMeta : ActorMetadata
        where TConfig : ActorManagerConfiguration<TMeta>
    {
        [Serializable]
        private class GameState : ISerializationCallbackReceiver
        {
            public List<TState> ActorState { get; set; } = new List<TState>();

            [SerializeField] private List<string> actorStateJson = new List<string>();

            public virtual void OnBeforeSerialize ()
            {
                actorStateJson.Clear();
                foreach (var state in ActorState)
                {
                    var stateJson = state.ToJson();
                    actorStateJson.Add(stateJson);
                }
            }

            public virtual void OnAfterDeserialize ()
            {
                ActorState.Clear();
                foreach (var stateJson in actorStateJson)
                {
                    var state = new TState();
                    state.OverwriteFromJson(stateJson);
                    ActorState.Add(state);
                }
            }
        }

        public EasingType DefaultEasingType => Configuration.DefaultEasing;

        protected readonly TConfig Configuration;
        protected readonly Dictionary<string, TActor> ManagedActors;

        private static IEnumerable<Type> implementationTypes;

        private readonly Dictionary<string, TaskCompletionSource<TActor>> pendingAddActorTasks;

        static ActorManager ()
        {
            implementationTypes = ReflectionUtils.ExportedDomainTypes
                .Where(t => t.GetInterfaces().Contains(typeof(TActor)));
        }

        public ActorManager (TConfig config)
        {
            Configuration = config;
            ManagedActors = new Dictionary<string, TActor>(StringComparer.Ordinal);
            pendingAddActorTasks = new Dictionary<string, TaskCompletionSource<TActor>>();
        }

        public virtual Task InitializeServiceAsync () => Task.CompletedTask;

        public virtual void ResetService ()
        {
            RemoveAllActors();
        }

        public virtual void DestroyService ()
        {
            RemoveAllActors();
        }

        public virtual Task SaveServiceStateAsync (GameStateMap stateMap)
        {
            var state = new GameState();
            foreach (var kv in ManagedActors)
            {
                var actorState = new TState();
                actorState.OverwriteFromActor(kv.Value);
                state.ActorState.Add(actorState);
            }
            stateMap.SetState(state);
            return Task.CompletedTask;
        }

        public virtual async Task LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            if (state is null)
            {
                RemoveAllActors();
                return;
            }

            // Remove actors that doesn't exist in the serialized state.
            if (ManagedActors.Count > 0)
                foreach (var actorId in ManagedActors.Keys.ToList())
                    if (!state.ActorState.Exists(s => s.Id.EqualsFast(actorId)))
                        RemoveActor(actorId);

            foreach (var actorState in state.ActorState)
            {
                var actor = await GetOrAddActorAsync(actorState.Id);
                actorState.ApplyToActor(actor);
            }
        }

        /// <summary>
        /// Checks whether an actor with the provided ID is managed by the service. 
        /// </summary>
        public bool ActorExists (string actorId) => !string.IsNullOrEmpty(actorId) && ManagedActors.ContainsKey(actorId);

        /// <summary>
        /// Adds a new managed actor with the provided ID.
        /// </summary>
        public virtual async Task<TActor> AddActorAsync (string actorId)
        {
            if (ActorExists(actorId))
            {
                Debug.LogWarning($"Actor '{actorId}' was requested to be added, but it already exists.");
                return GetActor(actorId);
            }

            if (pendingAddActorTasks.ContainsKey(actorId))
                return await pendingAddActorTasks[actorId].Task;

            pendingAddActorTasks[actorId] = new TaskCompletionSource<TActor>();

            var constructedActor = await ConstructActorAsync(actorId);
            ManagedActors.Add(actorId, constructedActor);

            pendingAddActorTasks[actorId].SetResult(constructedActor);
            pendingAddActorTasks.Remove(actorId);

            return constructedActor;
        }

        /// <summary>
        /// Adds a new managed actor with the provided ID.
        /// </summary>
        async Task<IActor> IActorManager.AddActorAsync (string actorId) => await AddActorAsync(actorId);

        /// <summary>
        /// Adds a new managed actor with the provided state.
        /// </summary>
        public virtual async Task<TActor> AddActorAsync (TState state)
        {
            if (string.IsNullOrWhiteSpace(state?.Id))
            {
                Debug.LogWarning($"Can't add an actor with '{state}' state: actor name is undefined.");
                return default;
            }

            var actor = await AddActorAsync(state.Id);
            state.ApplyToActor(actor);
            return actor;
        }

        /// <summary>
        /// Retrieves a managed actor with the provided ID.
        /// </summary>
        public virtual TActor GetActor (string actorId)
        {
            if (!ActorExists(actorId))
            {
                Debug.LogError($"Can't find '{actorId}' actor.");
                return default;
            }

            return ManagedActors[actorId];
        }

        /// <summary>
        /// Retrieves a managed actor with the provided ID.
        /// </summary>
        IActor IActorManager.GetActor (string actorId) => GetActor(actorId);

        /// <summary>
        /// Returns a managed actor with the provided ID. If the actor doesn't exist, will add it.
        /// </summary>
        public virtual async Task<TActor> GetOrAddActorAsync (string actorId) => ActorExists(actorId) ? GetActor(actorId) : await AddActorAsync(actorId);

        /// <summary>
        /// Retrieves all the actors managed by the service.
        /// </summary>
        public virtual IEnumerable<TActor> GetAllActors () => ManagedActors?.Values;

        /// <summary>
        /// Retrieves all the actors managed by the service.
        /// </summary>
        IEnumerable<IActor> IActorManager.GetAllActors () => ManagedActors?.Values.Cast<IActor>();

        /// <summary>
        /// Removes a managed actor with the provided ID.
        /// </summary>
        public virtual void RemoveActor (string actorId)
        {
            if (!ActorExists(actorId)) return;
            var actor = GetActor(actorId);
            ManagedActors.Remove(actor.Id);
            (actor as IDisposable)?.Dispose();
        }

        /// <summary>
        /// Removes all the actors managed by the service.
        /// </summary>
        public virtual void RemoveAllActors ()
        {
            if (ManagedActors.Count == 0) return;
            var managedActors = GetAllActors().ToArray();
            for (int i = 0; i < managedActors.Length; i++)
                RemoveActor(managedActors[i].Id);
            ManagedActors.Clear();
        }

        /// <summary>
        /// Retrieves state of a managed actor with the provided ID.
        /// </summary>
        ActorState IActorManager.GetActorState (string actorId) => GetActorState(actorId);

        /// <summary>
        /// Retrieves state of a managed actor with the provided ID.
        /// </summary>
        public virtual TState GetActorState (string actorId)
        {
            if (!ActorExists(actorId))
            {
                Debug.LogError($"Can't retrieve state of a '{actorId}' actor: actor not found.");
                return default;
            }

            var actor = GetActor(actorId);
            var state = new TState();
            state.OverwriteFromActor(actor);
            return state;
        }

        /// <summary>
        /// Retrieves metadata of a managed actor with the provided ID.
        /// </summary>
        ActorMetadata IActorManager.GetActorMetadata (string actorId) => GetActorMetadata(actorId);

        /// <summary>
        /// Retrieves metadata of a managed actor with the provided ID.
        /// </summary>
        public TMeta GetActorMetadata (string actorId) => 
            Configuration.ActorMetadataMap.GetMetaById(actorId) ?? Configuration.DefaultActorMetadata;

        protected virtual async Task<TActor> ConstructActorAsync (string actorId)
        {
            var metadata = GetActorMetadata(actorId);

            var implementationType = implementationTypes.FirstOrDefault(t => t.FullName == metadata.Implementation);
            Debug.Assert(implementationType != null, $"`{metadata.Implementation}` actor implementation type for `{typeof(TActor).Name}` is not found.");

            var actor = (TActor)Activator.CreateInstance(implementationType, actorId, metadata);

            await actor.InitializeAsync();

            return actor;
        }
    } 
}
