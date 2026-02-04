using UnityEngine;

[DefaultExecutionOrder(10)] // Run AFTER CameraFollow logic
public class ParallaxLayer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("0 = Static (Normal), 1 = Moves with Camera (Infinite Distance), >1 = Foreground")]
    public Vector2 parallaxEffect = new Vector2(0.5f, 0.5f);

    [Tooltip("If true, the background will repeat infinitely on X axis")]
    public bool infiniteHorizontal = false;
    [Tooltip("If true, the background will repeat infinitely on Y axis")]
    public bool infiniteVertical = false;

    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    private float textureUnitSizeX;
    private float textureUnitSizeY;

    void Start()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = FindFirstObjectByType<Camera>(); 
        
        if (mainCam != null)
        {
            cameraTransform = mainCam.transform;
            lastCameraPosition = cameraTransform.position;
        }

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            Texture2D texture = sprite.sprite.texture;
            // Subtract small overlap (0.05f) to prevent seams/gaps
            textureUnitSizeX = (texture.width / sprite.sprite.pixelsPerUnit) - 0.05f; 
            textureUnitSizeY = (texture.height / sprite.sprite.pixelsPerUnit) - 0.05f;

            // NEW: Create 3x3 Grid of Sidekicks (9 directions total including center)
            if (infiniteHorizontal || infiniteVertical) // Or just always if you want robust coverage
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue; // Skip self

                        // Check if we should generate this neighbor based on flags?
                        // User asked for "9 directions", so we generate the full surrounding grid
                        // to ensure no voids appear even on diagonals.
                        // You might want to restrict this if strictly only horizontal is needed,
                        // but 9-way ensures safety.
                        CreateSidekick(sprite, x * textureUnitSizeX, y * textureUnitSizeY);
                    }
                }
            }
        }
    }

    void CreateSidekick(SpriteRenderer parentSprite, float offsetX, float offsetY)
    {
        GameObject sidekick = new GameObject($"{name}_Sidekick_{offsetX}_{offsetY}");
        sidekick.transform.SetParent(transform);
        sidekick.transform.localPosition = new Vector3(offsetX, offsetY, 0);
        sidekick.transform.localScale = Vector3.one;
        
        SpriteRenderer sr = sidekick.AddComponent<SpriteRenderer>();
        sr.sprite = parentSprite.sprite;
        sr.sortingOrder = parentSprite.sortingOrder;
        sr.color = parentSprite.color;
        
        // Ensure sidekicks don't run Update scripts if any
        // (Since they are just visual children, this is fine)
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        
        // Apply Parallax
        transform.position += new Vector3(deltaMovement.x * parallaxEffect.x, deltaMovement.y * parallaxEffect.y, 0);

        lastCameraPosition = cameraTransform.position;

        // Infinite Scrolling Logic
        if (infiniteHorizontal)
        {
            float worldWidth = textureUnitSizeX * transform.localScale.x;
            if (Mathf.Abs(cameraTransform.position.x - transform.position.x) >= worldWidth)
            {
                float offsetPositionX = (cameraTransform.position.x - transform.position.x) % worldWidth;
                transform.position = new Vector3(cameraTransform.position.x + offsetPositionX, transform.position.y, transform.position.z);
            }
        }
        
        if (infiniteVertical)
        {
             float worldHeight = textureUnitSizeY * transform.localScale.y;
             if (Mathf.Abs(cameraTransform.position.y - transform.position.y) >= worldHeight)
            {
                float offsetPositionY = (cameraTransform.position.y - transform.position.y) % worldHeight;
                transform.position = new Vector3(transform.position.x, cameraTransform.position.y + offsetPositionY, transform.position.z);
            }
        }
    }
}
