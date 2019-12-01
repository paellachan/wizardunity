// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel
{
    [System.Serializable]
    public class InputConfiguration : Configuration
    {
        [Tooltip("Limits frequency on the continue input when using touch input.")]
        public float TouchContinueCooldown = .1f;
        [Tooltip("Whether to spawn an event system when initializing.")]
        public bool SpawnEventSystem = true;
        [Tooltip("A prefab with an `EventSystem` component to spawn for input processing. Will spawn a default one when not specified.")]
        public EventSystem CustomEventSystem = null;
        [Tooltip("Whether to spawn an input module when initializing.")]
        public bool SpawnInputModule = true;
        [Tooltip("A prefab with an `InputModule` component to spawn for input processing. Will spawn a default one when not specified.")]
        public BaseInputModule CustomInputModule = null;

        [Header("Control Scheme"), Tooltip("Bindings to process input for.")]
        public List<InputBinding> Bindings = new List<InputBinding> {
            new InputBinding { Name = InputManager.SubmitName, Keys = new List<KeyCode> { KeyCode.Mouse0 } },
            new InputBinding { Name = InputManager.CancelName, Keys = new List<KeyCode> { KeyCode.Escape }, AlwaysProcess = true },
            new InputBinding {
                Name = InputManager.ContinueName,
                Keys = new List<KeyCode> { KeyCode.Return, KeyCode.KeypadEnter, KeyCode.JoystickButton0 },
                Axes = new List<InputAxisTrigger> { new InputAxisTrigger { AxisName = "Mouse ScrollWheel", TriggerMode = InputAxisTriggerMode.Negative } }
            },
            new InputBinding { Name = InputManager.SkipName, Keys = new List<KeyCode> { KeyCode.LeftControl, KeyCode.RightControl, KeyCode.JoystickButton1 } },
            new InputBinding { Name = InputManager.AutoPlayName, Keys = new List<KeyCode> { KeyCode.A, KeyCode.JoystickButton2 } },
            new InputBinding { Name = InputManager.ToggleUIName, Keys = new List<KeyCode> { KeyCode.Space, KeyCode.JoystickButton3 } },
            new InputBinding { Name = InputManager.ShowBacklogName, Keys = new List<KeyCode> { KeyCode.L, KeyCode.JoystickButton5 } },
            new InputBinding {
                Name = InputManager.RollbackName,
                Keys = new List<KeyCode> { KeyCode.B, KeyCode.JoystickButton4 },
                Axes = new List<InputAxisTrigger> { new InputAxisTrigger { AxisName = "Mouse ScrollWheel", TriggerMode = InputAxisTriggerMode.Positive } }
            },
            new InputBinding {
                Name = InputManager.CameraLookXName,
                Axes = new List<InputAxisTrigger> {
                    new InputAxisTrigger { AxisName = "Horizontal", TriggerMode = InputAxisTriggerMode.Both },
                    new InputAxisTrigger { AxisName = "Mouse X", TriggerMode = InputAxisTriggerMode.Both }
                }
            },
            new InputBinding {
                Name = InputManager.CameraLookYName,
                Axes = new List<InputAxisTrigger> {
                    new InputAxisTrigger { AxisName = "Vertical", TriggerMode = InputAxisTriggerMode.Both },
                    new InputAxisTrigger { AxisName = "Mouse Y", TriggerMode = InputAxisTriggerMode.Both }
                }
            }
        };
    }
}
