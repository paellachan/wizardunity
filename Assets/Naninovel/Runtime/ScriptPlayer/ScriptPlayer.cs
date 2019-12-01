// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
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
    /// Handles <see cref="Script"/> execution (playback).
    /// </summary>
    [InitializeAtRuntime]
    public class ScriptPlayer : IStatefulService<SettingsStateMap>, IStatefulService<GlobalStateMap>, IStatefulService<GameStateMap>
    {
        [Serializable]
        private class Settings
        {
            public PlayerSkipMode SkipMode = PlayerSkipMode.ReadOnly;
        }

        [Serializable]
        private class GlobalState
        {
            public PlayedScriptRegister PlayedScriptRegister = new PlayedScriptRegister();
        }

        [Serializable]
        private class GameState
        {
            public string PlayedScriptName;
            public int PlayedIndex;
            public bool IsWaitingForInput;
            public List<PlaybackSpot> LastGosubReturnSpots;
        }

        /// <summary>
        /// Event invoked when player starts playing a script.
        /// </summary>
        public event Action OnPlay;
        /// <summary>
        /// Event invoked when player stops playing a script.
        /// </summary>
        public event Action OnStop;
        /// <summary>
        /// Event invoked when player starts executing a <see cref="Command"/>.
        /// </summary>
        public event Action<Command> OnCommandExecutionStart;
        /// <summary>
        /// Event invoked when player finishes executing a <see cref="Command"/>.
        /// </summary>
        public event Action<Command> OnCommandExecutionFinish;
        /// <summary>
        /// Event invoked when skip mode changes.
        /// </summary>
        public event Action<bool> OnSkip;
        /// <summary>
        /// Event invoked when auto play mode changes.
        /// </summary>
        public event Action<bool> OnAutoPlay;
        /// <summary>
        /// Event invoked when waiting for input mode changes.
        /// </summary>
        public event Action<bool> OnWaitingForInput;

        /// <summary>
        /// Whether script playback routine is currently running.
        /// </summary>
        public bool IsPlaying => playRoutineCTS != null;
        /// <summary>
        /// Checks whether a follow-up command after the currently played one exists.
        /// </summary>
        public bool IsNextCommandAvailable => Playlist?.IsIndexValid(PlayedIndex + 1) ?? false;
        /// <summary>
        /// Whether skip mode is currently active.
        /// </summary>
        public bool IsSkipActive { get; private set; }
        /// <summary>
        /// Whether auto play mode is currently active.
        /// </summary>
        public bool IsAutoPlayActive { get; private set; }
        /// <summary>
        /// Whether user input is required to execute next script command.
        /// </summary>
        public bool IsWaitingForInput { get; private set; }
        /// <summary>
        /// Skip mode to use while <see cref="IsSkipActive"/>.
        /// </summary>
        public PlayerSkipMode SkipMode { get; set; }
        /// <summary>
        /// Currently played <see cref="Script"/>.
        /// </summary>
        public Script PlayedScript { get; private set; }
        /// <summary>
        /// Currently played <see cref="Command"/>.
        /// </summary>
        public Command PlayedCommand => Playlist?.GetCommandByIndex(PlayedIndex);
        /// <summary>
        /// Currently played <see cref="Naninovel.PlaybackSpot"/>.
        /// </summary>
        public PlaybackSpot PlaybackSpot => new PlaybackSpot(PlayedScript?.Name, PlayedCommand?.LineIndex ?? 0, PlayedCommand?.InlineIndex ?? 0);
        /// <summary>
        /// List of <see cref="Command"/> built upon the currently played <see cref="Script"/>.
        /// </summary>
        public ScriptPlaylist Playlist { get; private set; }
        /// <summary>
        /// Index of the currently played command inside the <see cref="Playlist"/>.
        /// </summary>
        public int PlayedIndex { get; private set; }
        /// <summary>
        /// Last playback return spots stack registered by <see cref="Gosub"/> commands.
        /// </summary>
        public Stack<PlaybackSpot> LastGosubReturnSpots { get; private set; }
        /// <summary>
        /// Total number of commands existing in all the available naninovel scripts.
        /// </summary>
        public int TotalCommandsCount { get; private set; }
        /// <summary>
        /// Total number of unique commands ever played by the player (global state scope).
        /// </summary>
        public int PlayedCommandsCount => playedScriptRegister.CountPlayed();

        private readonly ScriptPlayerConfiguration config;
        private readonly InputManager inputManager;
        private readonly ScriptManager scriptManager;
        private readonly StateManager stateManager;
        private readonly ResourceProviderManager providerManager;
        private readonly HashSet<Func<Command, Task>> preExecutionTasks = new HashSet<Func<Command, Task>>();
        private readonly HashSet<Func<Command, Task>> postExecutionTasks = new HashSet<Func<Command, Task>>();
        private PlayedScriptRegister playedScriptRegister;
        private CancellationTokenSource playRoutineCTS;
        private CancellationTokenSource commandExecutionCTS;
        private TaskCompletionSource<object> waitForWaitForInputDisabledTCS;

        public ScriptPlayer (ScriptPlayerConfiguration config, ScriptManager scriptManager, 
            InputManager inputManager, ResourceProviderManager providerManager, StateManager stateManager)
        {
            this.config = config;
            this.scriptManager = scriptManager;
            this.inputManager = inputManager;
            this.providerManager = providerManager;
            this.stateManager = stateManager;

            LastGosubReturnSpots = new Stack<PlaybackSpot>();
            playedScriptRegister = new PlayedScriptRegister();
            commandExecutionCTS = new CancellationTokenSource();
        }

        public async Task InitializeServiceAsync ()
        {
            if (inputManager?.Continue != null)
                inputManager.Continue.OnStart += DisableWaitingForInput;
            if (inputManager?.Skip != null)
            {
                inputManager.Skip.OnStart += EnableSkip;
                inputManager.Skip.OnEnd += DisableSkip;
            }
            if (inputManager?.AutoPlay != null)
                inputManager.AutoPlay.OnStart += ToggleAutoPlay;

            if (config.UpdateActionCountOnInit)
                TotalCommandsCount = await UpdateTotalActionCountAsync();
        }

        public void ResetService ()
        {
            Stop(true);
            Playlist?.ReleaseResources();
            Playlist = null;
            PlayedIndex = -1;
            PlayedScript = null;
            DisableWaitingForInput();
            DisableAutoPlay();
            DisableSkip();
        }

        public void DestroyService ()
        {
            ResetService();

            commandExecutionCTS?.Dispose();

            if (inputManager?.Continue != null)
                inputManager.Continue.OnStart -= DisableWaitingForInput;
            if (inputManager?.Skip != null)
            {
                inputManager.Skip.OnStart -= EnableSkip;
                inputManager.Skip.OnEnd -= DisableSkip;
            }
            if (inputManager?.AutoPlay != null)
                inputManager.AutoPlay.OnStart -= ToggleAutoPlay;
        }

        public Task SaveServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                SkipMode = SkipMode
            };
            stateMap.SetState(settings);
            return Task.CompletedTask;
        }

        public Task LoadServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>() ?? new Settings();
            SkipMode = settings.SkipMode;
            return Task.CompletedTask;
        }

        public Task SaveServiceStateAsync (GlobalStateMap stateMap)
        {
            var globalState = new GlobalState {
                PlayedScriptRegister = playedScriptRegister
            };
            stateMap.SetState(globalState);
            return Task.CompletedTask;
        }

        public Task LoadServiceStateAsync (GlobalStateMap stateMap)
        {
            var state = stateMap.GetState<GlobalState>() ?? new GlobalState();
            playedScriptRegister = state.PlayedScriptRegister;
            return Task.CompletedTask;
        }

        public Task SaveServiceStateAsync (GameStateMap stateMap)
        {
            var gameState = new GameState() {
                PlayedScriptName = PlayedScript?.Name,
                PlayedIndex = PlayedIndex,
                IsWaitingForInput = IsWaitingForInput,
                LastGosubReturnSpots = LastGosubReturnSpots.Count > 0 ? LastGosubReturnSpots.Reverse().ToList() : null // Stack is reversed on enum.
            };
            stateMap.PlaybackSpot = PlaybackSpot;
            stateMap.SetState(gameState);
            return Task.CompletedTask;
        }

        public async Task LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            if (state is null)
            {
                ResetService();
                return;
            }

            Stop(true);

            PlayedIndex = state.PlayedIndex;
            SetWaitingForInputActive(state.IsWaitingForInput);
            if (state.LastGosubReturnSpots != null && state.LastGosubReturnSpots.Count > 0)
                LastGosubReturnSpots = new Stack<PlaybackSpot>(state.LastGosubReturnSpots);
            else LastGosubReturnSpots.Clear();

            if (!string.IsNullOrEmpty(state.PlayedScriptName))
            {
                if (PlayedScript is null || !state.PlayedScriptName.EqualsFast(PlayedScript.Name))
                {
                    PlayedScript = await scriptManager.LoadScriptAsync(state.PlayedScriptName);
                    Playlist = new ScriptPlaylist(PlayedScript);
                    var endIndex = providerManager.ResourcePolicy == ResourcePolicy.Static ? Playlist.Count - 1 :
                        Mathf.Min(PlayedIndex + providerManager.DynamicPolicySteps, Playlist.Count - 1);
                    await Playlist.HoldResourcesAsync(PlayedIndex, endIndex);
                }

                // Start playback and force waiting for input to prevent looping same command when performing state rollback.
                if (stateManager.RollbackInProgress)
                {
                    SetWaitingForInputActive(true);
                    Play();
                }
            }
            else
            {
                Playlist.Clear();
                PlayedScript = null;
            }
        }

        public async Task<int> UpdateTotalActionCountAsync ()
        {
            TotalCommandsCount = 0;

            var scripts = await scriptManager.LoadAllScriptsAsync();
            foreach (var script in scripts)
            {
                var playlist = new ScriptPlaylist(script);
                TotalCommandsCount += playlist.Count;
            }

            return TotalCommandsCount;
        }

        /// <summary>
        /// Adds an async delegate to invoke before a command is going to be executed.
        /// </summary>
        public void AddPreExecutionTask (Func<Command, Task> taskFunc) => preExecutionTasks.Add(taskFunc);

        /// <summary>
        /// Removes an async delegate to invoke before a command is going to be executed.
        /// </summary>
        public void RemovePreExecutionTask (Func<Command, Task> taskFunc) => preExecutionTasks.Remove(taskFunc);

        /// <summary>
        /// Adds an async delegate to invoke after a command is executed.
        /// </summary>
        public void AddPostExecutionTask (Func<Command, Task> taskFunc) => postExecutionTasks.Add(taskFunc);

        /// <summary>
        /// Removes an async delegate to invoke after a command is executed.
        /// </summary>
        public void RemovePostExecutionTask (Func<Command, Task> taskFunc) => postExecutionTasks.Remove(taskFunc);

        /// <summary>
        /// Starts <see cref="PlayedScript"/> playback at <see cref="PlayedIndex"/>.
        /// </summary>
        public void Play () => Play(PlayedIndex);

        /// <summary>
        /// Starts <see cref="PlayedScript"/> playback at <paramref name="playlistIndex"/>.
        /// </summary>
        public void Play (int playlistIndex)
        {
            if (PlayedScript is null || Playlist is null)
            {
                Debug.LogError("Failed to start script playback: the script is not set.");
                return;
            }

            if (IsPlaying) Stop();

            PlayedIndex = playlistIndex;
            if (Playlist.IsIndexValid(PlayedIndex) || SelectNextCommand())
            {
                playRoutineCTS = new CancellationTokenSource();
                PlayRoutineAsync(playRoutineCTS.Token).WrapAsync();
                OnPlay?.Invoke();
            }
        }

        /// <summary>
        /// Starts playback of the provided script at the provided line and inline indexes.
        /// </summary>
        /// <param name="script">The script to play.</param>
        /// <param name="startLineIndex">Line index to start playback from.</param>
        /// <param name="startInlineIndex">Command inline index to start playback from.</param>
        public void Play (Script script, int startLineIndex = 0, int startInlineIndex = 0)
        {
            PlayedScript = script;

            if (Playlist is null || Playlist.ScriptName != script.Name)
                Playlist = new ScriptPlaylist(script);

            if (startLineIndex > 0 || startInlineIndex > 0)
            {
                var startAction = Playlist.GetFirstCommandAfterLine(startLineIndex, startInlineIndex);
                if (startAction is null) { Debug.LogError($"Script player failed to start: no commands found in script `{PlayedScript.Name}` at line #{startLineIndex}.{startInlineIndex}."); return; }
                PlayedIndex = Playlist.IndexOf(startAction);
            }
            else PlayedIndex = 0;

            Play();
        }

        /// <summary>
        /// Starts playback of the provided script at the provided label.
        /// </summary>
        /// <param name="script">The script to play.</param>
        /// <param name="label">Name of the label within provided script to start playback from.</param>
        public void Play (Script script, string label)
        {
            if (!script.LabelExists(label))
            {
                Debug.LogError($"Failed to jump to `{label}` label: label not found in `{script.Name}` script.");
                return;
            }

            Play(script, script.GetLineIndexForLabel(label));
        }

        /// <summary>
        /// Preloads the script's commands and starts playing.
        /// </summary>
        public async Task PreloadAndPlayAsync (Script script, int startLineIndex = 0, int startInlineIndex = 0)
        {
            Playlist = new ScriptPlaylist(script);
            var startAction = Playlist.GetFirstCommandAfterLine(startLineIndex, startInlineIndex);
            var startIndex = startAction != null ? Playlist.IndexOf(startAction) : 0;

            var endIndex = providerManager.ResourcePolicy == ResourcePolicy.Static ? Playlist.Count - 1 :
                Mathf.Min(startIndex + providerManager.DynamicPolicySteps, Playlist.Count - 1);
            await Playlist.HoldResourcesAsync(startIndex, endIndex);

            Play(script, startLineIndex, startInlineIndex);
        }

        /// <summary>
        /// Loads a script with the provided name, preloads the script's commands and starts playing.
        /// </summary>
        public async Task PreloadAndPlayAsync (string scriptName, int startLineIndex = 0, int startInlineIndex = 0, string label = null)
        {
            var script = await scriptManager.LoadScriptAsync(scriptName);
            if (script is null)
            {
                Debug.LogError($"Script player failed to start: script with name `{scriptName}` wasn't able to load.");
                return;
            }

            if (!string.IsNullOrEmpty(label))
            {
                if (!script.LabelExists(label))
                {
                    Debug.LogError($"Failed to jump to `{label}` label: label not found in `{script.Name}` script.");
                    return;
                }
                startLineIndex = script.GetLineIndexForLabel(label);
                startInlineIndex = 0;
            }

            await PreloadAndPlayAsync(script, startLineIndex, startInlineIndex);
        }

        /// <summary>
        /// Halts the playback of the currently played script.
        /// </summary>
        /// <param name="cancelCommands">
        /// Whether to also cancel any executing commands. Be aware that this could lead to an inconsistent state; 
        /// only use when the current engine state is going to be discarded (eg, when preparing to load a game or perform state rollback).
        /// </param>
        public void Stop (bool cancelCommands = false)
        {
            if (IsPlaying)
            {
                playRoutineCTS.Cancel();
                playRoutineCTS.Dispose();
                playRoutineCTS = null;
            }

            if (cancelCommands)
            {
                commandExecutionCTS.Cancel();
                commandExecutionCTS.Dispose();
                commandExecutionCTS = new CancellationTokenSource();
            }

            OnStop?.Invoke();
        }

        /// <summary>
        /// Depending on whether the provided <paramref name="lineIndex"/> being before or after currently played command' line index,
        /// performs a fast-forward playback or state rollback of the currently loaded script.
        /// </summary>
        /// <param name="lineIndex">The line index to rewind at.</param>
        /// <param name="inlineIndex">The inline index to rewind at.</param>
        /// <param name="resumePlayback">Whether to resume script playback after the rewind.</param>
        /// <returns>Whether the <paramref name="lineIndex"/> has been reached.</returns>
        public async Task<bool> RewindAsync (int lineIndex, int inlineIndex = 0, bool resumePlayback = true)
        {
            if (IsPlaying) Stop();

            if (PlayedCommand is null)
            {
                Debug.LogError("Script player failed to rewind: played command is not valid.");
                if (resumePlayback) Play();
                return false;
            }

            var targetCommand = Playlist.GetFirstCommandAfterLine(lineIndex, inlineIndex);
            if (targetCommand is null)
            {
                Debug.LogError($"Script player failed to rewind: target line index ({lineIndex}) is not valid for `{PlayedScript.Name}` script.");
                if (resumePlayback) Play();
                return false;
            }

            var targetPlaylistIndex = Playlist.IndexOf(targetCommand);
            if (targetPlaylistIndex == PlayedIndex)
            {
                if (resumePlayback) Play();
                return true;
            }

            DisableAutoPlay();
            DisableSkip();
            DisableWaitingForInput();

            playRoutineCTS = new CancellationTokenSource();
            var cancellationToken = playRoutineCTS.Token;

            bool result;
            if (targetPlaylistIndex > PlayedIndex)
            {
                result = await FastForwardRoutineAsync(cancellationToken, targetPlaylistIndex);
            }
            else
            {
                var targetSpot = new PlaybackSpot(PlayedScript.Name, lineIndex, inlineIndex);
                result = await stateManager.RollbackAsync(targetSpot);
            }

            if (resumePlayback) Play();
            else Stop();

            return result;
        }

        /// <summary>
        /// Attempts to <see cref="RewindAsync(int, int, bool)"/> to the next command in the current playlist.
        /// </summary>
        /// <param name="resumePlayback">Whether to resume script playback after the rewind.</param>
        /// <returns>True if the next command was available and is now played.</returns>
        public async Task<bool> RewindToNextCommandAsync (bool resumePlayback = true)
        {
            if (Playlist is null || PlayedCommand is null) return false;
            var nextCommand = Playlist.GetCommandByIndex(PlayedIndex + 1);
            if (nextCommand is null) return false;
            return await RewindAsync(nextCommand.LineIndex, nextCommand.InlineIndex, resumePlayback);
        }

        /// <summary>
        /// Attempts to <see cref="RewindAsync(int, int, bool)"/> to the previous command in the current playlist.
        /// </summary>
        /// <param name="resumePlayback">Whether to resume script playback after the rewind.</param>
        /// <returns>True if the previous command was available and is now played.</returns>
        public async Task<bool> RewindToPreviousCommandAsync (bool resumePlayback = true)
        {
            if (Playlist is null || PlayedCommand is null) return false;
            var prevCommand = Playlist.GetCommandByIndex(PlayedIndex - 1);
            if (prevCommand is null) return false;
            return await RewindAsync(prevCommand.LineIndex, prevCommand.InlineIndex, resumePlayback);
        }

        /// <summary>
        /// Checks whether <see cref="IsSkipActive"/> can be enabled at the moment.
        /// Result depends on <see cref="PlayerSkipMode"/> and currently played command.
        /// </summary>
        public bool IsSkipAllowed ()
        {
            if (SkipMode == PlayerSkipMode.Everything) return true;
            if (PlayedScript is null) return false;
            return playedScriptRegister.IsIndexPlayed(PlayedScript.Name, PlayedIndex);
        }

        /// <summary>
        /// Enables <see cref="IsSkipActive"/> when <see cref="IsSkipAllowed"/>.
        /// </summary>
        public void EnableSkip ()
        {
            if (!IsSkipAllowed()) return;
            SetSkipActive(true);
        }

        /// <summary>
        /// Disables <see cref="IsSkipActive"/>.
        /// </summary>
        public void DisableSkip () => SetSkipActive(false);

        public void EnableAutoPlay () => SetAutoPlayActive(true);

        public void DisableAutoPlay () => SetAutoPlayActive(false);

        public void ToggleAutoPlay ()
        {
            if (IsAutoPlayActive) DisableAutoPlay();
            else EnableAutoPlay();
        }

        public void EnableWaitingForInput ()
        {
            if (IsSkipActive) return;
            SetWaitingForInputActive(true);
        }

        public void DisableWaitingForInput () => SetWaitingForInputActive(false);

        private async Task WaitForWaitForInputDisabledAsync ()
        {
            if (waitForWaitForInputDisabledTCS is null)
                waitForWaitForInputDisabledTCS = new TaskCompletionSource<object>();
            await waitForWaitForInputDisabledTCS.Task;
        }

        private async Task WaitForAutoPlayDelayAsync ()
        {
            await new WaitForSeconds(config.MinAutoPlayDelay);
            if (!IsAutoPlayActive) await WaitForWaitForInputDisabledAsync(); // In case auto play was disabled while waiting for delay.
        }

        private async Task ExecutePlayedCommandAsync ()
        {
            if (PlayedCommand is null || !PlayedCommand.ShouldExecute) return;

            OnCommandExecutionStart?.Invoke(PlayedCommand);

            playedScriptRegister.RegisterPlayedIndex(PlayedScript.Name, PlayedIndex);

            foreach (var task in preExecutionTasks)
                await task(PlayedCommand);

            if (PlayedCommand.Wait) await PlayedCommand.ExecuteAsync(commandExecutionCTS.Token);
            else PlayedCommand.ExecuteAsync(commandExecutionCTS.Token).WrapAsync();

            foreach (var task in postExecutionTasks)
                await task(PlayedCommand);

            if (providerManager.ResourcePolicy == ResourcePolicy.Dynamic)
            {
                if (PlayedCommand is Command.IPreloadable playedPreloadableCmd)
                    playedPreloadableCmd.ReleaseResources();
                // TODO: Handle @goto, @if/else/elseif and all the conditionally executed actions. (just unload everything that has a lower play index?)
                if (Playlist.GetCommandByIndex(PlayedIndex + providerManager.DynamicPolicySteps) is Command.IPreloadable nextPreloadableCmd)
                    nextPreloadableCmd.HoldResourcesAsync().WrapAsync();
            }

            OnCommandExecutionFinish?.Invoke(PlayedCommand);
        }

        private async Task PlayRoutineAsync (CancellationToken cancellationToken)
        {
            while (Engine.IsInitialized && IsPlaying)
            {
                if (IsWaitingForInput)
                {
                    if (IsAutoPlayActive) { await Task.WhenAny(WaitForAutoPlayDelayAsync(), WaitForWaitForInputDisabledAsync()); DisableWaitingForInput(); }
                    else await WaitForWaitForInputDisabledAsync();
                    if (cancellationToken.IsCancellationRequested) break;
                }

                await ExecutePlayedCommandAsync();

                if (cancellationToken.IsCancellationRequested) break;

                var nextActionAvailable = SelectNextCommand();
                if (!nextActionAvailable) break;

                if (IsSkipActive && !IsSkipAllowed()) SetSkipActive(false);
            }
        }

        private async Task<bool> FastForwardRoutineAsync (CancellationToken cancellationToken, int targetPlaylistIndex)
        {
            SetSkipActive(true);

            var reachedLine = true;
            while (Engine.IsInitialized && IsPlaying)
            {
                var nextCommandAvailable = SelectNextCommand();
                if (!nextCommandAvailable) { reachedLine = false; break; }

                await ExecutePlayedCommandAsync();
                SetSkipActive(true); // Force skip mode to be always active while fast-forwarding.

                if (PlayedIndex >= targetPlaylistIndex) { reachedLine = true; break; }
                if (cancellationToken.IsCancellationRequested) { reachedLine = false; break; }
            }

            SetSkipActive(false);
            return reachedLine;
        }

        /// <summary>
        /// Attempts to select next <see cref="Command"/> in the current <see cref="Playlist"/>.
        /// </summary>
        /// <returns>Whether next command is available and was selected.</returns>
        private bool SelectNextCommand ()
        {
            PlayedIndex++;
            if (Playlist.IsIndexValid(PlayedIndex)) return true;

            // No commands left in the played script.
            Debug.Log($"Script '{PlayedScript.Name}' has finished playing, and there wasn't a follow-up goto command. " +
                        "Consider using stop command in case you wish to gracefully stop script execution.");
            return false;
        }

        private void SetSkipActive (bool isActive)
        {
            if (IsSkipActive == isActive) return;
            IsSkipActive = isActive;
            Time.timeScale = isActive ? config.SkipTimeScale : 1f;
            OnSkip?.Invoke(isActive);

            if (isActive && IsWaitingForInput) SetWaitingForInputActive(false);
        }

        private void SetAutoPlayActive (bool isActive)
        {
            if (IsAutoPlayActive == isActive) return;
            IsAutoPlayActive = isActive;
            OnAutoPlay?.Invoke(isActive);

            if (isActive && IsWaitingForInput) SetWaitingForInputActive(false);
        }

        private void SetWaitingForInputActive (bool isActive)
        {
            if (IsWaitingForInput == isActive) return;
            IsWaitingForInput = isActive;
            if (!isActive)
            {
                waitForWaitForInputDisabledTCS?.TrySetResult(null);
                waitForWaitForInputDisabledTCS = null;
            }
            OnWaitingForInput?.Invoke(isActive);
        }
    } 
}
