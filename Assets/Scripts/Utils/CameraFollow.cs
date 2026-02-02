using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 0, -10f);
    
    [Header("Settings")]
    public float smoothTime = 0.25f;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
            else 
            {
                // Fallback: Find by name if tag is missing
                GameObject zenitsu = GameObject.Find("Zenitsu");
                if(zenitsu) target = zenitsu.transform;
            }
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Goal Position (Target + Offset)
            // We use World Position calculations. Even if camera rotates, 
            // the offset (0,0,-10) in world space keeps it "in front" of the 2D plane.
            Vector3 targetPosition = target.position + offset;
            
            // Smoothly move the camera to the target position
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}
