using UnityEngine;

/// <summary>
/// Buoyancy system for waves. Applies spring force to keep the player's feet 
/// bouncing along the wave surface when inside or below the wave.
/// When above the wave, lets the hover system take over.
/// </summary>
public class WaveBuoyancy : MonoBehaviour
{
    [Header("Buoyancy Settings")]
    [SerializeField] private float buoyancyForce = 100f; // Spring force pushing player up
    [SerializeField] private float buoyancyDamping = 10f; // Dampening to prevent oscillation
    [SerializeField] private float targetHeightAboveWave = 0.5f; // How high above wave surface to maintain feet
    [SerializeField] private float maxBuoyancyForce = 500f; // Maximum force to prevent instability
    [SerializeField] private float raycastDistance = 20f; // How far to check for waves above/below
    [SerializeField] private LayerMask waveLayer = -1; // Layer(s) to detect as waves (-1 means everything)
    [SerializeField] private bool showDebugRays = true;
    
    [Header("Collider Settings")]
    [SerializeField] private float colliderBottomOffset = 0.5f; // Offset from center to bottom of collider (auto-detected if collider present)
    
    [Header("References")]
    [SerializeField] private SimpleSurfaceAligner surfaceAligner; // Reference to existing hover system
    
    private Rigidbody _rb;
    private Collider _collider;
    private bool _isInWave = false; // Whether we're currently inside/below a wave
    
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        
        if (_rb == null)
        {
            Debug.LogError("WaveBuoyancy requires a Rigidbody component!");
            return;
        }
        
        // Auto-detect collider bottom offset if we have a collider
        if (_collider != null)
        {
            colliderBottomOffset = _collider.bounds.extents.y;
            
            Debug.Log($"WaveBuoyancy: Auto-detected collider bottom offset: {colliderBottomOffset}");
        }
        
        // Auto-find surface aligner if not assigned
        if (surfaceAligner == null)
        {
            surfaceAligner = GetComponent<SimpleSurfaceAligner>();
        }
        
        Debug.Log("WaveBuoyancy initialized on " + gameObject.name);
    }
    
    void FixedUpdate()
    {
        if (_rb == null) return;
        
        CheckWaveBuoyancy();
    }
    
    /// <summary>
    /// Check if we're inside or below a wave and apply buoyancy forces
    /// </summary>
    private void CheckWaveBuoyancy()
    {
        // Get the bottom of the player's collider (feet position)
        Vector3 playerFeet = transform.position - new Vector3(0, colliderBottomOffset, 0);
        
        // Cast a ray upward from feet to detect waves above us - get ALL hits
        RaycastHit[] hits = Physics.RaycastAll(playerFeet, Vector3.up, raycastDistance, waveLayer);
        
        // Find the highest wave object (anything on the waveLayer)
        RaycastHit? highestWaveHit = null;
        float highestY = float.MinValue;
        
        foreach (RaycastHit hit in hits)
        {
            if (hit.point.y > highestY)
            {
                highestY = hit.point.y;
                highestWaveHit = hit;
            }
        }
        
        // Check if we found a wave-tagged object
        if (highestWaveHit.HasValue)
        {
            RaycastHit hit = highestWaveHit.Value;
            
            // Recalculate actual distance from feet to wave surface
            float distanceToWave = hit.point.y - playerFeet.y;
            _isInWave = true;
            ApplyBuoyancyForce(hit, playerFeet, distanceToWave);
            
            // Debug visualization
            if (showDebugRays)
            {
                Debug.DrawRay(playerFeet, Vector3.up * distanceToWave, Color.cyan);
                Debug.DrawRay(hit.point, hit.normal * 2f, Color.blue);
                Debug.Log($"WaveBuoyancy: Hit wave at Y={hit.point.y:F2}, Feet at Y={playerFeet.y:F2}, Distance={distanceToWave:F2}");
            }
        }
        else
        {
            // No wave detected - we're above all waves (use hover)
            _isInWave = false;
            
            // Debug visualization
            if (showDebugRays)
            {
                Debug.DrawRay(playerFeet, Vector3.up * raycastDistance, Color.red);
            }
        }
    }
    
    /// <summary>
    /// Apply spring force to push the player up toward the wave surface
    /// </summary>
    private void ApplyBuoyancyForce(RaycastHit waveHit, Vector3 playerFeet, float distanceToSurface)
    {
        // distanceToSurface is always positive (wave Y - feet Y)
        // If positive: we're below the wave (good!)
        // If negative: shouldn't happen with upward raycast, but handle it
        
        // Calculate how far we are from the target height above the wave
        // Positive heightError = we're too far from wave (need more upward force)
        // Negative heightError = we're too close to wave (need less/downward force)
        float heightError = distanceToSurface - targetHeightAboveWave;
        
        // Spring force: proportional to height error
        float springForce = heightError * buoyancyForce;
        
        // Clamp spring force to prevent instability
        springForce = Mathf.Clamp(springForce, -maxBuoyancyForce, maxBuoyancyForce);
        
        // Dampen velocity in the vertical direction to prevent bouncing
        float verticalVelocity = Vector3.Dot(_rb.linearVelocity, Vector3.up);
        float dampingForce = -verticalVelocity * buoyancyDamping;
        
        // Total upward force
        float totalForce = springForce + dampingForce;
        
        // Always apply force in world UP direction (not surface normal)
        // Because when raycasting from below, the normal might point down (backface)
        Vector3 forceVector = Vector3.up * totalForce;
        _rb.AddForce(forceVector, ForceMode.Acceleration);
        
        // Debug visualization
        if (showDebugRays)
        {
            Debug.DrawRay(transform.position, forceVector.normalized * (Mathf.Abs(totalForce) / buoyancyForce) * 2f, Color.magenta);
            Debug.Log($"WaveBuoyancy Force: distanceToSurface={distanceToSurface:F2}, heightError={heightError:F2}, springForce={springForce:F2}, totalForce={totalForce:F2}, normal={waveHit.normal}");
        }
    }
    
    /// <summary>
    /// Check if player is currently in a wave
    /// </summary>
    public bool IsInWave()
    {
        return _isInWave;
    }
    
    // Visualize in editor
    void OnDrawGizmosSelected()
    {
        // Show player feet position (raycast origin and buoyancy target)
        Vector3 playerFeet = transform.position - new Vector3(0, colliderBottomOffset, 0);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerFeet, 0.3f);
        
        // Show raycast line from feet upward
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(playerFeet, playerFeet + Vector3.up * raycastDistance);
    }
}
