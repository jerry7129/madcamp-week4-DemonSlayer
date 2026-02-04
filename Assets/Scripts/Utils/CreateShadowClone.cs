#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class CreateShadowClone : MonoBehaviour
{
    [MenuItem("Tools/Create Kaigaku")]
    public static void CreateClone()
    {
        // 1. Find Zenitsu Position
        GameObject player = GameObject.Find("Zenitsu");
        Vector3 spawnPos = Vector3.zero;
        if (player != null)
        {
            spawnPos = player.transform.position + Vector3.right * 3;
        }

        // 2. Load Prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Kaigaku.prefab");
        if (prefab == null) 
        {
            Debug.LogError("Kaigaku.prefab not found at Assets/Prefabs/Enemy/Kaigaku.prefab!");
            return;
        }

        // 3. Instantiate
        GameObject clone = Instantiate(prefab, spawnPos, Quaternion.identity);
        clone.name = "Kaigaku";
        clone.tag = "Enemy"; 
        Undo.RegisterCreatedObjectUndo(clone, "Create Shadow Clone");

        
        Debug.Log("Created Kaigaku from Prefab!");
        
        Selection.activeGameObject = clone;
    }
}
#endif
