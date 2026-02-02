using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Added for Input

public class GravityManager : MonoBehaviour
{
    [Header("Settings")]
    public float minTime = 5f;
    public float maxTime = 15f;
    public AudioClip biwaSound;

    [Header("References")]
    public Transform playerTransform;
    public Transform cameraTransform;

    private AudioSource audioSource;
    private bool isPaused = false; // Pause state

    void Start()
    {
        // Auto-find references if missing
        if (playerTransform == null && GameObject.Find("Zenitsu"))
            playerTransform = GameObject.Find("Zenitsu").transform;
            
        if (cameraTransform == null && Camera.main)
            cameraTransform = Camera.main.transform;

        audioSource = gameObject.AddComponent<AudioSource>();
        
        StartCoroutine(RandomGravityRoutine());
    }

    void Update()
    {
        // Toggle Pause on 'P' key
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            isPaused = !isPaused;
        }
    }

    IEnumerator RandomGravityRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minTime, maxTime);
            float timer = 0f;

            // Wait for time to pass (handling pause)
            while (timer < waitTime)
            {
                if (!isPaused)
                {
                    timer += Time.deltaTime;
                }
                yield return null;
            }

            if (biwaSound) audioSource.PlayOneShot(biwaSound);

            // Random Gravity Direction (Ensure change)
            Vector2[] directions = { Vector2.down, Vector2.up, Vector2.left, Vector2.right };
            Vector2 currentDir = Physics2D.gravity.normalized;
            Vector2 newGravity;
            
            do
            {
                newGravity = directions[Random.Range(0, directions.Length)];
            } while (Vector2.Distance(newGravity, currentDir) < 0.1f);

            // Calculate rotation angle change (From Old Gravity to New Gravity)
            float rotationDiff = Vector2.SignedAngle(currentDir, newGravity);

            // Apply Gravity
            Physics2D.gravity = newGravity * 9.81f;

            // Reset Player Velocity to stop "Ascent" from previous gravity
            if (playerTransform != null)
            {
                Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero; 
                }
                
                // NEW: Pause Controls during rotation
                // This forces the player to re-press or simply waits until rotation aligns
                PlayerController pc = playerTransform.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.PauseControls(0.5f); // Duration matches RotateObject's duration
                }
            }

            // NEW: Apply same logic to Doppelgangers (Clones)
            DoppelgangerAI[] clones = FindObjectsByType<DoppelgangerAI>(FindObjectsSortMode.None);
            foreach (var clone in clones)
            {
                // 1. Reset Velocity
                if (clone.GetComponent<Rigidbody2D>()) 
                    clone.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                
                // 2. Pause AI
                clone.PauseAI(0.5f);
            }

            // Determine Rotation Angle (Z-axis) based on Gravity
            float targetZ = 0f;
            if (newGravity == Vector2.right) targetZ = 90f;
            else if (newGravity == Vector2.left) targetZ = -90f;
            else if (newGravity == Vector2.up) targetZ = 180f;

            // Rotate Camera only (Player rotates itself via AlignToGravity)
            if (cameraTransform) StartCoroutine(RotateObject(cameraTransform, targetZ));
        }
    }

    IEnumerator RotateObject(Transform target, float targetZ)
    {
        Quaternion startRot = target.rotation;
        Quaternion endRot = Quaternion.Euler(0, 0, targetZ);
        
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            yield return null;
        }
        target.rotation = endRot;
    }
}