using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Health UI")]
    public Slider healthSlider;
    public Image healthFillImage;
    public Gradient healthColorGradient; // Green -> Red

    [Header("Skill UI")]
    public Image dashCooldownImage; // Filled Image type
    public GameObject dashReadyEffect; // Optional flashing outline

    [Header("References")]
    public PlayerController playerController;

    void Start()
    {
        if (playerController == null)
        {
            // Prioritize object with "Player" tag
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) 
            {
                playerController = playerObj.GetComponent<PlayerController>();
            }
            
            // Fallback
            // Fallback
            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }
        }

        // Init Health Color
        if (healthFillImage && healthColorGradient != null)
            healthFillImage.color = healthColorGradient.Evaluate(1f);
            
        // Auto-Find Slider if missing
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
        }
    }

    void Update()
    {
        UpdateDashUI();
    }

    public void UpdateHealthBar(float ratio)
    {
        if (healthSlider) 
        {
            healthSlider.value = ratio;
        }
        
        if (healthFillImage && healthColorGradient != null)
        {
            healthFillImage.color = healthColorGradient.Evaluate(ratio);
        }
    }

    private void UpdateDashUI()
    {
        if (playerController == null || dashCooldownImage == null) return;

        float ratio = playerController.GetDashCooldownRatio();
        dashCooldownImage.fillAmount = ratio;
        
        if (dashReadyEffect)
            dashReadyEffect.SetActive(ratio >= 1f);
    }
}
