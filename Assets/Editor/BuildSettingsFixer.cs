using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BuildSettingsFixer
{
    [MenuItem("Tools/Fix Build Settings")]
    public static void FixScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        
        // Add MainMenu first (Index 0)
        string menuPath = "Assets/Scenes/MainMenu.unity";
        if (System.IO.File.Exists(menuPath))
            scenes.Add(new EditorBuildSettingsScene(menuPath, true));
        else
            Debug.LogError($"Could not find {menuPath}");

        // Add Map second (Index 1)
        string mapPath = "Assets/Scenes/Map.unity";
        if (System.IO.File.Exists(mapPath))
            scenes.Add(new EditorBuildSettingsScene(mapPath, true));
        else
            Debug.LogError($"Could not find {mapPath}");

        // Apply
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Build Settings Updated: MainMenu (0), Map (1)");
    }
}
