#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class CreateShadowClone : MonoBehaviour
{
    [MenuItem("Tools/Create Shadow Clone (From Player)")]
    public static void CreateClone()
    {
        // 1. Find Zenitsu
        GameObject player = GameObject.Find("Zenitsu");
        if (player == null)
        {
            Debug.LogError("Zenitsu object not found!");
            return;
        }

        // 2. Duplicate
        GameObject clone = Instantiate(player, player.transform.position + Vector3.right * 3, Quaternion.identity);
        clone.name = "Zenitsu_Shadow";
        clone.tag = "Enemy"; // Distinguish from Player
        Undo.RegisterCreatedObjectUndo(clone, "Create Shadow Clone");

        // 3. Remove Player Components
        DestroyImmediate(clone.GetComponent("PlayerController")); 
        DestroyImmediate(clone.GetComponent("ContextInput")); 
        DestroyImmediate(clone.GetComponent<PlayerHealth>()); // Remove PlayerHealth to avoid UI link
        // Remove Cam Follow if accidentally attached
        // Remove Cam Follow if accidentally attached
        DestroyImmediate(clone.GetComponent("CameraFollow")); 

        // 4. Clean up other components we don't need on AI (like AudioListener)
        DestroyImmediate(clone.GetComponent<AudioListener>());

        // 5. Add AI Components
        DoppelgangerAI ai = clone.GetComponent<DoppelgangerAI>();
        if (!ai) ai = clone.AddComponent<DoppelgangerAI>();
        
        if (!clone.GetComponent<EnemyHealth>()) clone.AddComponent<EnemyHealth>();
        
        // 5.a Set Layer to "Enemy" (Important for penetration/detection)
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) clone.layer = enemyLayer;

        // 5.b Setup Attack Hitbox (Explicit Assignment)
        Transform attackChild = clone.transform.Find("NormalAttackCollider");
        if (attackChild)
        {
            ai.attackHitbox = attackChild.gameObject;
            DamageDealer dd = attackChild.GetComponent<DamageDealer>();
            if (dd) dd.targetTag = "Player"; // Hit Player
        }

        // 5.c Setup Ground Check & Jump (Explicit Assignment)
        Transform groundCheck = clone.transform.Find("GroundCheck");
        if (groundCheck)
        {
            ai.groundCheck = groundCheck;
            
            // Assign Ground Layer (Try to get from Project settings or default to Ground)
            int groundLayerIndex = LayerMask.NameToLayer("Ground");
            if (groundLayerIndex != -1)
                ai.groundLayer = 1 << groundLayerIndex;
            else
                ai.groundLayer = LayerMask.GetMask("Default", "Ground", "Wall");
        }
        
        // 6. Ensure Rigidbody is Kinematic or Dynamic (AI needs Dynamic usually for gravity, or Kinematic + script)
        // Player is Dynamic, so clone should be Dynamic.
        Rigidbody2D rb = clone.GetComponent<Rigidbody2D>();
        if (rb) rb.gravityScale = 3f; // Same as player

        // 7. Visuals (Dark Tint is handled in AI Start, but let's set nice name)
        Debug.Log("Created Shadow Clone! It will chase you.");
        
        Selection.activeGameObject = clone;
    }
}
#endif
