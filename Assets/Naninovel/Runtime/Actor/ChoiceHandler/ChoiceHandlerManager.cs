// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <summary>
    /// Manages choice handler actors.
    /// </summary>
    [InitializeAtRuntime]
    public class ChoiceHandlerManager : ActorManager<IChoiceHandlerActor, ChoiceHandlerState, ChoiceHandlerMetadata, ChoiceHandlersConfiguration>
    {
        public string DefaultHandlerId => Configuration.DefaultHandlerId;

        public ChoiceHandlerManager (ChoiceHandlersConfiguration config)
            : base(config) { }
    }
}
