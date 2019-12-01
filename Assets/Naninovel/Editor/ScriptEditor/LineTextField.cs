// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;
using UnityEngine.UIElements;

namespace Naninovel
{
    public class LineTextField : TextField
    {
        public LineTextField (string label = default, string value = default)
        {
            if (label != null)
                this.label = label;
            if (value != null)
                this.value = value;
            labelElement.name = "InputLabel";
            multiline = true;
            var inputField = this.Q<VisualElement>("unity-text-input");
            inputField.RegisterCallback<MouseDownEvent>(HandleFieldMouseDown);
            inputField.style.unityTextAlign = TextAnchor.UpperLeft;
            AddToClassList("Inlined");

            RegisterCallback<ChangeEvent<string>>(HandleValueChanged);
        }

        private void HandleFieldMouseDown (MouseDownEvent evt)
        {
            if (focusController.focusedElement == this)
                return; // Prevent do-focusing the field on consequent clicks.

            // Propagate the event to the parent.
            var newEvt = MouseDownEvent.GetPooled(evt);
            newEvt.target = this;
            SendEvent(newEvt);
        }

        private void HandleValueChanged (ChangeEvent<string> evt)
        {
            if (evt.newValue != evt.previousValue)
                ScriptView.ScriptModified = true;
        }
    }
}
