// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Makes a [managed UI](/guide/ui-customization.md) with the provided prefab name visible.
    /// </summary>
    /// <example>
    /// ; Given you've added a custom managed UI with prefab name `Calendar`,
    /// ; the following will make it visible on the scene.
    /// @showUI Calendar
    /// </example>
    public class ShowUI : Command
    {
        /// <summary>
        /// Name of the managed UI prefab to make visible.
        /// </summary>
        [CommandParameter(NamelessParameterAlias)]
        public string UIPrefabName { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }

        public override Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var uiManager = Engine.GetService<UIManager>();
            var ui = uiManager.GetUI(UIPrefabName);

            if (ui is null)
            {
                Debug.LogWarning($"Failed to execute {nameof(ShowUI)} script command: managed UI with prefab name `{UIPrefabName}` not found.");
                return Task.CompletedTask;
            }

            ui.Show();

            return Task.CompletedTask;
        }
    }
}
