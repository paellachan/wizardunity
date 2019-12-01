// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class StateConfiguration : Configuration
    {
        [Tooltip("The folder will be created in the game data folder.")]
        public string SaveFolderName = "Saves";
        [Tooltip("The name of the settings save file.")]
        public string DefaultSettingsSlotId = "Settings";
        [Tooltip("The name of the global save file.")]
        public string DefaultGlobalSlotId = "GlobalSave";
        [Tooltip("Mask used to name save slots.")]
        public string SaveSlotMask = "GameSave{0:000}";
        [Tooltip("Mask used to name quick save slots.")]
        public string QuickSaveSlotMask = "GameQuickSave{0:000}";
        [Tooltip("Maximum number of save slots."), Range(1, 999)]
        public int SaveSlotLimit = 99;
        [Tooltip("Maximum number of quick save slots."), Range(1, 999)]
        public int QuickSaveSlotLimit = 9;
        [Tooltip("Whether to compress and store the saves as binary files (.nson) instead of text files (.json). This will significantly reduce the files size and make them harder to edit (to prevent cheating), but will consume more memory and CPU time when saving and loading.")]
        public bool BinarySaveFiles = true;
        [Tooltip("Seconds to wait before starting the load operation.")]
        public float LoadStartDelay = 0.3f;
        [Tooltip("Whether to reset state of all the engine services and unload (dispose) resources upon loading a naninovel script. This is usually triggered when using `@goto` command to move playback to another script. It's recommended to leave this enabled to prevent memory leak issues. If you choose to disable this option, you can still reset the state and dispose resources manually at any time using `@resetState` command.")]
        public bool ResetStateOnLoad = true;
        [Tooltip("Whether to enable state rollback feature allowing to play (rewind) the script backwards. Be aware that enabling this feature will have permament impact on performance (both memory and CPU-wise)." +
            "\n\nAvailable modes:" +
            "\n • Disabled — The feature will be completely disabled." +
            "\n • Debug — The feature will only work in editor and debug builds. Performance of the release builds won't be affected." +
            "\n • Full — The feature will always be enabled allowing players to rewind played scripts at will.")]
        public StateRollbackMode StateRollbackMode = StateRollbackMode.Full;
        [Tooltip("The number of state snapshots to keep at runtime; determines how far back the rollback (rewind) can be performed. Increasing this value will consume more memory.")]
        public int StateRollbackSteps = 1024;
        [Tooltip("The number of state snapshots to serialize (save) under the save game slots; determines how far back the rollback can be performed after loading a saved game. Increasing this value will enlarge save game files.")]
        public int SavedRollbackSteps = 128;
    }
}
