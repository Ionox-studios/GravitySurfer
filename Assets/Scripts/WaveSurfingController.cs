using UnityEngine;

/// <summary>
/// Extension to HoverVehicleController for wave surfing mechanics
/// Adds boost when riding waves in the right direction
/// </summary>
[RequireComponent(typeof(BarycentricAlignment))]
public class WaveSurfingController : MonoBehaviour
{
    [Header("Surfing")]
    [SerializeField] private float waveBoostMultiplier = 1.5f;
    [SerializeField] private float minSurfingAngle = 30f; // Minimum angle to surface for boost
    [SerializeField] private float maxSurfingAngle = 60f; // Optimal angle
    
    [Header("Wave Detection")]
    [SerializeField] private float detectionDistance = 5f;
    [SerializeField] private LayerMask waveLayer;
    
    [Header("Debug")]
    [SerializeField] private bool showSurfingInfo = false;
    
    private BarycentricAlignment alignment;
    private Rigidbody rb;
    private bool isSurfing = false;
    private float surfingBoost = 0f;
    
    void Awake()
    {
        alignment = GetComponent<BarycentricAlignment>();
        rb = GetComponent<Rigidbody>();
    }
    
    void FixedUpdate()
    {
        if (!alignment.IsAlignmentEnabled) return;
        
        UpdateSurfingState();
        ApplySurfingBoost();
    }
    
    private void UpdateSurfingState()
    {
        isSurfing = false;
        surfingBoost = 0f;
        
        //Vector3 surfaceVelocity = alignment.GetSurfaceVelocity();
        //if (surfaceVelocity.magnitude < 0.1f) return;
        
        // Check if vehicle is moving in similar direction to wave
        Vector3 vehicleVelocity = rb.linearVelocity;
        if (vehicleVelocity.magnitude < 1f) return;

        // Calculate angle between vehicle movement and wave movement
        
        // Check angle to surface (for downhill surfing)
        Vector3 surfaceNormal = transform.up;
        float angleToVertical = Vector3.Angle(surfaceNormal, Vector3.up);
        
        // Surfing happens when:
        // 1. Moving with the wave (dotProduct > 0.5)
        // 2. Surface is angled (not flat)
        if (angleToVertical > minSurfingAngle)
        {
            isSurfing = true;

            // Calculate boost based on alignment quality
            float alignmentFactor = 1f;
            float angleFactor = 1f - Mathf.Abs(angleToVertical - maxSurfingAngle) / maxSurfingAngle;
            angleFactor = Mathf.Clamp01(angleFactor);
            
            surfingBoost = alignmentFactor * angleFactor * waveBoostMultiplier;
        }
        
        if (showSurfingInfo)
        {
            Debug.Log($"Surfing: {isSurfing}, Boost: {surfingBoost:F2}, Angle: {angleToVertical:F1}°");
        }
    }
    
    private void ApplySurfingBoost()
    {
        if (!isSurfing) return;
        
        // Apply forward boost when surfing
        Vector3 boostForce = transform.forward * surfingBoost * 10f;
        rb.AddForce(boostForce, ForceMode.Acceleration);
        
        // Optional: Add slight downward force for "sticking" to wave
        rb.AddForce(-transform.up * 5f, ForceMode.Acceleration);
    }
    
    public bool IsSurfing => isSurfing;
    public float CurrentBoost => surfingBoost;
}
