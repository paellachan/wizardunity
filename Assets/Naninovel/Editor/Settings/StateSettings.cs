// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class StateSettings : ConfigurationSettings<StateConfiguration>
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers => new Dictionary<string, Action<SerializedProperty>> {
            [nameof(StateConfiguration.StateRollbackSteps)] = property => { if (Configuration.StateRollbackMode != StateRollbackMode.Disabled) EditorGUILayout.PropertyField(property); },
            [nameof(StateConfiguration.SavedRollbackSteps)] = property => { if (Configuration.StateRollbackMode == StateRollbackMode.Full) EditorGUILayout.PropertyField(property); }
        };
    }
}
