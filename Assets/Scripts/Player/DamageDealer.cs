using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    public int damageAmount = 50;
    public string targetTag = "Enemy"; // Default to targeting Enemies
    
    // Track hits to ensure we only deal damage ONCE per activation (swing)
    private System.Collections.Generic.List<Collider2D> hitTargets = new System.Collections.Generic.List<Collider2D>();

    void OnEnable()
    {
        hitTargets.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryDealDamage(other);
    }

    void OnTriggerStay2D(Collider2D other) // Fallback for stationary overlaps
    {
        TryDealDamage(other);
    }

    void TryDealDamage(Collider2D other)
    {
        // 1. Check if already hit this activation
        if (hitTargets.Contains(other)) return;

        // 2. Check Tag match
        if (other.CompareTag(targetTag))
        {
            hitTargets.Add(other); // Mark handled

            // 3. Try to damage Player
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damageAmount);
                return;
            }

            // 4. Try to damage Enemy
            EnemyHealth eh = other.GetComponent<EnemyHealth>();
            if (eh != null)
            {
                eh.TakeDamage(damageAmount);
                return;
            }
        }
    }
}
