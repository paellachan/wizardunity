// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <summary>
    /// A <see cref="UITextPrinterPanel"/> implementation for a chat-style printer.
    /// </summary>
    public class ChatPrinterPanel : UITextPrinterPanel
    {
        [System.Serializable]
        private class GameState
        {
            public List<ChatMessage.State> Messages;
            public string LastMesssageText;
        }

        public override string PrintedText { get => printedText; set => SetPrintedText(value); }
        public override string ActorNameText { get; set; }
        public override float RevealProgress { get => revealProgress; set { if (value == 0) DestroyAllMessages(); } }
        public override string Apperance { get; set; }

        [SerializeField] private ScrollRect scrollRect = default;
        [SerializeField] private RectTransform messagesContainer = default;
        [SerializeField] private ChatMessage messagePrototype = default;
        [SerializeField] private ScriptableUIBehaviour inputIndicator = default;
        [SerializeField] private float revealDelayModifier = 3f;
        [SerializeField] private float printDotDelay = .5f;

        private Stack<ChatMessage> messageStack = new Stack<ChatMessage>();
        private CharacterManager characterManager;
        private StateManager stateManager;
        private string lastAuthorId;
        private string printedText;
        private string lastMesssageText;
        private float revealProgress = .1f;

        public override IEnumerator RevealPrintedTextOverTime (CancellationToken cancellationToken, float revealDelay)
        {
            var message = AddMessage(string.Empty, lastAuthorId);

            revealProgress = .1f;

            if (revealDelay > 0)
            {
                var revealDuration = lastMesssageText.Count(c => char.IsLetterOrDigit(c)) * revealDelay * revealDelayModifier;
                var revealStartTime = Time.time;
                var revealFinishTime = revealStartTime + revealDuration;
                var lastPrintDotTime = 0f;
                while (revealFinishTime > Time.time && messageStack.Count > 0 && messageStack.Peek() == message)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    // Print dots while waiting.
                    if (Time.time >= lastPrintDotTime + printDotDelay)
                    {
                        lastPrintDotTime = Time.time;
                        message.PrintedText = message.PrintedText.Length >= 9 ? string.Empty : message.PrintedText + " . ";
                    }

                    revealProgress = (Time.time - revealStartTime) / revealDuration;

                    yield return null;
                }
            }

            if (messageStack.Contains(message))
                message.PrintedText = lastMesssageText;

            ScrollToBottom();

            revealProgress = 1f;
        }

        public override void SetWaitForInputIndicatorVisible (bool isVisible)
        {
            if (isVisible) inputIndicator.Show();
            else inputIndicator.Hide();
        }

        public override void OnAuthorChanged (string authorId, CharacterMetadata authorMeta)
        {
            lastAuthorId = authorId;
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(scrollRect, messagesContainer, messagePrototype, inputIndicator);

            characterManager = Engine.GetService<CharacterManager>();
            stateManager = Engine.GetService<StateManager>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            stateManager.AddOnGameSerializeTask(SerializeState);
            stateManager.AddOnGameDeserializeTask(DeserializeState);
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            stateManager.RemoveOnGameSerializeTask(SerializeState);
            stateManager.RemoveOnGameDeserializeTask(DeserializeState);
        }

        protected virtual void SetPrintedText (string value)
        {
            printedText = value;

            if (messageStack.Count == 0 || string.IsNullOrEmpty(lastMesssageText))
                lastMesssageText = value;
            else
            {
                var previousText = string.Join(string.Empty, messageStack.Select(m => m.PrintedText).Reverse());
                lastMesssageText = value.GetAfterFirst(previousText);
            }
        }

        private ChatMessage AddMessage (string messageText, string authorId = null, bool instant = false)
        {
            var message = Instantiate(messagePrototype);
            message.transform.SetParent(messagesContainer, false);
            message.PrintedText = messageText;
            message.AuthorId = authorId;

            if (!string.IsNullOrEmpty(authorId))
            {
                message.ActorNameText = characterManager.GetDisplayName(authorId);
                message.AvatarTexture = CharacterManager.GetAvatarTextureFor(authorId);

                var meta = characterManager.GetActorMetadata(authorId);
                if (meta.UseCharacterColor)
                {
                    message.MessageColor = meta.MessageColor;
                    message.ActorNameTextColor = meta.NameColor;
                }
            }
            else
            {
                message.ActorNameText = string.Empty;
                message.AvatarTexture = null;
            }

            if (instant) message.IsVisible = true;
            else message.Show();

            messageStack.Push(message);
            ScrollToBottom();
            return message;
        }

        private void DestroyAllMessages ()
        {
            while (messageStack.Count > 0)
            {
                var message = messageStack.Pop();
                ObjectUtils.DestroyOrImmediate(message.gameObject);
            }
        }

        private async void ScrollToBottom ()
        {
            await new WaitForEndOfFrame();
            scrollRect.verticalNormalizedPosition = 0;
        }

        private Task SerializeState (GameStateMap stateMap)
        {
            var state = new GameState() {
                Messages = messageStack.Select(m => m.GetState()).Reverse().ToList(),
                LastMesssageText = lastMesssageText
            };
            stateMap.SetState(state);
            return Task.CompletedTask;
        }

        private Task DeserializeState (GameStateMap stateMap)
        {
            DestroyAllMessages();
            lastMesssageText = null;

            var state = stateMap.GetState<GameState>();
            if (state is null) return Task.CompletedTask;

            if (state.Messages?.Count > 0)
                foreach (var message in state.Messages)
                    AddMessage(message.PrintedText, message.AuthorId, true);

            lastMesssageText = state.LastMesssageText;

            return Task.CompletedTask;
        }
    } 
}
