// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Shows (makes visible) an actor (character, background, text printer, choice handler, etc) with the provided ID.
    /// In case mutliple actors with the same ID found (eg, a character and a printer), will affect all of them.
    /// </summary>
    /// <example>
    /// ; Given an actor with ID `SomeActor` is hidden, reveal (fade-in) it over 3 seconds.
    /// @show SomeActor time:3
    /// </example>
    [CommandAlias("show")]
    public class ShowActor : Command
    {
        /// <summary>
        /// ID of the actor to show.
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

            await Task.WhenAll(managers.Select(m => m.GetActor(ActorId).ChangeVisibilityAsync(true, Duration, cancellationToken: cancellationToken)));
        }
    } 
}
