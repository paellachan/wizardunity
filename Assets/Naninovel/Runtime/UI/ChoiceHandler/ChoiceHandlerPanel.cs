// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents a view for choosing between a set of choices.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ChoiceHandlerPanel : ScriptableUIBehaviour, IManagedUI
    {
        [System.Serializable]
        private class GameState
        {
            public bool RemoveAllButtonsPending;
        }

        /// <summary>
        /// Invoked when one of active choices are choosen.
        /// </summary>
        public event Action<ChoiceState> OnChoice;

        [Tooltip("Container that will hold spawned choice buttons.")]
        [SerializeField] private RectTransform buttonsContainer = null;
        [Tooltip("Button prototype to use by default.")]
        [SerializeField] private ChoiceHandlerButton defaultButtonPrefab = null;
        [Tooltip("Whether to focus added choice buttons for keyboard and gamepad control.")]
        [SerializeField] private bool focusChoiceButtons = true;

        private readonly List<ChoiceHandlerButton> choiceButtons = new List<ChoiceHandlerButton>();
        private StateManager stateManager;
        private bool removeAllButtonsPending;

        public virtual Task InitializeAsync () => Task.CompletedTask;

        public virtual void AddChoiceButton (ChoiceState choice)
        {
            if (removeAllButtonsPending)
            {
                removeAllButtonsPending = false;
                RemoveAllChoiceButtons();
            }

            var choicePrefab = string.IsNullOrWhiteSpace(choice.ButtonPath) ? defaultButtonPrefab : Resources.Load<ChoiceHandlerButton>(choice.ButtonPath);
            var choiceButton = Instantiate(choicePrefab);
            choiceButton.transform.SetParent(buttonsContainer, false);
            choiceButton.Initialize(choice);
            choiceButton.OnButtonClicked += () => OnChoice?.Invoke(choice);

            if (choice.OverwriteButtonPosition)
                choiceButton.transform.localPosition = choice.ButtonPosition;

            choiceButtons.Add(choiceButton);

            if (focusChoiceButtons && EventSystem.current)
                EventSystem.current.SetSelectedGameObject(choiceButton.gameObject);
        }

        public virtual void RemoveChoiceButton (string id)
        {
            var buttons = choiceButtons.FindAll(c => c.Id == id);
            if (buttons is null || buttons.Count == 0) return;

            foreach (var button in buttons)
            {
                if (button) Destroy(button.gameObject);
                choiceButtons.Remove(button);
            }
        }

        /// <summary>
        /// Will remove the buttons before the next <see cref="AddChoiceButton(ChoiceState)"/> call.
        /// </summary>
        public virtual void RemoveAllChoiceButtonsDelayed ()
        {
            removeAllButtonsPending = true;
        }

        public virtual void RemoveAllChoiceButtons ()
        {
            for (int i = 0; i < choiceButtons.Count; i++)
                Destroy(choiceButtons[i].gameObject);
            choiceButtons.Clear();
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(defaultButtonPrefab, buttonsContainer);

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

            if (stateManager != null)
            {
                stateManager.RemoveOnGameSerializeTask(SerializeState);
                stateManager.RemoveOnGameDeserializeTask(DeserializeState);
            }
        }

        private Task SerializeState (GameStateMap stateMap)
        {
            var state = new GameState() {
                RemoveAllButtonsPending = removeAllButtonsPending
            };
            stateMap.SetState(state, name);
            return Task.CompletedTask;
        }

        private Task DeserializeState (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>(name);
            if (state is null) return Task.CompletedTask;
            removeAllButtonsPending = state.RemoveAllButtonsPending;
            return Task.CompletedTask;
        }
    } 
}
