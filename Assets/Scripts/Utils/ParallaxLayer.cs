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
        if (mainCam == null) mainCam = FindFirstObjectByType<Camera>(); // Fallback
        
        if (mainCam != null)
        {
            cameraTransform = mainCam.transform;
            lastCameraPosition = cameraTransform.position;
            // Debug.Log($"ParallaxLayer: Camera found! {mainCam.name}");
        }
        else
        {
            Debug.LogError("ParallaxLayer: NO CAMERA FOUND! The background will not move.");
        }

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            Texture2D texture = sprite.sprite.texture;
            textureUnitSizeX = texture.width / sprite.sprite.pixelsPerUnit;
            textureUnitSizeY = texture.height / sprite.sprite.pixelsPerUnit;
        }
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
            if (Mathf.Abs(cameraTransform.position.x - transform.position.x) >= textureUnitSizeX)
            {
                float offsetPositionX = (cameraTransform.position.x - transform.position.x) % textureUnitSizeX;
                transform.position = new Vector3(cameraTransform.position.x + offsetPositionX, transform.position.y, transform.position.z);
            }
        }
        
        if (infiniteVertical)
        {
             if (Mathf.Abs(cameraTransform.position.y - transform.position.y) >= textureUnitSizeY)
            {
                float offsetPositionY = (cameraTransform.position.y - transform.position.y) % textureUnitSizeY;
                transform.position = new Vector3(transform.position.x, cameraTransform.position.y + offsetPositionY, transform.position.z);
            }
        }
    }
}
