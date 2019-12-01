// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Naninovel
{
    public class UISettings : ConfigurationSettings<UIConfiguration>
    {
        protected override string HelpUri => "guide/ui-customization.html";

        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers => new Dictionary<string, Action<SerializedProperty>> {
            [nameof(UIConfiguration.ObjectsLayer)] = (property) => {
                var label = EditorGUI.BeginProperty(Rect.zero, null, property);
                property.intValue = EditorGUILayout.LayerField(label, property.intValue);
            },
            [nameof(UIConfiguration.DefaultUI)] = DrawDefaultUI,
            [nameof(UIConfiguration.CustomUI)] = null
        };

        private ReorderableList customUIList;

        protected override void DrawConfigurationEditor ()
        {
            DrawDefaultEditor();

            EditorGUILayout.Space();

            // Always check list's serialized object parity with the inspected object.
            if (customUIList is null || customUIList.serializedProperty.serializedObject != SerializedObject)
                InitilizeCustomUIList();

            customUIList.DoLayoutList();
        }

        private void InitilizeCustomUIList ()
        {
            customUIList = new ReorderableList(SerializedObject, SerializedObject.FindProperty(nameof(UIConfiguration.CustomUI)), true, true, true, true);
            customUIList.drawHeaderCallback = DrawCustomUIListHeader;
            customUIList.drawElementCallback = DrawCustomUIListElement;
        }

        private void DrawCustomUIListHeader (Rect rect)
        {
            var label = EditorGUI.BeginProperty(Rect.zero, null, customUIList.serializedProperty);
            var propertyRect = new Rect(5 + rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(propertyRect, label);
        }

        private void DrawCustomUIListElement (Rect rect, int index, bool isActive, bool isFocused)
        {
            var elementProperty = customUIList.serializedProperty.GetArrayElementAtIndex(index);
            var propertyRect = new Rect(rect.x, rect.y + EditorGUIUtility.standardVerticalSpacing, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.ObjectField(propertyRect, elementProperty, typeof(GameObject), GUIContent.none);
        }

        private void DrawDefaultUI (SerializedProperty property)
        {
            EditorGUILayout.Space();

            var label = EditorGUI.BeginProperty(Rect.zero, null, property);
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var defaultLabelWidth = EditorGUIUtility.labelWidth;

                for (int i = 0; i < property.arraySize; i++)
                {
                    var dataProperty = property.GetArrayElementAtIndex(i);
                    var hidden = dataProperty.FindPropertyRelative(nameof(DefaultUIData.Hidden)).boolValue;
                    if (hidden) continue;

                    var name = dataProperty.FindPropertyRelative(nameof(DefaultUIData.Name)).stringValue;
                    var path = dataProperty.FindPropertyRelative(nameof(DefaultUIData.ResourcePath)).stringValue;
                    var enabledProperty = dataProperty.FindPropertyRelative(nameof(DefaultUIData.Enabled));
                    var prefabProperty = dataProperty.FindPropertyRelative(nameof(DefaultUIData.CustomPrefab));
                    
                    var rect = EditorGUILayout.GetControlRect();
                    EditorGUIUtility.labelWidth = 0;
                    EditorGUI.LabelField(new Rect(rect.x, rect.y, 30, rect.height), new GUIContent(string.Empty, enabledProperty.boolValue ? $"Uncheck to disable the `{name}` built-in UI." : $"Check to enable the `{name}` built-in UI."));
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, defaultLabelWidth, rect.height), enabledProperty, GUIContent.none);
                    rect.x += 17;
                    EditorGUIUtility.labelWidth = defaultLabelWidth - rect.x;
                    rect = EditorGUI.PrefixLabel(rect, new GUIContent(name, path));

                    EditorGUI.BeginDisabledGroup(!enabledProperty.boolValue);
                    if (enabledProperty.boolValue) EditorGUI.LabelField(rect, new GUIContent(string.Empty, $"Assign a custom prefab to override the `{name}` built-in UI."));
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 20, rect.height), prefabProperty, GUIContent.none);
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
