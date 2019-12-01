// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityCommon;
using UnityEditor;

namespace Naninovel
{
    public class ScriptAssetPostprocessor : AssetPostprocessor
    {
        private static ScriptsConfiguration configuration = default;
        private static EditorResources editorResources = default;

        private static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var modifiedResources = false;

            foreach (string assetPath in importedAssets)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(ScriptAsset)) continue;

                if (configuration is null)
                    configuration = Configuration.LoadOrDefault<ScriptsConfiguration>();
                if (editorResources is null)
                    editorResources = EditorResources.LoadOrDefault();

                if (!configuration.AutoAddScripts) return;

                // A sad hack to prevent adding managed text and localization documents.
                // TODO: Create a standlone format(s?) for them.
                if (AssetDatabase.LoadAssetAtPath<ScriptAsset>(assetPath).ScriptText != AssetMenuItems.DefaultScriptContent) continue;

                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                var name = (assetPath.Contains("/") ? assetPath.GetAfter("/") : assetPath).GetBefore(".");
                if (guid != null && editorResources.GetPathByGuid(guid) is null)
                {
                    editorResources.AddRecord(configuration.Loader.PathPrefix, configuration.Loader.PathPrefix, name, guid);
                    modifiedResources = true;
                }

                if (modifiedResources)
                {
                    EditorUtility.SetDirty(editorResources);
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}
