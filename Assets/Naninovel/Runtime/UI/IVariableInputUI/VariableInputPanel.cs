// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityCommon;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class VariableInputPanel : CustomUI, IVariableInputUI
    {
        [SerializeField] private InputField inputField = default;
        [SerializeField] private Text summaryText = default;
        [SerializeField] private Button submitButton = default;

        private ScriptPlayer scriptPlayer;
        private CustomVariableManager variableManager;
        private InputManager inputManager;
        private string variableName;
        private bool playOnSubmit;

        public void Show (string variableName, string summary, string predefinedValue, bool playOnSubmit)
        {
            this.variableName = variableName;
            this.playOnSubmit = playOnSubmit;
            summaryText.text = summary ?? string.Empty;
            summaryText.gameObject.SetActive(!string.IsNullOrWhiteSpace(summary));
            inputField.text = predefinedValue ?? string.Empty;

            Show();
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(inputField, summaryText, submitButton);

            scriptPlayer = Engine.GetService<ScriptPlayer>();
            variableManager = Engine.GetService<CustomVariableManager>();
            inputManager = Engine.GetService<InputManager>();

            submitButton.interactable = false;
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            submitButton.onClick.AddListener(HandleSubmit);
            inputField.onValueChanged.AddListener(HandleInputChanged);
            inputManager.AddBlockingUI(this);
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            submitButton.onClick.RemoveListener(HandleSubmit);
            inputField.onValueChanged.RemoveListener(HandleInputChanged);
            inputManager.RemoveBlockingUI(this);
        }

        private void HandleInputChanged (string text)
        {
            submitButton.interactable = !string.IsNullOrWhiteSpace(text);
        }

        private void HandleSubmit ()
        {
            variableManager.SetVariableValue(variableName, inputField.text);

            if (playOnSubmit)
            {
                // Attempt to select and play next command.
                var nextIndex = scriptPlayer.PlayedIndex + 1;
                scriptPlayer.Play(nextIndex);
            }

            Hide();
        }
    }
}
