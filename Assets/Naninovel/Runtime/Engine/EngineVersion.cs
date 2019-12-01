// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Stores engine version and build number.
    /// </summary>
    public class EngineVersion : ScriptableObject
    {
        public string Version => engineVersion;
        public string Build => buildDate;

        private const string resourcesPath = "Naninovel/" + nameof(EngineVersion);

        [SerializeField, ReadOnly] private string engineVersion = string.Empty;
        [SerializeField, ReadOnly] private string buildDate = string.Empty;

        public static EngineVersion LoadFromResources ()
        {
            return Resources.Load<EngineVersion>(resourcesPath);
        }
    }
}
