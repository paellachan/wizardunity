// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Makes a [managed UI](/guide/ui-customization.md) with the provided prefab name invisible.
    /// </summary>
    /// <example>
    /// ; Given you've added a custom managed UI with prefab name `Calendar`,
    /// ; the following will make it invisible on the scene.
    /// @hideUI Calendar
    /// </example>
    public class HideUI : Command
    {
        /// <summary>
        /// Name of the managed UI prefab to hide.
        /// </summary>
        [CommandParameter(NamelessParameterAlias)]
        public string UIPrefabName { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }

        public override Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var uiManager = Engine.GetService<UIManager>();
            var ui = uiManager.GetUI(UIPrefabName);

            if (ui is null)
            {
                Debug.LogWarning($"Failed to execute {nameof(HideUI)} script command: managed UI with prefab name `{UIPrefabName}` not found.");
                return Task.CompletedTask;
            }

            ui.Hide();

            return Task.CompletedTask;
        }
    }
}
