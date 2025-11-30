using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays boost meter as a circular fill image that changes color based on boost state
/// </summary>
public class BoostMeterUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VehicleController vehicleController;
    [SerializeField] private Image fillImage; // The circular fill image
    
    [Header("Colors")]
    [SerializeField] private Color readyColor = Color.green; // Boost fully charged
    [SerializeField] private Color chargingColor = Color.yellow; // Boost recharging
    [SerializeField] private Color activeColor = Color.cyan; // Boost active
    [SerializeField] private Color depletedColor = Color.red; // Boost depleted/on cooldown
    
    void Update()
    {
        if (vehicleController == null || fillImage == null)
            return;
        
        UpdateBoostMeter();
    }
    
    private void UpdateBoostMeter()
    {
        // Get boost state from vehicle controller
        bool isBoostActive = vehicleController.IsBoostActive();
        bool boostAvailable = vehicleController.IsBoostAvailable();
        float boostTimeRemaining = vehicleController.GetBoostTimeRemaining();
        float boostCooldownRemaining = vehicleController.GetBoostCooldownRemaining();
        float boostDuration = vehicleController.GetBoostDuration();
        float boostCooldown = vehicleController.GetBoostCooldown();
        
        // Update fill amount and color based on state
        if (isBoostActive)
        {
            // Boost is active - show remaining boost time
            fillImage.fillAmount = boostTimeRemaining / boostDuration;
            fillImage.color = activeColor;
        }
        else if (boostAvailable)
        {
            // Boost is ready to use
            fillImage.fillAmount = 1f;
            fillImage.color = readyColor;
        }
        else
        {
            // Boost is recharging
            float chargeProgress = 1f - (boostCooldownRemaining / boostCooldown);
            fillImage.fillAmount = chargeProgress;
            
            // Color transition: red (0%) → yellow (50%) → green (100%)
            if (chargeProgress < 0.5f)
            {
                // 0% to 50%: Depleted (red) → Charging (yellow)
                fillImage.color = Color.Lerp(depletedColor, chargingColor, chargeProgress / 0.5f);
            }
            else
            {
                // 50% to 100%: Charging (yellow) → Ready (green)
                fillImage.color = Color.Lerp(chargingColor, readyColor, (chargeProgress - 0.5f) / 0.5f);
            }
        }
    }
}
