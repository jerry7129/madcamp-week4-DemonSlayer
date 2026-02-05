using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public Color hitColor = Color.red;
    private Color originalColor;

    void Start()
    {
        currentHealth = maxHealth;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    // Invincibility Logic (Unused for now)
    // private bool isInvincible = false;
    // public float invincibilityDuration = 0.2f;

    // Triggered when Player's Attack Collider (Trigger) touches this Enemy (Trigger usually)
    void OnTriggerEnter2D(Collider2D other)
    {
        // Removed passive check. Damage is now handled by DamageDealer.cs
    }

    /*
    System.Collections.IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }
    */

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Enemy Hit! remaining HP: {currentHealth}");

        // Flash Red
        if (spriteRenderer != null) StartCoroutine(FlashRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator FlashRoutine()
    {
        if (spriteRenderer) spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(0.1f);
        if (spriteRenderer) spriteRenderer.color = originalColor;
    }

    void Die()
    {
        Debug.Log("Enemy Died!");
        // TODO: Add particle effect?
        Destroy(gameObject);
    }
}
