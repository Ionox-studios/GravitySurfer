using UnityEngine;

/// <summary>
/// Minimal controller with basic movement and surface normal alignment.
/// Aligns the object's bottom to the surface normal as you approach a surface.
/// </summary>
public class SimpleSurfaceAligner : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float jumpForce = 10f;
    
    [Header("Surface Alignment")]
    [SerializeField] private float alignmentSpeed = 5f;
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask groundLayer = -1; // -1 means everything
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private float worldDownBiasDistance = 8f; // Distance threshold for world down bias
    [SerializeField] private float worldDownBiasStrength = 2f; // How strongly to rotate toward world down
    
    [Header("Surface Suction")]
    [SerializeField] private float suctionForce = 20f; // Force pulling toward surface
    [SerializeField] private float suctionDamping = 2f; // Dampens bounce/oscillation
    [SerializeField] private bool distanceScaling = true; // Scale force by distance
    [SerializeField] private float maxSuctionDistance = 10f; // Distance at which suction is strongest
    [SerializeField] private float distanceScaleMultiplier = 1f; // Multiplier for distance scaling strength
    [SerializeField] private float minDistanceScale = 0f; // Minimum scale value (floor)
    
    private Rigidbody _rb;
    private Vector2 _moveInput; // Store input for FixedUpdate
    
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        
        if (_rb == null)
        {
            Debug.LogError("SimpleSurfaceAligner requires a Rigidbody component!");
            return;
        }
        
        // Basic rigidbody setup
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.useGravity = true;
        
        // Freeze rotation so physics won't make it tumble
        // We'll control rotation manually with the alignment system
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
        
        Debug.Log("SimpleSurfaceAligner initialized on " + gameObject.name);
    }
    
    void FixedUpdate()
    {
        // Apply movement
        ApplyMovement();
        
        // Handle surface alignment
        AlignToSurface();
    }
    
    /// <summary>
    /// Move the vehicle based on input (call this from another script or Input system)
    /// </summary>
    public void Move(Vector2 input)
    {
        _moveInput = input;
        
        // Debug to see if input is being received
        if (input.magnitude > 0.1f)
        {
            Debug.Log($"Move input received: {input}");
        }
    }
    
    /// <summary>
    /// Jump - applies an impulse force in the object's up direction
    /// </summary>
    public void Jump()
    {
        if (_rb == null) return;
        
        // Apply jump force in the object's local up direction
        _rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        
        Debug.Log("Jump!");
    }
    
    /// <summary>
    /// Apply the stored movement input
    /// </summary>
    private void ApplyMovement()
    {
        if (_rb == null) return;
        
        // Turning first (so forward direction is updated)
        if (Mathf.Abs(_moveInput.x) > 0.01f)
        {
            float turn = _moveInput.x * turnSpeed * Time.fixedDeltaTime;
            transform.Rotate(0f, turn, 0f, Space.World);
        }
        
        // Forward/backward movement
        Vector3 moveForce = transform.forward * _moveInput.y * moveSpeed;
        _rb.AddForce(moveForce, ForceMode.Acceleration);
    }
    
    /// <summary>
    /// Aligns the object's bottom (negative Y) to the surface normal below
    /// </summary>
    private void AlignToSurface()
    {
        // Cast a ray downward from the object's position
        Vector3 rayStart = transform.position;
        Vector3 rayDirection = -transform.up; // Object's local "down"
        
        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, raycastDistance, groundLayer))
        {
            // We hit a surface! Get its normal
            Vector3 surfaceNormal = hit.normal;
            
            // Calculate the target rotation that aligns our "up" with the surface normal
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, surfaceNormal) * transform.rotation;
            
            // Smoothly rotate towards the target
            Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(newRotation);
            
            // Apply suction force toward the surface
            ApplySuctionForce(hit, surfaceNormal);
            
            // Debug visualization
            if (showDebugRays)
            {
                Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.green);
                Debug.DrawRay(hit.point, surfaceNormal * 2f, Color.blue);
            }
        }
        else
        {
            // No surface detected - apply world down bias
            ApplyWorldDownBias();
            
            // No surface detected
            if (showDebugRays)
            {
                Debug.DrawRay(rayStart, rayDirection * raycastDistance, Color.red);
            }
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
        float normalVelocity = Vector3.Dot(_rb.linearVelocity, surfaceNormal);
        Vector3 dampingForce = -surfaceNormal * (normalVelocity * suctionDamping);
        
        // Combine forces
        _rb.AddForce(force + dampingForce, ForceMode.Acceleration);
        
        // Debug: Show force strength
        if (showDebugRays && distanceMultiplier > 0.01f)
        {
            Debug.DrawRay(transform.position, towardSurface * distanceMultiplier * 2f, Color.magenta);
        }
    }
    
    // Optional: Visualize in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Show raycast direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, -transform.up * raycastDistance);
    }
}
