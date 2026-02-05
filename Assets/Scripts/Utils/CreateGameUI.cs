using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.UI;

public class CreateGameUI : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Create Stylish UI")]
    static void Create()
    {
        // 1. Setup Canvas
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            canvasObj.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 2. Setup Game UI Manager
        GameObject managerObj = GameObject.Find("GameManager");
        if (managerObj == null) managerObj = new GameObject("GameManager");
        GameUIManager uiManager = managerObj.GetComponent<GameUIManager>();
        if (uiManager == null) uiManager = managerObj.AddComponent<GameUIManager>();

        // 3. Create Health Bar Container (Top Left)
        GameObject healthPanel = CreateRect(canvasObj.transform, "HealthPanel", new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -50), new Vector2(400, 50));
        
        // Background
        Image bg = healthPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Dark Grey

        // Fill Area
        GameObject healthFillObj = CreateRect(healthPanel.transform, "Fill", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        // Add padding
        RectTransform fillRect = healthFillObj.GetComponent<RectTransform>();
        fillRect.offsetMin = new Vector2(5, 5);
        fillRect.offsetMax = new Vector2(-5, -5);
        
        Image fillImg = healthFillObj.AddComponent<Image>();
        fillImg.color = new Color(1f, 0.2f, 0.2f, 1f); // Red
        fillImg.type = Image.Type.Simple;

        // Create Slider Component on Parent
        Slider healthSlider = healthPanel.AddComponent<Slider>();
        healthSlider.targetGraphic = bg;
        healthSlider.fillRect = fillRect;
        healthSlider.direction = Slider.Direction.LeftToRight;
        healthSlider.minValue = 0;
        healthSlider.maxValue = 1;
        healthSlider.value = 1;
        healthSlider.interactable = false; // Disable dragging

        // Create standard white sprite for UI rendering
        Sprite whiteSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (whiteSprite == null)
        {
            // Fallback: Generate one if builtin not found (common in some pipelines)
            Texture2D tex = new Texture2D(2, 2);
            tex.SetPixels(new Color[] {Color.white, Color.white, Color.white, Color.white});
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0,0,2,2), Vector2.one * 0.5f);
        }

        // 4. Create Dash Icon (Under Health Bar)
        GameObject dashPanel = CreateRect(canvasObj.transform, "DashPanel", new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, -120), new Vector2(60, 60));
        Image dashBg = dashPanel.AddComponent<Image>();
        dashBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        dashBg.sprite = whiteSprite; // Assign Sprite

        // Flash Icon (Text or Shape)
        GameObject dashIconObj = CreateRect(dashPanel.transform, "Icon", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        Text iconText = dashIconObj.AddComponent<Text>();
        iconText.text = "⚡";
        // iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Often null in newer Unity
        iconText.font = Resources.FindObjectsOfTypeAll<Font>()[0]; // Grab any font
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.resizeTextForBestFit = true;
        iconText.color = Color.yellow;

        // Cooldown Overlay (Darkens when on cooldown)
        GameObject cooldownOverlay = CreateRect(dashPanel.transform, "CooldownOverlay", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        Image cooldownImg = cooldownOverlay.AddComponent<Image>();
        cooldownImg.sprite = whiteSprite; // CRITICAL: Filled type needs a sprite!
        cooldownImg.color = new Color(0, 0, 0, 0.7f);
        cooldownImg.type = Image.Type.Filled;
        cooldownImg.fillMethod = Image.FillMethod.Radial360;
        cooldownImg.fillOrigin = 2; // Top
        cooldownImg.fillClockwise = false;

        // 5. Link to Manager
        uiManager.healthSlider = healthSlider;
        uiManager.healthFillImage = fillImg;
        uiManager.dashCooldownImage = cooldownImg;
        
        // Create Default Gradient (Red -> Yellow -> Green)
        Gradient g = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(Color.red, 0.0f);
        colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
        colorKeys[2] = new GradientColorKey(Color.green, 1.0f);
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
        
        g.SetKeys(colorKeys, alphaKeys);
        uiManager.healthColorGradient = g;

        // Force initial color update
        fillImg.color = Color.green; // Start Green

        // 6. Link Player
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player)
        {
            uiManager.playerController = player;
            PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
            if (pHealth == null) pHealth = player.gameObject.AddComponent<PlayerHealth>();

            // Note: UnityEvents are hard to link via script persistence without SerializedObject magic,
            // so we will rely on GameUIManager finding the player itself, 
            // OR we update PlayerHealth to auto-find the manager if event is empty.
            // Let's make PlayerHealth robust.
        }

        Selection.activeGameObject = canvasObj;
        Debug.Log("UI Created & Linked!");
    }

    static GameObject CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return obj;
    }
#endif
}
