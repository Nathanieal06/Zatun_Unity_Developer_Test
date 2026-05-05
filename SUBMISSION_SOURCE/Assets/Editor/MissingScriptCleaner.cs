using UnityEngine;
using UnityEditor;

public class MissingScriptCleaner
{
    [MenuItem("Tools/Clean Missing Scripts from Prefabs")]
    public static void CleanUp()
    {
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab");
        int totalRemoved = 0;
        foreach (string guid in prefabPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                try
                {
                    using (var editingScope = new PrefabUtility.EditPrefabContentsScope(path))
                    {
                        GameObject contentsRoot = editingScope.prefabContentsRoot;
                        int count = RecursivelyRemoveMissingScripts(contentsRoot);
                        if (count > 0)
                        {
                            Debug.Log($"Removed {count} missing scripts from {path}");
                            totalRemoved += count;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not clean prefab {path}: {e.Message}");
                }
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Missing script cleanup complete! Removed {totalRemoved} missing scripts.");
    }

    private static int RecursivelyRemoveMissingScripts(GameObject obj)
    {
        int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
        foreach (Transform child in obj.transform)
        {
            removedCount += RecursivelyRemoveMissingScripts(child.gameObject);
        }
        return removedCount;
    }
}
