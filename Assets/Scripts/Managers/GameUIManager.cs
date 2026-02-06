using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Required for SceneManager

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance; // Singleton

    [Header("Health UI")]
    public Slider healthSlider;
    public Image healthFillImage;
    public Gradient healthColorGradient; // Green -> Red

    [Header("Skill UI")]
    public Image dashCooldownImage; // Filled Image type
    public GameObject dashReadyEffect; // Optional flashing outline

    [Header("Game Panels")]
    public GameObject gameOverPanel;
    public GameObject gameClearPanel; // NEW
    public GameObject bossHUD; // NEW

    [Header("References")]
    public PlayerController playerController;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

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

        // Auto-Find BossHUD if missing
        if (bossHUD == null)
        {
            bossHUD = GameObject.Find("BossHUD");
        }

        // Ensure Panels are hidden on start
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (gameClearPanel) gameClearPanel.SetActive(false);
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

    public void ShowGameOver()
    {
        if (gameOverPanel) 
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void ShowGameClear()
    {
        if (gameClearPanel)
        {
            gameClearPanel.SetActive(true);
            Time.timeScale = 0f; // Pause game? Optional.
            
            // Hide HUD
            if (healthSlider) healthSlider.gameObject.SetActive(false);
            
            if (dashCooldownImage && dashCooldownImage.transform.parent)
            {
                dashCooldownImage.transform.parent.gameObject.SetActive(false);
            }

            // Hide BossHUD
            if (bossHUD) bossHUD.SetActive(false);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Ensure time is running
        string sceneName = SceneManager.GetActiveScene().name;

        // Special case for Final Stage: Reload scene to ensure full reset
        if (sceneName == "Final Stage")
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        // Default behavior: Don't reload scene, just respawn player
        if (playerController)
        {
            // Find Health component to trigger full respawn chain
            var health = playerController.GetComponent<PlayerHealth>();
            if (health)
            {
                health.Respawn();
            }
            else
            {
                // Fallback for controller only
                playerController.Respawn();
            }
        }
        else
        {
            // Fallback if controller missing: Reload Scene
             SceneManager.LoadScene(sceneName);
             return;
        }

        // Hide UI
        if(gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu"); // Make sure scene name matches exactly
    }
}
