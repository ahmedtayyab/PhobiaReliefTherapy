using UnityEngine;
using UnityEditor;

namespace PhobiaReliefTherapy.Editor
{
    public class FindMissingScripts : EditorWindow
    {
        [MenuItem("Tools/Find & Clean Missing Scripts")]
        public static void FindAndCleanAll()
        {
            // 1. Scan the active scene
            var allObjects = Object.FindObjectsOfType<GameObject>(true);
            int sceneMissingCount = 0;
            int sceneCleanedCount = 0;
            
            foreach (var go in allObjects)
            {
                if (go == null) continue;
                
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missingCount > 0)
                {
                    Debug.LogError($"[Missing Script Detector] Scene GameObject '{go.name}' has {missingCount} missing script(s)!", go);
                    sceneMissingCount += missingCount;
                    
                    // Automatically clean it up
                    int cleaned = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    sceneCleanedCount += cleaned;
                }
            }

            // 2. Scan all project prefabs
            int prefabMissingCount = 0;
            int prefabCleanedCount = 0;
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            
            foreach (string path in allAssetPaths)
            {
                if (!path.StartsWith("Assets/") || !path.EndsWith(".prefab")) continue;
                
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab);
                    if (missingCount > 0)
                    {
                        Debug.LogError($"[Missing Script Detector] Prefab Asset '{path}' has {missingCount} missing script(s)!", prefab);
                        prefabMissingCount += missingCount;
                        
                        int cleaned = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefab);
                        prefabCleanedCount += cleaned;
                        
                        if (cleaned > 0)
                        {
                            EditorUtility.SetDirty(prefab);
                        }
                    }
                }
            }
            
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[Missing Script Detector] Scan complete.\n" +
                      $"Scene: Found {sceneMissingCount} missing, cleaned {sceneCleanedCount}.\n" +
                      $"Prefabs: Found {prefabMissingCount} missing, cleaned {prefabCleanedCount}.");
        }
    }
}
