// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class DefaultUIData
    {
        public const string ResourcePathPrefix = "Naninovel/DefaultUI/";

        /// <summary>
        /// Name (title) of the item in the defult UI list editor.
        /// </summary>
        public string Name = default;
        /// <summary>
        /// Assembly-qualified type name of the UI interface.
        /// </summary>
        public string TypeName = default;
        /// <summary>
        /// Resources path to the UI prefab, relative to <see cref="ResourcePathPrefix"/>.
        /// </summary>
        public string ResourcePath = default;
        /// <summary>
        /// Whether to spawn the UI prefab.
        /// </summary>
        public bool Enabled = true;
        /// <summary>
        /// When provided and valid (has a <see cref="TypeName"/> component attached to the root), 
        /// will be spawned instead of the default prefab.
        /// </summary>
        public GameObject CustomPrefab = default;
        /// <summary>
        /// Whether the UI shouldn't be exposed to the editor.
        /// </summary>
        public bool Hidden = false;
    }
}
