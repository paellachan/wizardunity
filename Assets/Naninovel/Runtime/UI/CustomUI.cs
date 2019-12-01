// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// An implementation of <see cref="IManagedUI"/>, that
    /// can be used to create custom user managed UI objects.
    /// </summary>
    public class CustomUI : ScriptableUIBehaviour, IManagedUI
    {
        [System.Serializable]
        private class GameState
        {
            public bool IsVisible;
        }

        [Tooltip("Whether to automatically hide the UI when loading game or resetting state.")]
        [SerializeField] private bool hideOnLoad = true;
        [Tooltip("Whether to preserve visibility of the UI when saving/loading game.")]
        [SerializeField] private bool saveVisibilityState = true;
        [Tooltip("Whether the engine should halt user input processing while the UI is visible.")]
        [SerializeField] private bool blockInputWhenVisible = false;

        public virtual Task InitializeAsync () => Task.CompletedTask;

        private StateManager stateManager;
        private InputManager inputManager;

        protected override void Awake ()
        {
            base.Awake();

            stateManager = Engine.GetService<StateManager>();
            inputManager = Engine.GetService<InputManager>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            if (hideOnLoad)
            {
                stateManager.OnGameLoadStarted += HandleGameLoadStarted;
                stateManager.OnResetStarted += Hide;
            }

            stateManager.AddOnGameSerializeTask(SerializeState);
            stateManager.AddOnGameDeserializeTask(DeserializeState);

            if (blockInputWhenVisible)
                inputManager.AddBlockingUI(this);
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (hideOnLoad && stateManager != null)
            {
                stateManager.OnGameLoadStarted -= HandleGameLoadStarted;
                stateManager.OnResetStarted -= Hide;
            }

            if (stateManager != null)
            {
                stateManager.RemoveOnGameSerializeTask(SerializeState);
                stateManager.RemoveOnGameDeserializeTask(DeserializeState);
            }

            if (blockInputWhenVisible)
                inputManager.RemoveBlockingUI(this);
        }

        protected virtual Task SerializeState (GameStateMap stateMap)
        {
            if (saveVisibilityState)
            {
                var state = new GameState() {
                    IsVisible = IsVisible
                };
                stateMap.SetState(state, name);
            }
            return Task.CompletedTask;
        }

        protected virtual Task DeserializeState (GameStateMap stateMap)
        {
            if (saveVisibilityState)
            {
                var state = stateMap.GetState<GameState>(name);
                if (state is null) return Task.CompletedTask;
                IsVisible = state.IsVisible;
            }
            return Task.CompletedTask;
        }

        private void HandleGameLoadStarted (GameSaveLoadArgs args) => Hide();
    }
}
