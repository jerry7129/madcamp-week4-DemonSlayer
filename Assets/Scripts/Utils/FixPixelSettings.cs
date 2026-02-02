#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class FixPixelSettings : MonoBehaviour
{
    [MenuItem("Tools/Fix Pixel Art Settings (PPU 32)")]
    public static void Fix()
    {
        string[] folders = new string[] { "Assets/Sprites/Zenitsu" };
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", folders);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                importer.spritePixelsPerUnit = 32; // Set 1 Unit = 32 Pixels (Original)
                importer.filterMode = FilterMode.Point; // No Blur
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                Debug.Log($"Fixed settings for: {path}");
            }
        }
    }
}
#endif
