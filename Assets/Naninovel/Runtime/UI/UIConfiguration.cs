// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.UI;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class UIConfiguration : Configuration
    {
        [Tooltip("The layer to assign for the UI elements instatiated by the engine. Used to cull the UI when using `toogle UI` feature.")]
        public int ObjectsLayer = 5;
        [Tooltip("The canvas render mode to apply for all the managed UI elements.")]
        public RenderMode RenderMode = RenderMode.ScreenSpaceCamera;
        [Tooltip("The sorting offset to apply for all the managed UI elements.")]
        public int SortingOffset = 1;
        [Tooltip("The list of default UI to spawn on the engine initialization. You can override or disable the built-in UI here.")]
        public List<DefaultUIData> DefaultUI = new List<DefaultUIData> {
            new DefaultUIData { Hidden = true, ResourcePath = nameof(ClickThroughPanel) },
            new DefaultUIData { Name = "Backlog", ResourcePath = nameof(IBacklogUI), TypeName = typeof(IBacklogUI).AssemblyQualifiedName },
            new DefaultUIData { Name = "CG Gallery", ResourcePath = nameof(ICGGalleryUI), TypeName = typeof(ICGGalleryUI).AssemblyQualifiedName },
            new DefaultUIData { Name = "Confirmation Dialogue", ResourcePath = nameof(IConfirmationUI), TypeName = typeof(IConfirmationUI).AssemblyQualifiedName },
            new DefaultUIData { Name = "Continue Input Trigger", ResourcePath = nameof(IContinueInputUI), TypeName = typeof(IContinueInputUI).AssemblyQualifiedName },
            new DefaultUIData { Name = "External Scripts Browser", ResourcePath = nameof(IExternalScriptsUI), TypeName = typeof(IExternalScriptsUI).AssemblyQualifiedName },
            new DefaultUIData { Name = "Loading Screen", ResourcePath = nameof(ILoadingUI), TypeName = typeof(ILoadingUI).AssemblyQualifiedName },
            new DefaultUIData { Name = "Movie Player", ResourcePath = nameof(IMovieUI), TypeName = typeof(IMovieUI).AssemblyQualifiedName },
            new DefaultUIData { Name = "State Rollback Indicator", ResourcePath = nameof(IRollbackUI), TypeName = typeof(IRollbackUI).AssemblyQualifiedName },
            new DefaultUIData { Name = "Save/Load Game Menu", ResourcePath = nameof(ISaveLoadUI), TypeName = typeof(ISaveLoadUI).AssemblyQualifiedName },
            new DefaultUIData { Name = "Game Settings Menu", ResourcePath = nameof(ISettingsUI), TypeName = typeof(ISettingsUI).AssemblyQualifiedName },
            new DefaultUIData { Name = "Unlockable Tips Panel", ResourcePath = nameof(ITipsUI), TypeName = typeof(ITipsUI).AssemblyQualifiedName },
            new DefaultUIData { Name = "Title Menu", ResourcePath = nameof(ITitleUI), TypeName = typeof(ITitleUI).AssemblyQualifiedName },
            new DefaultUIData { Name = "Variable Input Dialogue", ResourcePath = nameof(IVariableInputUI), TypeName = typeof(IVariableInputUI).AssemblyQualifiedName },
        };
        [Tooltip("The list of custom UI prefabs to spawn on the engine initialization. Each prefab should have a `" + nameof(IManagedUI) + "`-derived component attached to the root object.")]
        public List<GameObject> CustomUI = new List<GameObject>();
    }
}
