// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Hides (makes invisible) an actor (character, background, text printer, choice handler, etc) with the provided ID.
    /// In case mutliple actors with the same ID found (eg, a character and a printer), will affect all of them.
    /// </summary>
    /// <example>
    /// ; Given an actor with ID `SomeActor` is visible, hide (fade-out) it over 3 seconds.
    /// @hide SomeActor time:3
    /// </example>
    [CommandAlias("hide")]
    public class HideActor : Command
    {
        /// <summary>
        /// ID of the actor to hide.
        /// </summary>
        [CommandParameter(alias: NamelessParameterAlias)]
        public string ActorId { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var managers = Engine.GetAllServices<IActorManager>(c => c.ActorExists(ActorId));

            if (managers is null || managers.Count == 0)
            {
                Debug.LogError($"Can't find a manager with `{ActorId}` actor.");
                return;
            }

            await Task.WhenAll(managers.Select(m => m.GetActor(ActorId).ChangeVisibilityAsync(false, Duration, cancellationToken: cancellationToken)));
        }
    } 
}
