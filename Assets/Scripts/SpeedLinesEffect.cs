using UnityEngine;

public class SpeedLinesEffect : MonoBehaviour
{
    [Header("References")]
    public VehicleController targetVehicle; 
    public ParticleSystem speedLineParticles;

    [Header("Settings")]
    public float activationSpeed = 10f; 
    public float effectMaxSpeed = 60f;
    
    [Header("Colors")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.5f); // White with 50% transparency
    [ColorUsage(true, true)] // This enables HDR intensity if you use URP/HDRP
    public Color boostColor = new Color(0f, 1f, 1f, 1f);    // Bright Cyan/Blue

    [Header("Visual Tweaks")]
    public float maxEmissionRate = 150f; // Increased based on your preference for "prominent"
    public float lineSpeed = -150f;      // Negative to move towards camera

    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;
    private ParticleSystem.MainModule mainModule;

    void Start()
    {
        if (speedLineParticles == null) 
            speedLineParticles = GetComponent<ParticleSystem>();

        emissionModule = speedLineParticles.emission;
        velocityModule = speedLineParticles.velocityOverLifetime;
        mainModule = speedLineParticles.main;

        // Ensure velocity is set up correctly
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.Local;
        mainModule.startSpeed = 0; 
    }

    void Update()
    {
        if (targetVehicle == null) return;

        // 1. Get Data
        float currentSpeed = targetVehicle.GetSpeed();
        bool isBoosting = targetVehicle.IsBoostActive();

        // 2. Handle Color Switching
        // We use Lerp for a smooth transition, or you can snap immediately
        Color targetColor = isBoosting ? boostColor : normalColor;
        mainModule.startColor = Color.Lerp(mainModule.startColor.color, targetColor, Time.deltaTime * 10f);

        // 3. Calculate Speed Intensity
        float speedPercent = Mathf.InverseLerp(activationSpeed, effectMaxSpeed, currentSpeed);
        
        if (isBoosting) 
        {
            speedPercent = Mathf.Max(speedPercent, 1.5f); // Force max intensity during boost
        }

        // 4. Apply Emission (Quantity)
        var rate = emissionModule.rateOverTime;
        rate.constant = Mathf.Lerp(0, maxEmissionRate, Mathf.Clamp01(speedPercent));
        emissionModule.rateOverTime = rate;

        // 5. Apply Speed (Velocity)
        velocityModule.z = Mathf.Lerp(-10f, lineSpeed, speedPercent);
    }
}