// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;

namespace Naninovel.Commands
{
    /// <summary>
    /// Shows an input field UI where user can enter an arbitrary text.
    /// Upon submit the entered text will be assigned to the specified custom variable.
    /// </summary>
    /// <remarks>
    /// Check out this [video guide](https://youtu.be/F9meuMzvGJw) on usage example.
    /// <br/><br/>
    /// To assign a display name for a character using this command consider [binding the name to a custom variable](/guide/characters.html#display-names).
    /// </remarks>
    /// <example>
    /// ; Allow user to enter an arbitrary text and assign it to `name` custom state variable
    /// @input name summary:"Choose your name."
    /// ; Stop command is required to halt script execution until user submits the input
    /// @stop
    /// 
    /// ; You can then inject the assigned `name` variable in naninovel scripts
    /// Archibald: Greetings, {name}!
    /// {name}: Yo! 
    /// 
    /// ; ...or use it inside set and conditional expressions
    /// @set score=score+1 if:name=="Felix"
    /// </example>
    [CommandAlias("input")]
    public class InputCustomVariable : Command, Command.ILocalizable
    {
        /// <summary>
        /// Name of a custom variable to which the entered text will be assigned.
        /// </summary>
        [CommandParameter(alias: NamelessParameterAlias)]
        public string VariableName { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// An optional summary text to show along with input field.
        /// When the text contain spaces, wrap it in double quotes (`"`). 
        /// In case you wish to include the double quotes in the text itself, escape them.
        /// </summary>
        [CommandParameter(optional: true)]
        public string Summary { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// A predefined value to set for the input field.
        /// </summary>
        [CommandParameter("value", true)]
        public string PredefinedValue { get => GetDynamicParameter<string>(null); set => SetDynamicParameter(value); }
        /// <summary>
        /// Whether to automatically resume script playback when user submits the input form.
        /// </summary>
        [CommandParameter("play", true)]
        public bool PlayOnSubmit { get => GetDynamicParameter(true); set => SetDynamicParameter(value); }

        public override Task ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var inputUI = Engine.GetService<UIManager>().GetUI<UI.IVariableInputUI>();
            inputUI?.Show(VariableName, Summary, PredefinedValue, PlayOnSubmit);

            return Task.CompletedTask;
        }
    }
}
