// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Allows halting and resuming user input processing (eg, reacting to pressing keyboard keys).
    /// The effect of the action is persistent and saved with the game.
    /// </summary>
    /// <example>
    /// ; Halt input processing
    /// @processInput false
    /// ; Resume input processing
    /// @processInput true
    /// </example>
    public class ProcessInput : Command
    {
        /// <summary>
        /// Whether to enable input processing.
        /// </summary>
        [CommandParameter(NamelessParameterAlias)]
        public bool InputEnabled { get => GetDynamicParameter(true); set => SetDynamicParameter(value); }

        public override Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var inputManager = Engine.GetService<InputManager>();
            inputManager.ProcessInput = InputEnabled;

            return Task.CompletedTask;
        }
    }
}
