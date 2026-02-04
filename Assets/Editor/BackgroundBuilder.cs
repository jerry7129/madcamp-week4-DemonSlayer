using UnityEngine;
using UnityEditor;

public class BackgroundBuilder
{
    [MenuItem("Tools/Setup Backgrounds")]
    public static void Setup()
    {
        string pathBase = "Assets/Sprites/Environment/background-layer";
        
        GameObject parent = GameObject.Find("Environment_Group");
        if (parent == null) parent = new GameObject("Environment_Group");

        // Settings to iterate: [FilePath, Name, Order, ParallaxX, ParallaxY]
        var layers = new (string s, string n, int o, float px, float py)[] {
            ("1.png", "Layer_1_Back", -20, 1.0f, 0.9f),  // Sky: Follows camera almost perfectly
            ("2.png", "Layer_2_Mid",  -10, 0.6f, 0.2f),  // Mid: Moves somewhat
            ("3.png", "Layer_3_Front", -5, 0.2f, 0.05f)  // Front: Mostly static relative to world
        };

        foreach (var (suffix, name, order, px, py) in layers)
        {
            string fullPath = pathBase + suffix;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);

            if (sprite == null)
            {
                Debug.LogError($"Could not find sprite at {fullPath}");
                continue;
            }

            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                obj.transform.SetParent(parent.transform);
                obj.transform.localPosition = Vector3.zero;
                // Scale up a bit to cover screen
                obj.transform.localScale = Vector3.one * 2f; 
            }

            // Sprite Renderer
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;

            // Parallax
            ParallaxLayer pl = obj.GetComponent<ParallaxLayer>();
            if (pl == null) pl = obj.AddComponent<ParallaxLayer>();
            pl.parallaxEffect = new Vector2(px, py);
            pl.infiniteHorizontal = true; 
            pl.infiniteVertical = false; // Usually only horizontal needed for runners/platformers
        }

        Debug.Log("Backgrounds Setup Complete!");
    }
}
