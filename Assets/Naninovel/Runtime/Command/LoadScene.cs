// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Naninovel.Commands
{
    /// <summary>
    /// Loads a [Unity scene](https://docs.unity3d.com/Manual/CreatingScenes.html) with the provided name.
    /// Don't forget to add the required scenes to the [build settings](https://docs.unity3d.com/Manual/BuildSettings.html) to make them available for loading.
    /// </summary>
    /// <example>
    /// ; Load scene "MyTestScene" in single mode
    /// @loadScene MyTestScene
    /// ; Load scene "MyTestScene" in additive mode
    /// @loadScene MyTestScene additive:true
    /// </example>
    public class LoadScene : Command
    {
        /// <summary>
        /// Name of the scene to load.
        /// </summary>
        [CommandParameter(NamelessParameterAlias)]
        public string SceneName { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Whether to load the scene additively, or unload any currently loaded scenes before loading the new one (default).
        /// See the [load scene documentation](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadScene.html) for more information.
        /// </summary>
        [CommandParameter(optional: true)]
        public bool Additive { get => GetDynamicParameter(false); set => SetDynamicParameter(value); }

        public override Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            SceneManager.LoadScene(SceneName, Additive ? LoadSceneMode.Additive : LoadSceneMode.Single);

            return Task.CompletedTask;
        }
    }
}
