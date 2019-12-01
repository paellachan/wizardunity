// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;
using UnityCommon;

namespace Naninovel.Commands
{
    /// <summary>
    /// Sets an [unlockable item](/guide/unlockable-items.md) with the provided ID to `locked` state.
    /// </summary>
    /// <remarks>
    /// The unlocked state of the items is stored in [global scope](/guide/state-management.md#global-state).<br/>
    /// In case item with the provided ID is not registered in the global state map, 
    /// the corresponding record will automatically be added.
    /// </remarks>
    /// <example>
    /// @lock CG/FightScene1
    /// </example>
    public class Lock : Command
    {
        /// <summary>
        /// ID of the unlockable item. Use `all` to lock all the registered unlockable items. 
        /// </summary>
        [CommandParameter(NamelessParameterAlias)]
        public string Id { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var unlockableManager = Engine.GetService<UnlockableManager>();

            if (Id.EqualsFastIgnoreCase("all")) unlockableManager.LockAllItems();
            else unlockableManager.LockItem(Id);

            await Engine.GetService<StateManager>().SaveGlobalStateAsync();
        }
    } 
}
