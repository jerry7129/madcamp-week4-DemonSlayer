using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Settings")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Events")]
    public UnityEvent OnDeath;
    public UnityEvent<float> OnHealthChanged; // Returns ration (0~1)

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        
        // Auto-Link UI if not linked
        var uiManager = FindAnyObjectByType<GameUIManager>();
        if (uiManager != null)
        {
            OnHealthChanged.AddListener(uiManager.UpdateHealthBar);
        }

        OnHealthChanged?.Invoke(1f);
    }

    // NEW: Sync with Inspector changes
    void OnValidate()
    {
        // Clamp logic
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Only valid if playing (events can't be safely invoked in Edit Mode directly without precautions, 
        // but for runtime debugging it's fine).
        if (Application.isPlaying)
        {
            float ratio = maxHealth > 0 ? currentHealth / maxHealth : 0;
            OnHealthChanged?.Invoke(ratio);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Update UI
        float ratio = currentHealth / maxHealth;
        OnHealthChanged?.Invoke(ratio);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke();
        
        // Optional: Disable controls, play animation, show Game Over screen
        GetComponent<PlayerController>()?.PauseControls(999f);
        GetComponent<Animator>()?.SetTrigger("Die");
    }


}
