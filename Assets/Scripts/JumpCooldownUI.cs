using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a visual indicator for the jump cooldown status.
/// Shows "JUMP" text with a fill image that gradually fills up as the cooldown recharges.
/// </summary>
public class JumpCooldownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VehicleController vehicleController;
    [SerializeField] private Image fillImage; // Image that fills up over time
    [SerializeField] private TextMeshProUGUI jumpText; // "JUMP" text (optional - for color changes)
    
    [Header("Visual Settings")]
    [SerializeField] private Color readyColor = Color.green; // Color when jump is ready
    [SerializeField] private Color cooldownColor = Color.red; // Color during cooldown
    [SerializeField] private bool useGradient = true; // Smoothly transition colors based on fill amount
    
    private float lastJumpTime = -999f; // Track when jump was last used
    private float jumpCooldown = 1f; // Cache the cooldown duration
    
    void Start()
    {
        // Find vehicle controller if not assigned
        if (vehicleController == null)
        {
            vehicleController = FindObjectOfType<VehicleController>();
            
            if (vehicleController == null)
            {
                Debug.LogError("JumpCooldownUI: No VehicleController found in scene!");
                enabled = false;
                return;
            }
        }
        
        // Validate references
        if (fillImage == null)
        {
            Debug.LogError("JumpCooldownUI: Fill Image not assigned!");
            enabled = false;
            return;
        }
        
        // Get the jump cooldown value from VehicleController
        jumpCooldown = vehicleController.GetJumpCooldown();
        
        // Initialize UI
        UpdateUI(1f); // Start at ready state
    }
    
    void Update()
    {
        // Calculate fill amount based on time since last jump
        float timeSinceJump = Time.time - vehicleController.GetLastJumpTime();
        float fillAmount = Mathf.Clamp01(timeSinceJump / jumpCooldown);
        
        // Update the UI
        UpdateUI(fillAmount);
    }
    
    /// <summary>
    /// Update the fill image and text color based on cooldown status
    /// </summary>
    private void UpdateUI(float fillAmount)
    {
        // Update fill amount
        fillImage.fillAmount = fillAmount;
        
        // Update colors
        Color currentColor = useGradient 
            ? Color.Lerp(cooldownColor, readyColor, fillAmount)
            : (fillAmount >= 1f ? readyColor : cooldownColor);
        
        fillImage.color = currentColor;
        
        // Update text color if assigned
        if (jumpText != null)
        {
            jumpText.color = currentColor;
        }
    }
}
