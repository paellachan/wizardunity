// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityCommon;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Stores resource path to editor asset GUID map that is required by <see cref="EditorResourceProvider"/> when running under the Unity editor.
    /// </summary>
    /// <remarks>
    /// When user assign specific project assets for the resources via the editor menus (eg, sprites for character appearance or audio clips for BGM), the assigned asset references are stored in this asset.
    /// Before entering play mode in the editor, all the stored references are added to a <see cref="EditorResourceProvider"/> instance, which is included to the provider lists when the app is running under the editor.
    /// When building the player, the referenced assets are copied to a temp `Resources` folder; this allows the assets to be packaged with the build and makes them available for <see cref="ProjectResourceProvider"/>.
    /// </remarks>
    [System.Serializable]
    public class EditorResources : ScriptableObject
    {
        // Following types are modified by the editor via reflection.
        #pragma warning disable CS0649
        [System.Serializable]
        private class ResourceCategory
        {
            public string Id;
            public List<EditorResource> Resources;
        }

        [System.Serializable]
        private struct EditorResource
        {
            public string Name, PathPrefix, Guid;
            public string Path => $"{PathPrefix ?? string.Empty}/{Name ?? string.Empty}";
        }
        #pragma warning restore CS0649

        [SerializeField] private List<ResourceCategory> resourceCategories = new List<ResourceCategory>();

        private const string providerFieldName = "editorProvider";
        private static string directoryPath => PathUtils.Combine(Application.dataPath, ConfigurationSettings.GeneratedDataPath);
        private static string assetPath => PathUtils.AbsoluteToAssetPath(PathUtils.Combine(directoryPath, $"{nameof(EditorResources)}.asset"));

        /// <summary>
        /// Loads an existing asset from package data folder or creates a new default instance.
        /// </summary>
        public static EditorResources LoadOrDefault ()
        {
            var obj = AssetDatabase.LoadAssetAtPath<EditorResources>(assetPath);

            if (!ObjectUtils.IsValid(obj))
            {
                obj = CreateInstance<EditorResources>();
                System.IO.Directory.CreateDirectory(directoryPath);
                AssetDatabase.CreateAsset(obj, assetPath);
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }

            return obj;
        }

        /// <summary>
        /// Retrieves all the existing resources records in [path] -> [guid] map format.
        /// </summary>
        /// <param name="categoryId">When specified, will only fetch resources under the category.</param>
        /// <param name="skipEmpty">When enabled, will skip records where either path or guid is not defined.</param>
        public Dictionary<string, string> GetAllRecords (string categoryId = null, bool skipEmpty = true)
        {
            var records = new Dictionary<string, string>();

            foreach (var resourceCategory in resourceCategories)
                if (categoryId is null || resourceCategory.Id == categoryId)
                    foreach (var resource in resourceCategory.Resources)
                        if (!skipEmpty || (!string.IsNullOrEmpty(resource.Path) && !string.IsNullOrEmpty(resource.Guid)))
                            records[resource.Path] = resource.Guid;

            return records;
        }

        /// <summary>
        /// Attempts to find an added resource GUID based on its path (prefix + name).
        /// </summary>
        public string GetGuidByPath (string path)
        {
            var result = GetAllRecords().FirstOrDefault(kv => kv.Key == path);
            if (result.Key != path) return null;
            return result.Value;
        }

        /// <summary>
        /// Attempts to find an added resource path (prefix + name) based on its GUID.
        /// </summary>
        public string GetPathByGuid (string guid)
        {
            var result = GetAllRecords().FirstOrDefault(kv => kv.Value == guid);
            if (result.Value != guid) return null;
            return result.Key;
        }

        /// <summary>
        /// Adds a new record; don't forget to save the asset after the modification.
        /// </summary>
        public void AddRecord (string categoryId, string pathPrefix, string name, string guid)
        {
            var resource = new EditorResource { PathPrefix = pathPrefix, Name = name, Guid = guid };
            var category = resourceCategories.Find(c => c.Id == categoryId);
            if (category is null)
            {
                category = new ResourceCategory { Id = categoryId, Resources = new List<EditorResource>() };
                resourceCategories.Add(category);
            }
            category.Resources.Add(resource);
        }

        /// <summary>
        /// Removes all the records with provided GUID; don't forget to save the asset after the modification.
        /// </summary>
        public int RemoveAllRecordsWithGuid (string guid, string categoryId = null)
        {
            var removedCount = 0;
            foreach (var resourceCategory in resourceCategories)
                if (categoryId is null || resourceCategory.Id == categoryId)
                    removedCount += resourceCategory.Resources.RemoveAll(c => c.Guid == guid);
            return removedCount;
        }

        /// <summary>
        /// Removes all the records with provided path (prefix + name); don't forget to save the asset after the modification.
        /// </summary>
        public int RemoveAllRecordsWithPath (string pathPrefix, string name, string categoryId = null)
        {
            var removedCount = 0;
            foreach (var resourceCategory in resourceCategories)
                if (categoryId is null || resourceCategory.Id == categoryId)
                    removedCount += resourceCategory.Resources.RemoveAll(c => c.PathPrefix == pathPrefix && c.Name == name);
            return removedCount;
        }

        /// <summary>
        /// Draws a dropdown selection list of strings fed by existing resource paths records using automatic editor GUI layout.
        /// </summary>
        /// <param name="property">The property for which to assign value of the selected element.</param>
        /// <param name="category">When specified, will only fetch resources under the category.</param>
        /// <param name="pathPrefix">When specified, will only fetch resources under the path prefix and trim the prefix from the values.</param>
        /// <param name="emptyOption">When specified, will include an additional option with the provided name and <see cref="string.Empty"/> value to the list.</param>
        public void DrawPathPopup (SerializedProperty property, string category = null, string pathPrefix = null, string emptyOption = null)
        {
            DrawPathPopup(EditorGUILayout.GetControlRect(), property, category, pathPrefix, emptyOption);
        }

        /// <summary>
        /// Draws a dropdown selection list of strings fed by existing resource paths records using the provided draw rect.
        /// </summary>
        public void DrawPathPopup (Rect rect, SerializedProperty property, string category = null, string pathPrefix = null, string emptyOption = null)
        {
            var options = new List<string>();

            foreach (var resourceCategory in resourceCategories)
                if (category is null || resourceCategory.Id == category)
                    foreach (var resource in resourceCategory.Resources)
                    {
                        if (pathPrefix is null)
                        {
                            options.Add(resource.Path);
                            continue;
                        }
                        var option = resource.Path.GetAfterFirst(pathPrefix + "/");
                        if (!string.IsNullOrEmpty(option))
                            options.Add(option);
                    }

            if (options.Count == 0)
            {
                EditorGUI.PropertyField(rect, property, true);
                return;
            }

            if (emptyOption != null)
                options.Insert(0, emptyOption);

            var curValue = emptyOption != null && string.IsNullOrEmpty(property.stringValue) ? emptyOption : property.stringValue;
            var optionsArray = options.Select(o => new GUIContent(o)).ToArray();
            var label = EditorGUI.BeginProperty(Rect.zero, null, property);
            var curIndex = options.IndexOf(curValue);
            var newIndex = EditorGUI.Popup(rect, label, curIndex, optionsArray);

            var newValue = options.IsIndexValid(newIndex) ? options[newIndex] : options[0];
            if (emptyOption != null && newValue == emptyOption)
                newValue = string.Empty;

            if (property.stringValue != newValue)
                property.stringValue = newValue;

        }

        [InitializeOnLoadMethod]
        private static void InitializeEditorProvider ()
        {
            void InitializeProvider ()
            {
                var records = LoadOrDefault().GetAllRecords();
                var provider = new EditorResourceProvider();
                foreach (var record in records)
                    if (EditorUtils.AssetExistsByGuid(record.Value))
                        provider.AddResourceGuid(record.Key, record.Value);
                var providerField = typeof(ResourceProviderManager).GetField(providerFieldName, BindingFlags.Static | BindingFlags.NonPublic);
                providerField.SetValue(null, provider);
            }

            Engine.OnInitializationStarted -= InitializeProvider;
            Engine.OnInitializationStarted += InitializeProvider;
        }
    }
}
