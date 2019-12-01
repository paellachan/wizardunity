// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class CustomVariablesConfiguration : Configuration
    {
        [Tooltip("The list of variables to initialize by default. Global variables (names starting with `G_` or `g_`) are intialized on first application start, and others on each state reset.")]
        public List<CustomVariableData> PredefinedVariables = new List<CustomVariableData>();
    }
}
