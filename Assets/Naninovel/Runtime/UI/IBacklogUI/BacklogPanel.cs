// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class BacklogPanel : CustomUI, IBacklogUI
    {
        [System.Serializable]
        private class GameState
        {
            public List<BacklogMessage.State> Messages;
        }

        protected virtual BacklogMessage LastMessage => messageStack != null && messageStack.Count > 0 ? messageStack.Peek() : null;

        [SerializeField] private RectTransform messagesContainer = default;
        [SerializeField] private ScrollRect scrollRect = default;
        [SerializeField] private BacklogMessage messagePrefab = default;
        [Tooltip("Whether to clear the backlog when loading game or resetting state.")]
        [SerializeField] private bool clearOnLoad = true;
        [Tooltip("How many messages should the backlog keep when saving the game.")]
        [SerializeField] private int saveCapacity = 30;

        private InputManager inputManager;
        private CharacterManager charManager;
        private StateManager stateManager;
        private Stack<BacklogMessage> messageStack;

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(messagesContainer, scrollRect, messagePrefab);

            inputManager = Engine.GetService<InputManager>();
            charManager = Engine.GetService<CharacterManager>();
            stateManager = Engine.GetService<StateManager>();
            messageStack = new Stack<BacklogMessage>(saveCapacity);
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            if (inputManager?.ShowBacklog != null)
                inputManager.ShowBacklog.OnStart += Show;

            if (clearOnLoad)
            {
                stateManager.OnGameLoadStarted += HandleGameLoadStarted;
                stateManager.OnResetStarted += Clear;
            }
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (inputManager?.ShowBacklog != null)
                inputManager.ShowBacklog.OnStart -= Show;

            if (stateManager != null && clearOnLoad)
            {
                stateManager.OnGameLoadStarted -= HandleGameLoadStarted;
                stateManager.OnResetStarted -= Clear;
            }
        }

        public void Clear ()
        {
            foreach (var message in messageStack)
                Destroy(message.gameObject);
            messageStack.Clear();
        }

        public void AddMessage (string messageText, string actorId = null, string voiceClipName = null)
        {
            var actorNameText = charManager.GetDisplayName(actorId) ?? actorId;
            SpawnMessage(messageText, actorNameText, voiceClipName != null ? new List<string> { voiceClipName } : null);
        }

        public void AppendMessage (string message, string voiceClipName = null)
        {
            if (!LastMessage) return;
            LastMessage.AppendText(message);
            if (!string.IsNullOrWhiteSpace(voiceClipName))
                LastMessage.AddVoiceClipName(voiceClipName);
        }

        public override void SetIsVisible (bool isVisible)
        {
            if (isVisible) ScrollToBottom();
            base.SetIsVisible(isVisible);
        }

        public override Task SetIsVisibleAsync (bool isVisible, float? fadeTime = null)
        {
            if (isVisible) ScrollToBottom();
            return base.SetIsVisibleAsync(isVisible, fadeTime);
        }

        protected virtual void SpawnMessage (string messageText, string actorNameText, List<string> voiceClipNames = null)
        {
            var message = Instantiate(messagePrefab);
            message.Initialize(messageText, actorNameText, voiceClipNames);
            message.transform.SetParent(messagesContainer.transform, false);
            messageStack.Push(message);
        }

        protected override async Task SerializeState (GameStateMap stateMap)
        {
            await base.SerializeState(stateMap);
            var state = new GameState() {
                Messages = messageStack.Take(saveCapacity).Select(m => m.GetState()).Reverse().ToList()
            };
            stateMap.SetState(state);
        }

        protected override async Task DeserializeState (GameStateMap stateMap)
        {
            await base.DeserializeState(stateMap);

            Clear();

            var state = stateMap.GetState<GameState>();
            if (state is null) return;

            if (state.Messages?.Count > 0)
                foreach (var messageState in state.Messages)
                    SpawnMessage(messageState.MessageText, messageState.ActorNameText, messageState.VoiceClipNames);
        }

        private async void ScrollToBottom ()
        {
            await new WaitForEndOfFrame();
            scrollRect.verticalNormalizedPosition = 0;
        }

        private void HandleGameLoadStarted (GameSaveLoadArgs args) => Clear();
    }
}
