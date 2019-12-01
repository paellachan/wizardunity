// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel.UI
{
    public class CustomVariableGUI : MonoBehaviour
    {
        private class Record : IEquatable<Record>
        {
            public string Name = string.Empty, Value = string.Empty, EditedValue = string.Empty;
            public bool Changed => !Value.Equals(EditedValue, StringComparison.Ordinal);

            public override bool Equals (object obj) => obj is Record record && Equals(record);
            public bool Equals (Record other) => Name == other.Name;
            public override int GetHashCode () => 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
            public static bool operator == (Record left, Record right) => left.Equals(right);
            public static bool operator != (Record left, Record right) => !(left == right);
        }

        private const float width = 400;
        private const int windowId = 1;

        private static Rect windowRect = new Rect(Screen.width - width, 0, width, Screen.height * .85f);
        private static Vector2 scrollPos;
        private static bool show;
        private static CustomVariableGUI instance;

        private readonly HashSet<Record> records = new HashSet<Record>();
        private CustomVariableManager variableManager;
        private StateManager stateManager;

        public static void Toggle ()
        {
            if (instance == null)
                instance = Engine.CreateObject<CustomVariableGUI>(nameof(CustomVariableGUI));
            show = !show;
            if (show) instance.UpdateRecords();
        }

        private void Awake ()
        {
            variableManager = Engine.GetService<CustomVariableManager>();
            stateManager = Engine.GetService<StateManager>();
        }

        private void OnEnable ()
        {
            variableManager.OnVariableUpdated += HandleVariableUpdated;
            stateManager.OnGameLoadFinished += HandleGameLoadFinished;
            stateManager.OnResetFinished += UpdateRecords;
            stateManager.OnRollbackFinished += UpdateRecords;
        }

        private void OnDisable ()
        {
            if (variableManager != null)
                variableManager.OnVariableUpdated -= HandleVariableUpdated;
            if (variableManager != null)
            {
                stateManager.OnGameLoadFinished -= HandleGameLoadFinished;
                stateManager.OnResetFinished -= UpdateRecords;
                stateManager.OnRollbackFinished -= UpdateRecords;
            }
        }

        private void OnGUI ()
        {
            if (!show) return;

            windowRect = GUI.Window(windowId, windowRect, DrawWindow, "Custom Variables");
        }

        private void DrawWindow (int windowId)
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (var record in records)
            {
                GUILayout.BeginHorizontal();
                GUILayout.TextField(record.Name, GUILayout.Width(width / 2f - 15));
                record.EditedValue = GUILayout.TextField(record.EditedValue);
                if (record.Changed && GUILayout.Button("SET", GUILayout.Width(50)))
                    variableManager.SetVariableValue(record.Name, record.EditedValue);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Close Window")) show = false;

            GUI.DragWindow();
        }

        private void HandleVariableUpdated (CustomVariableUpdatedArgs args)
        {
            // Checking if the update changed an existing variable.
            foreach (var record in records)
                if (record.Name == args.Name)
                {
                    record.Value = args.Value;
                    record.EditedValue = args.Value;
                    return;
                }
            // Adding a new variable.
            records.Add(new Record { Name = args.Name, Value = args.Value, EditedValue = args.Value });
        }

        private void HandleGameLoadFinished (GameSaveLoadArgs obj) => UpdateRecords();

        private void UpdateRecords ()
        {
            records.Clear();
            records.UnionWith(variableManager.GetAllVariables().Select(kv => new Record { Name = kv.Key, Value = kv.Value, EditedValue = kv.Value }));
        }
    }
}
