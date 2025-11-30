using UnityEngine;

/// <summary>
/// Handles surface attraction forces: suction, slam down, and world rotation bias.
/// This script performs its own raycasting to detect surfaces independently.
/// </summary>
public class SurfaceAttraction : MonoBehaviour
{
    [Header("Raycasting")]
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask groundLayer = -1; // -1 means everything
    [SerializeField] private LayerMask waveLayer; // Layer for wave roads
    [SerializeField] private bool showDebugRays = true;
    
    [Header("Surface Alignment")]
    [SerializeField] private float alignmentSpeed = 5f; // How quickly to rotate to match surface normal
    
    [Header("Surface Suction")]
    [SerializeField] private float suctionForce = 20f; // Force pulling toward surface
    [SerializeField] private float suctionDamping = 2f; // Dampens bounce/oscillation
    [SerializeField] private float suctionDampingDistance = 5f; // Max distance for damping to apply
    [SerializeField] private float suctionActivationDistance = 5f; // Max distance for suction force to apply
    [SerializeField] private bool distanceScaling = true; // Scale force by distance
    [SerializeField] private float maxSuctionDistance = 10f; // Distance at which suction is strongest
    [SerializeField] private float distanceScaleMultiplier = 1f; // Multiplier for distance scaling strength
    [SerializeField] private float minDistanceScale = 0f; // Minimum scale value (floor)
    
    [Header("Slam Down")]
    [SerializeField] private bool enableSlamDown = true; // Toggle slam down force
    [SerializeField] private float slamDownThreshold = 15f; // Distance above surface to trigger slam
    [SerializeField] private float slamDownForce = 100f; // Force applied when slamming down
    [SerializeField] private bool slamTowardSurface = true; // If true, slams toward surface normal; if false, slams toward world down
    
    [Header("World Down Bias")]
    [SerializeField] private float worldDownBiasDistance = 8f; // Distance threshold for world down bias
    [SerializeField] private float worldDownBiasStrength = 2f; // How strongly to rotate toward world down
    
    private Rigidbody _rb;
    private bool _surfaceDetected; // Track if we detected a surface this frame
    private bool _onWaveRoad; // Track if the detected surface is a wave road
    
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        
        if (_rb == null)
        {
            Debug.LogError("SurfaceAttraction requires a Rigidbody component!");
            enabled = false;
            return;
        }
        
        Debug.Log("SurfaceAttraction initialized on " + gameObject.name);
    }
    
    void FixedUpdate()
    {
        ProcessSurfaceAttraction();
    }
    
    /// <summary>
    /// Main processing method - performs raycast and applies appropriate forces
    /// </summary>
    private void ProcessSurfaceAttraction()
    {
        // Cast a ray downward from the object's position
        Vector3 rayStart = transform.position;
        Vector3 rayDirection = -transform.up; // Object's local "down"
        
        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, raycastDistance, groundLayer))
        {
            _surfaceDetected = true;
            
            // Check if we hit a wave road
            _onWaveRoad = ((1 << hit.collider.gameObject.layer) & waveLayer) != 0;
            
            // Debug log when on wave road
            if (_onWaveRoad)
            {
                Debug.Log("ON WAVE ROAD - Max speed limit disabled");
            }
            
            Vector3 surfaceNormal = hit.normal;
            
            // Align to surface normal
            AlignToSurfaceNormal(surfaceNormal);
            
            // Apply suction force (always active when surface detected)
            ApplySuctionForce(hit, surfaceNormal);
            
            // Apply slam down force (if enabled and above threshold)
            if (enableSlamDown && hit.distance > slamDownThreshold)
            {
                ApplySlamDownForce(hit, surfaceNormal);
            }
            
            // Debug visualization
            if (showDebugRays)
            {
                Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.green);
                Debug.DrawRay(hit.point, surfaceNormal * 2f, Color.blue);
            }
        }
        else
        {
            _surfaceDetected = false;
            _onWaveRoad = false;
            
            // No surface detected - apply world down bias
            ApplyWorldDownBias();
            
            // Debug visualization
            if (showDebugRays)
            {
                Debug.DrawRay(rayStart, rayDirection * raycastDistance, Color.red);
            }
        }
    }
    
    /// <summary>
    /// Aligns the object's up direction to match the surface normal
    /// </summary>
    private void AlignToSurfaceNormal(Vector3 surfaceNormal)
    {
        // Calculate the target rotation that aligns our "up" with the surface normal
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, surfaceNormal) * transform.rotation;
        
        // Smoothly rotate towards the target
        Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSpeed * Time.fixedDeltaTime);
        _rb.MoveRotation(newRotation);
    }
    
    /// <summary>
    /// Applies a suction force pulling the object toward the detected surface
    /// </summary>
    private void ApplySuctionForce(RaycastHit hit, Vector3 surfaceNormal)
    {
        // Direction toward the surface (opposite of the surface normal)
        Vector3 towardSurface = -surfaceNormal;
        
        // Calculate distance-based scaling
        float distanceMultiplier = 1f;
        if (distanceScaling)
        {
            // Linear scaling: force increases with distance
            // At distance 0: multiplier = minDistanceScale (floor when touching)
            // At maxSuctionDistance: multiplier = distanceScaleMultiplier (full force when far)
            float normalizedDistance = Mathf.Clamp01(hit.distance / maxSuctionDistance);
            distanceMultiplier = Mathf.Lerp(minDistanceScale, distanceScaleMultiplier, normalizedDistance);
        }
        
        // Apply suction force scaled by distance
        Vector3 force = towardSurface * suctionForce * distanceMultiplier;
        
        // Dampen velocity along the surface normal to prevent bouncing
        // Only apply damping when close to surface (prevents interfering with falling from height)
        Vector3 dampingForce = Vector3.zero;
        if (hit.distance <= suctionDampingDistance)
        {
            float normalVelocity = Vector3.Dot(_rb.linearVelocity, surfaceNormal);
            dampingForce = -surfaceNormal * (normalVelocity * suctionDamping);
        }
        
        // Combine forces
        _rb.AddForce(force + dampingForce, ForceMode.Acceleration);
        
        // Debug: Show force strength
        if (showDebugRays && distanceMultiplier > 0.01f)
        {
            Debug.DrawRay(transform.position, towardSurface * distanceMultiplier * 2f, Color.magenta);
        }
    }
    
    /// <summary>
    /// Slams the player down toward the ground when above a distance threshold
    /// Very aggressive force to keep player grounded
    /// </summary>
    private void ApplySlamDownForce(RaycastHit hit, Vector3 surfaceNormal)
    {
        // Determine slam direction
        Vector3 slamDirection;
        if (slamTowardSurface)
        {
            // Slam toward the surface (perpendicular to surface)
            slamDirection = -surfaceNormal;
        }
        else
        {
            // Slam toward world down
            slamDirection = Vector3.down;
        }
        
        // Apply strong downward force
        Vector3 force = slamDirection * slamDownForce;
        _rb.AddForce(force, ForceMode.Acceleration);
        
        // Debug: Show slam force with red ray
        if (showDebugRays)
        {
            Debug.DrawRay(transform.position, slamDirection * 5f, Color.red);
        }
    }
    
    /// <summary>
    /// When far from any surface, gradually rotate toward world down
    /// </summary>
    private void ApplyWorldDownBias()
    {
        // Target rotation that aligns our up with world up
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
        
        // Smoothly rotate toward world up orientation
        Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, worldDownBiasStrength * Time.fixedDeltaTime);
        _rb.MoveRotation(newRotation);
        
        if (showDebugRays)
        {
            Debug.DrawRay(transform.position, Vector3.down * 2f, Color.yellow);
        }
    }
    
    /// <summary>
    /// Public getter to check if a surface was detected this frame
    /// </summary>
    public bool IsSurfaceDetected()
    {
        return _surfaceDetected;
    }
    
    /// <summary>
    /// Public getter to check if currently on a wave road
    /// </summary>
    public bool IsOnWaveRoad()
    {
        return _onWaveRoad;
    }
    
    // Optional: Visualize raycast range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        // Show raycast direction
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, -transform.up * raycastDistance);
    }
}
