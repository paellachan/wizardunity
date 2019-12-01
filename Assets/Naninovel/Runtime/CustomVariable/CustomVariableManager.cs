// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityCommon;

namespace Naninovel
{
    /// <summary>
    /// Manages variables set by user in the naninovel scripts.
    /// </summary>
    [InitializeAtRuntime]
    public class CustomVariableManager : IStatefulService<GameStateMap>, IStatefulService<GlobalStateMap>
    {
        [System.Serializable]
        private class GlobalState
        {
            public SerializableLiteralStringMap GlobalVariableMap;
        }

        [System.Serializable]
        private class GameState
        {
            public SerializableLiteralStringMap LocalVariableMap;
        }

        /// <summary>
        /// Invoked when a custom variable is created or its value changed.
        /// </summary>
        public event Action<CustomVariableUpdatedArgs> OnVariableUpdated;

        /// <summary>
        /// Custom variable name prefix (case-insensitive) used to indicate a global variable.
        /// </summary>
        public const string GlobalPrefix = "G_";

        private readonly CustomVariablesConfiguration config;
        private readonly SerializableLiteralStringMap globalVariableMap;
        private readonly SerializableLiteralStringMap localVariableMap;

        public CustomVariableManager (CustomVariablesConfiguration config)
        {
            this.config = config;
            globalVariableMap = new SerializableLiteralStringMap();
            localVariableMap = new SerializableLiteralStringMap();
        }

        public Task InitializeServiceAsync () => Task.CompletedTask;

        public void ResetService ()
        {
            ResetLocalVariables();
        }

        public void DestroyService () { }

        public Task SaveServiceStateAsync (GlobalStateMap stateMap)
        {
            var state = new GlobalState {
                GlobalVariableMap = new SerializableLiteralStringMap(globalVariableMap)
            };
            stateMap.SetState(state);
            return Task.CompletedTask;
        }

        public Task LoadServiceStateAsync (GlobalStateMap stateMap)
        {
            ResetGlobalVariables();

            var state = stateMap.GetState<GlobalState>();
            if (state is null) return Task.CompletedTask;

            foreach (var kv in state.GlobalVariableMap)
                globalVariableMap[kv.Key] = kv.Value;
            return Task.CompletedTask;
        }

        public Task SaveServiceStateAsync (GameStateMap stateMap)
        {
            var state = new GameState {
                LocalVariableMap = new SerializableLiteralStringMap(localVariableMap)
            };
            stateMap.SetState(state);
            return Task.CompletedTask;
        }

        public Task LoadServiceStateAsync (GameStateMap stateMap)
        {
            ResetLocalVariables();

            var state = stateMap.GetState<GameState>();
            if (state is null) return Task.CompletedTask;

            foreach (var kv in state.LocalVariableMap)
                localVariableMap[kv.Key] = kv.Value;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Checks whether a custom variable with the provided name is global.
        /// </summary>
        public bool IsGlobalVariable (string name) => name.StartsWith(GlobalPrefix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks whether a variable with the provided name exists.
        /// </summary>
        public bool VariableExists (string name) => IsGlobalVariable(name) ? globalVariableMap.ContainsKey(name) : localVariableMap.ContainsKey(name);

        /// <summary>
        /// Attempts to retrive value of a variable with the provided name. Variable names are case-insensitive. 
        /// When no variables of the provided name are found will return null.
        /// </summary>
        public string GetVariableValue (string name)
        {
            if (!VariableExists(name)) return null;
            return IsGlobalVariable(name) ? globalVariableMap[name] : localVariableMap[name];
        }

        /// <summary>
        /// Retrieves all the local variables (name -> value).
        /// </summary>
        public Dictionary<string, string> GetAllGlobalVariables () => globalVariableMap.ToDictionary(kv => kv.Key, kv => kv.Value);

        /// <summary>
        /// Retrieves all the global variables (name -> value).
        /// </summary>
        public Dictionary<string, string> GetAllLocalVariables () => localVariableMap.ToDictionary(kv => kv.Key, kv => kv.Value);

        /// <summary>
        /// Retrieves all the custom variables (name -> value), both global and local.
        /// </summary>
        public Dictionary<string, string> GetAllVariables ()
        {
            var result = new Dictionary<string, string>();
            foreach (var kv in globalVariableMap)
                result.Add(kv.Key, kv.Value);
            foreach (var kv in localVariableMap)
                result.Add(kv.Key, kv.Value);
            return result;
        }

        /// <summary>
        /// Sets value of a variable with the provided name. Variable names are case-insensitive. 
        /// When no variables of the provided name are found, will add a new one and assign the value.
        /// In case the name is starting with <see cref="GlobalPrefix"/>, the variable will be added to the global scope.
        /// </summary>
        public void SetVariableValue (string name, string value)
        {
            var isGlobal = IsGlobalVariable(name);
            var initialValue = default(string);

            if (isGlobal)
            {
                globalVariableMap.TryGetValue(name, out initialValue);
                globalVariableMap[name] = value;
            }
            else
            {
                localVariableMap.TryGetValue(name, out initialValue);
                localVariableMap[name] = value;
            }

            if (initialValue != value)
                OnVariableUpdated?.Invoke(new CustomVariableUpdatedArgs(name, value, initialValue));
        }

        /// <summary>
        /// Purges all the custom local state variables and restores the 
        /// pre-defined values specified in the service configuration.
        /// </summary>
        public void ResetLocalVariables ()
        {
            localVariableMap?.Clear();

            foreach (var varData in config.PredefinedVariables)
            {
                if (IsGlobalVariable(varData.Name)) continue;
                SetVariableValue(varData.Name, varData.Value);
            }
        }

        /// <summary>
        /// Purges all the custom global state variables and restores the
        /// pre-defined values specified in the service configuration.
        /// </summary>
        public void ResetGlobalVariables ()
        {
            globalVariableMap?.Clear();

            foreach (var varData in config.PredefinedVariables)
            {
                if (!IsGlobalVariable(varData.Name)) continue;
                SetVariableValue(varData.Name, varData.Value);
            }
        }

        /// <summary>
        /// Attempts to parse the provided value string into float (the string should contain a dot), integer and then boolean.
        /// When parsing fails will return the initial string.
        /// </summary>
        public static object ParseVariableValue (string value)
        {
            if (value.Contains(".") && ParseUtils.TryInvariantFloat(value, out var floatValue)) return floatValue;
            else if (ParseUtils.TryInvariantInt(value, out var intValue)) return intValue;
            else if (bool.TryParse(value, out var boolValue)) return boolValue;
            else return value;
        }
    }
}
