// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Adds a choice option to a choice handler with the specified ID (or default one).
    /// </summary>
    /// <remarks>
    /// When `goto` parameter is not specified, will continue script execution from the next script line.
    /// </remarks>
    /// <example>
    /// ; Print the text, then immediately show choices and stop script execution.
    /// Continue executing this script or load another?[skipInput]
    /// @choice "Continue" goto:.Continue
    /// @choice "Load another from start" goto:AnotherScript
    /// @choice "Load another from \"MyLabel\"" goto:AnotherScript.MyLabel
    /// @stop
    /// 
    /// ; Following example shows how to make an interactive map via `@choice` commands.
    /// ; For this example, we assume, that inside a `Resources/MapButtons` folder you've 
    /// ; stored prefabs with `ChoiceHandlerButton` component attached to their root objects.
    /// # Map
    /// @back Map
    /// @hidePrinter
    /// @choice handler:ButtonArea button:MapButtons/Home pos:-300,-300 goto:.HomeScene
    /// @choice handler:ButtonArea button:MapButtons/Shop pos:300,200 goto:.ShopScene
    /// @stop
    /// 
    /// # HomeScene
    /// @back Home
    /// Home, sweet home!
    /// @goto.Map
    /// 
    /// # ShopScene
    /// @back Shop
    /// Don't forget about cucumbers!
    /// @goto.Map
    /// 
    /// ; You can also set custom variables based on choices.
    /// @choice "I'm humble, one is enough..." set:score++
    /// @choice "Two, please." set:score=score+2
    /// @choice "I'll take the entire stock!" set:karma--;score=999
    /// </example>
    [CommandAlias("choice")]
    public class AddChoice : Command, Command.ILocalizable, Command.IPreloadable
    {
        /// <summary>
        /// Text to show for the choice.
        /// When the text contain spaces, wrap it in double quotes (`"`). 
        /// In case you wish to include the double quotes in the text itself, escape them.
        /// </summary>
        [CommandParameter(NamelessParameterAlias, true)]
        public string ChoiceSummary { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Path (relative to a `Resources` folder) to a [button prefab](/guide/choices.md#choice-button) representing the choice. 
        /// The prefab should have a `ChoiceHandlerButton` component attached to the root object.
        /// Will use a default button when not provided.
        /// </summary>
        [CommandParameter("button", true)]
        public string ButtonPath { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Local position of the choice button inside the choice handler (if supported by the handler implementation).
        /// </summary>
        [CommandParameter("pos", true)]
        public float?[] ButtonPosition { get => GetDynamicParameter<float?[]>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// ID of the choice handler to add choice for. Will use a default handler if not provided.
        /// </summary>
        [CommandParameter("handler", true)]
        public string HandlerId { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Path to go when the choice is selected by user;
        /// See [`@goto`](/api/#goto) command for the path format.
        /// </summary>
        [CommandParameter("goto", true)]
        public Named<string> GotoPath { get => GetDynamicParameter<Named<string>>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Set expression to execute when the choice is selected by user; 
        /// see [`@set`](/api/#set) command for syntax reference.
        /// </summary>
        [CommandParameter("set", true)]
        public string SetExpression { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }

        public async Task HoldResourcesAsync ()
        {
            var mngr = Engine.GetService<ChoiceHandlerManager>();
            var handlerId = string.IsNullOrWhiteSpace(HandlerId) ? mngr.DefaultHandlerId : HandlerId;
            var handler = await mngr.GetOrAddActorAsync(handlerId);
            await handler.HoldResourcesAsync(this, null);
        }

        public void ReleaseResources ()
        {
            var mngr = Engine.GetService<ChoiceHandlerManager>();
            var handlerId = string.IsNullOrWhiteSpace(HandlerId) ? mngr.DefaultHandlerId : HandlerId;
            if (mngr.ActorExists(handlerId)) mngr.GetActor(handlerId).ReleaseResources(this, null);
        }

        public override async Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var mngr = Engine.GetService<ChoiceHandlerManager>();

            var handlerId = string.IsNullOrEmpty(HandlerId) ? mngr.DefaultHandlerId : HandlerId;
            var choiceHandler = await mngr.GetOrAddActorAsync(handlerId);
            if (cancellationToken.IsCancellationRequested) return;

            if (!choiceHandler.IsVisible)
                choiceHandler.ChangeVisibilityAsync(true, Duration, cancellationToken: cancellationToken).WrapAsync();

            var buttonPos = ButtonPosition is null ? null : (Vector2?)ArrayUtils.ToVector2(ButtonPosition);
            var choice = new ChoiceState(ChoiceSummary, ButtonPath, buttonPos, GotoPath?.Item1, GotoPath?.Item2, SetExpression);
            choiceHandler.AddChoice(choice);
        }
    }
}
