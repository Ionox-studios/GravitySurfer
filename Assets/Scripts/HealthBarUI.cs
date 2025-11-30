using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the PlayerHealth script. If empty, will try to find it in the scene.")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("The image that will cover the health icons. Set Image Type to 'Filled', Fill Method to 'Horizontal', and Fill Origin to 'Right'.")]
    [SerializeField] private Image damageOverlayImage;

    void Start()
    {
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            // Subscribe to health changes
            playerHealth.OnHealthChanged += UpdateHealthUI;
            
            // Initial update
            UpdateHealthUI(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
        }
        else
        {
            Debug.LogError("HealthBarUI: PlayerHealth script not found in the scene!");
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (damageOverlayImage != null)
        {
            // Calculate health percentage (0 to 1)
            float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
            
            // The overlay should cover the health bar as health decreases.
            // If health is 100% (1.0), overlay should be 0% (0.0) visible.
            // If health is 0% (0.0), overlay should be 100% (1.0) visible.
            // So overlay fill amount = 1 - healthPercent.
            
           // damageOverlayImage.fillAmount = 1f - healthPercent;
           damageOverlayImage.fillAmount = healthPercent;        }
    }
}
