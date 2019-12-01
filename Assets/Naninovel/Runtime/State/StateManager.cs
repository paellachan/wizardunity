// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Handles <see cref="IEngineService"/>-related and other engine persistent data de-/serialization.
    /// Provides API to save and load game state.
    /// </summary>
    [InitializeAtRuntime(1)] // Here settings for all the other services will be applied, so initialize at the end.
    public class StateManager : IEngineService
    {
        /// <summary>
        /// Invoked when a game load operation (<see cref="LoadGameAsync(string)"/> or <see cref="QuickLoadAsync"/>) is started.
        /// </summary>
        public event Action<GameSaveLoadArgs> OnGameLoadStarted;
        /// <summary>
        /// Invoked when a game load operation (<see cref="LoadGameAsync(string)"/> or <see cref="QuickLoadAsync"/>) is finished.
        /// </summary>
        public event Action<GameSaveLoadArgs> OnGameLoadFinished;
        /// <summary>
        /// Invoked when a game save operation (<see cref="SaveGameAsync(string)"/> or <see cref="QuickSaveAsync"/>) is started.
        /// </summary>
        public event Action<GameSaveLoadArgs> OnGameSaveStarted;
        /// <summary>
        /// Invoked when a game save operation (<see cref="SaveGameAsync(string)"/> or <see cref="QuickSaveAsync"/>) is finished.
        /// </summary>
        public event Action<GameSaveLoadArgs> OnGameSaveFinished;
        /// <summary>
        /// Invoked when a state reset operation (<see cref="ResetStateAsync(Func{Task}[])"/>) is started.
        /// </summary>
        public event Action OnResetStarted;
        /// <summary>
        /// Invoked when a state reset operation (<see cref="ResetStateAsync(Func{Task}[])"/>) is finished.
        /// </summary>
        public event Action OnResetFinished;
        /// <summary>
        /// Invoked when a state rollback operation is started.
        /// </summary>
        public event Action OnRollbackStarted;
        /// <summary>
        /// Invoked when a state rollback operation is finished.
        /// </summary>
        public event Action OnRollbackFinished;

        public GlobalStateMap GlobalState { get; private set; }
        public SettingsStateMap SettingsState { get; private set; }
        public readonly GameStateSlotManager GameStateSlotManager;
        public readonly GlobalStateSlotManager GlobalStateSlotManager;
        public readonly SettingsSlotManager SettingsSlotManager;
        public string LastQuickSaveSlotId => GameStateSlotManager.IndexToQuickSaveSlotId(1);
        public bool QuickLoadAvailable => GameStateSlotManager.SaveSlotExists(LastQuickSaveSlotId);
        public bool AnyGameSaveExists => GameStateSlotManager.AnySaveExists();
        public bool ResetStateOnLoad => config.ResetStateOnLoad;
        public bool RollbackInProgress => rollbackTaskQueue.Count > 0;

        private readonly StateConfiguration config;
        private readonly InputManager inputManager;
        private readonly StateRollbackStack rollbackStateStack;
        private readonly Queue<GameStateMap> rollbackTaskQueue = new Queue<GameStateMap>();
        private readonly WaitForEndOfFrame waitForFrame = new WaitForEndOfFrame();
        private readonly HashSet<Func<GameStateMap, Task>> onGameSerializeTasks = new HashSet<Func<GameStateMap, Task>>();
        private readonly HashSet<Func<GameStateMap, Task>> onGameDeserializeTasks = new HashSet<Func<GameStateMap, Task>>();

        public StateManager (StateConfiguration config, EngineConfiguration engineConfig, InputManager inputManager)
        {
            this.config = config;
            this.inputManager = inputManager;

            var allowUserRollback = config.StateRollbackMode == StateRollbackMode.Full || (config.StateRollbackMode == StateRollbackMode.Debug && Debug.isDebugBuild);
            var rollbackCapacity = allowUserRollback ? Mathf.Max(1, config.StateRollbackSteps) : 1; // One step is reserved for game save operations.
            rollbackStateStack = new StateRollbackStack(rollbackCapacity);

            var savesFolderPath = PathUtils.Combine(engineConfig.GeneratedDataPath, config.SaveFolderName);
            GameStateSlotManager = new GameStateSlotManager(savesFolderPath, config.SaveSlotMask, config.QuickSaveSlotMask, config.SaveSlotLimit, config.QuickSaveSlotLimit, config.BinarySaveFiles);
            GlobalStateSlotManager = new GlobalStateSlotManager(savesFolderPath, config.DefaultGlobalSlotId, config.BinarySaveFiles);
            SettingsSlotManager = new SettingsSlotManager(engineConfig.GeneratedDataPath, config.DefaultSettingsSlotId, false);
        }

        public async Task InitializeServiceAsync ()
        {
            SettingsState = await LoadSettingsAsync();
            GlobalState = await LoadGlobalStateAsync();

            Engine.GetService<ScriptPlayer>()?.AddPreExecutionTask(PushRollbackSnapshotAsync);

            if (inputManager?.Rollback != null)
                inputManager.Rollback.OnStart += HandleRollbackInputStart;
        }

        public void ResetService ()
        {
            rollbackStateStack.Clear();
        }

        public void DestroyService ()
        {
            if (config.StateRollbackMode != StateRollbackMode.Disabled)
            {
                Engine.GetService<ScriptPlayer>()?.RemovePreExecutionTask(PushRollbackSnapshotAsync);
                if (inputManager?.Rollback != null)
                    inputManager.Rollback.OnStart -= HandleRollbackInputStart;
            }
        }

        /// <summary>
        /// Adds an async task to invoke when serializing (saving) game state.
        /// Use <see cref="GameStateMap"/> to serialize arbitrary custom objects to the game save slot.
        /// </summary>
        public void AddOnGameSerializeTask (Func<GameStateMap, Task> task) => onGameSerializeTasks.Add(task);

        /// <summary>
        /// Removes an async task to invoke when serializing (saving) game state.
        /// </summary>
        public void RemoveOnGameSerializeTask (Func<GameStateMap, Task> task) => onGameSerializeTasks.Remove(task);

        /// <summary>
        /// Adds an async task to invoke when de-serializing (loading) game state.
        /// Use <see cref="GameStateMap"/> to deserialize previously serialized custom objects from the loaded game save slot.
        /// </summary>
        public void AddOnGameDeserializeTask (Func<GameStateMap, Task> task) => onGameDeserializeTasks.Add(task);

        /// <summary>
        /// Removes an async task to invoke when de-serializing (loading) game state.
        /// </summary>
        public void RemoveOnGameDeserializeTask (Func<GameStateMap, Task> task) => onGameDeserializeTasks.Remove(task);

        /// <summary>
        /// Saves current game state to the specified save slot.
        /// </summary>
        public async Task<GameStateMap> SaveGameAsync (string slotId)
        {
            if (rollbackStateStack.Count == 0 || rollbackStateStack.Peek() is null)
            {
                Debug.LogError("Failed to save game state: rollback stack is empty.");
                return null;
            }

            var quick = slotId.StartsWithFast(config.QuickSaveSlotMask.GetBefore("{"));

            OnGameSaveStarted?.Invoke(new GameSaveLoadArgs(slotId, quick));

            var state = new GameStateMap(rollbackStateStack.Peek());
            state.SaveDateTime = DateTime.Now;
            state.Thumbnail = Engine.GetService<CameraManager>().CaptureThumbnail();
            if (config.StateRollbackMode == StateRollbackMode.Full && config.SavedRollbackSteps > 0)
                state.RollbackStackJson = rollbackStateStack.ToJson(config.SavedRollbackSteps);

            await GameStateSlotManager.SaveAsync(slotId, state);

            // Also save global state on every game save.
            await SaveGlobalStateAsync();

            OnGameSaveFinished?.Invoke(new GameSaveLoadArgs(slotId, quick));

            return state;
        }

        /// <summary>
        /// Saves current game state to the first quick save slot.
        /// Will shift the quick save slots chain by one index before saving.
        /// </summary>
        public async Task<GameStateMap> QuickSaveAsync ()
        {
            GameStateSlotManager.ShiftQuickSaveSlots();
            var firstSlotId = string.Format(config.QuickSaveSlotMask, 1);
            return await SaveGameAsync(firstSlotId);
        }

        /// <summary>
        /// Loads game state from the specified save slot.
        /// Will reset the engine services and unload unused assets before load.
        /// </summary>
        public async Task<GameStateMap> LoadGameAsync (string slotId)
        {
            if (string.IsNullOrEmpty(slotId) || !GameStateSlotManager.SaveSlotExists(slotId))
            {
                Debug.LogError($"Slot '{slotId}' not found when loading '{typeof(GameStateMap)}' data.");
                return null;
            }

            var quick = slotId.EqualsFast(LastQuickSaveSlotId);

            OnGameLoadStarted?.Invoke(new GameSaveLoadArgs(slotId, quick));

            if (config.LoadStartDelay > 0)
                await new WaitForSeconds(config.LoadStartDelay);

            Engine.Reset();
            await Resources.UnloadUnusedAssets();

            var state = await GameStateSlotManager.LoadAsync(slotId) as GameStateMap;
            await LoadAllServicesFromStateAsync<IStatefulService<GameStateMap>, GameStateMap>(state);

            if (config.StateRollbackMode == StateRollbackMode.Full && config.SavedRollbackSteps > 0)
                rollbackStateStack.OverrideFromJson(state.RollbackStackJson);

            foreach (var task in onGameDeserializeTasks)
                await task(state);

            OnGameLoadFinished?.Invoke(new GameSaveLoadArgs(slotId, quick));

            return state;
        }

        /// <summary>
        /// Loads game state from the most recent quick save slot.
        /// </summary>
        public async Task<GameStateMap> QuickLoadAsync () => await LoadGameAsync(LastQuickSaveSlotId);

        /// <summary>
        /// Serializes (saves) global state of the engine services.
        /// </summary>
        public async Task<GlobalStateMap> SaveGlobalStateAsync ()
        {
            await SaveAllServicesToStateAsync<IStatefulService<GlobalStateMap>, GlobalStateMap>(GlobalState);
            await GlobalStateSlotManager.SaveAsync(config.DefaultGlobalSlotId, GlobalState);
            return GlobalState;
        }

        /// <summary>
        /// Serializes (saves) settings state of the engine services.
        /// </summary>
        public async Task<SettingsStateMap> SaveSettingsAsync ()
        {
            await SaveAllServicesToStateAsync<IStatefulService<SettingsStateMap>, SettingsStateMap>(SettingsState);
            await SettingsSlotManager.SaveAsync(config.DefaultSettingsSlotId, SettingsState);
            return SettingsState;
        }

        /// <summary>
        /// Resets all the engine services and unloads unused assets; will basically revert to an empty initial engine state.
        /// The operation will invoke default on-load events, allowing to mask the process with a loading screen.
        /// </summary>
        /// <param name="additionalTasks">Additional tasks to perform during the reset (will be performed in order after the reset).</param>
        public async Task ResetStateAsync (params Func<Task>[] additionalTasks)
        {
            OnResetStarted?.Invoke();

            if (config.LoadStartDelay > 0)
                await new WaitForSeconds(config.LoadStartDelay);

            Engine.Reset();
            await Resources.UnloadUnusedAssets();

            if (additionalTasks != null)
            {
                foreach (var task in additionalTasks)
                    await task?.Invoke();
            }

            OnResetFinished?.Invoke();
        }

        /// <summary>
        /// Attempts to rollback (revert) all the engine services to a state they had at the provided playback spot. 
        /// Has no effect when the rollback feature is disabled.
        /// </summary>
        /// <param name="playbackSpot">The playback spot to revert to.</param>
        /// <returns>Whether the provided playback spot was found in the rollback stack and the operation succeeded.</returns>
        public async Task<bool> RollbackAsync (PlaybackSpot playbackSpot)
        {
            var state = rollbackStateStack.Pop(playbackSpot);
            if (state is null) return false;

            await RollbackToStateAsync(state);
            return true;
        }

        /// <summary>
        /// Attempts to rollback (revert) all the engine services to a state they had at the previous rollback step. 
        /// Has no effect when the rollback feature is disabled.
        /// </summary>
        /// <returns>Whether the provided the operation succeeded.</returns>
        public async Task<bool> RollbackAsync ()
        {
            if (rollbackStateStack.Capacity <= 1) return false;

            var state = rollbackStateStack.Pop();
            if (state is null) return false;

            await RollbackToStateAsync(state);
            return true;
        }

        private async Task RollbackToStateAsync (GameStateMap state)
        {
            rollbackTaskQueue.Enqueue(state);
            OnRollbackStarted?.Invoke();

            while (rollbackTaskQueue.Peek() != state)
                await waitForFrame;

            await LoadAllServicesFromStateAsync<IStatefulService<GameStateMap>, GameStateMap>(state);

            foreach (var task in onGameDeserializeTasks)
                await task(state);

            rollbackTaskQueue.Dequeue();
            OnRollbackFinished?.Invoke();
        }

        private async Task<GlobalStateMap> LoadGlobalStateAsync ()
        {
            var stateData = await GlobalStateSlotManager.LoadOrDefaultAsync(config.DefaultGlobalSlotId);
            await LoadAllServicesFromStateAsync<IStatefulService<GlobalStateMap>, GlobalStateMap>(stateData);
            return stateData;
        }

        private async Task<SettingsStateMap> LoadSettingsAsync ()
        {
            var settingsData = await SettingsSlotManager.LoadOrDefaultAsync(config.DefaultSettingsSlotId);
            await LoadAllServicesFromStateAsync<IStatefulService<SettingsStateMap>, SettingsStateMap>(settingsData);
            return settingsData;
        }

        private async Task SaveAllServicesToStateAsync<TService, TState> (TState state) 
            where TService : class, IStatefulService<TState>
            where TState : StateMap, new()
        {
            foreach (var service in Engine.GetAllServices<TService>())
                await service.SaveServiceStateAsync(state);
        }

        private async Task LoadAllServicesFromStateAsync<TService, TState> (TState state)
            where TService : class, IStatefulService<TState>
            where TState : StateMap, new()
        {
            foreach (var service in Engine.GetAllServices<TService>())
                await service.LoadServiceStateAsync(state);
        }

        private async Task PushRollbackSnapshotAsync (Commands.Command executedCommand)
        {
            var state = new GameStateMap();
            state.SaveDateTime = DateTime.Now;

            await SaveAllServicesToStateAsync<IStatefulService<GameStateMap>, GameStateMap>(state);

            foreach (var task in onGameSerializeTasks)
                await task(state);

            rollbackStateStack.Push(state);
        }

        private async void HandleRollbackInputStart ()
        {
            if (config.StateRollbackMode == StateRollbackMode.Disabled) return;
            if (config.StateRollbackMode == StateRollbackMode.Debug && !Debug.isDebugBuild) return;

            await RollbackAsync();
        }
    } 
}
