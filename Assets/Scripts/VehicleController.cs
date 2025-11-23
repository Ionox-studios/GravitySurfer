using UnityEngine;

/// <summary>
/// Handles vehicle movement, turning, jumping, and optional hover functionality.
/// Surface alignment and attraction forces are handled by SurfaceAttraction script.
/// </summary>
public class VehicleController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float velocityDamping = 3f; // How quickly velocity decreases when no input
    [SerializeField] private float velocityAlignmentStrength = 5f; // How much velocity rotates with the object (arcade feel)
    
    [Header("Raycasting (for hover & grounded detection)")]
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask groundLayer = -1; // -1 means everything
    [SerializeField] private bool showDebugRays = true;
    
    [Header("Hover")]
    [SerializeField] private bool enableHover = false; // Toggle hover mode
    [SerializeField] private float hoverHeight = 2f; // Target height to maintain
    [SerializeField] private float hoverForce = 50f; // Spring force to maintain height
    [SerializeField] private float hoverDamping = 5f; // Dampening for hover oscillation
    
    private Rigidbody _rb;
    private Vector2 _moveInput; // Store input for FixedUpdate
    private bool _isGrounded; // Track if we detected a surface this frame
    public Animator animator; // AL
    public Animator SlashEffect; //AL
    
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
        
        Debug.Log("VehicleController initialized on " + gameObject.name);
    }
    
    void FixedUpdate()
    {
        // Apply movement
        ApplyMovement();
        
        // Handle hover (if enabled) - requires surface detection
        if (enableHover)
        {
            DetectSurfaceForHover();
        }
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
            //Debug.Log($"Move input received: {input}");
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
        if (animator != null) // AL
            animator.SetTrigger("Jump"); // AL
            SlashEffect.SetTrigger("Kick"); // AL
        
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
        
        // Arcade-style velocity alignment: gradually rotate velocity toward facing direction
        // Only apply when grounded to avoid interfering with falling/jumping
        if (_isGrounded && velocityAlignmentStrength > 0f && _rb.linearVelocity.magnitude > 0.1f)
        {
            // Get current speed
            float currentSpeed = _rb.linearVelocity.magnitude;
            
            // Calculate desired velocity direction (forward-facing)
            Vector3 desiredVelocity = transform.forward * currentSpeed;
            
            // Lerp current velocity toward desired velocity
            Vector3 newVelocity = Vector3.Lerp(_rb.linearVelocity, desiredVelocity, velocityAlignmentStrength * Time.fixedDeltaTime);
            _rb.linearVelocity = newVelocity;
        }
        
        // Forward/backward movement
        if (Mathf.Abs(_moveInput.y) > 0.01f)
        {
            // Check current speed in local forward direction
            Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity);
            float forwardSpeed = localVelocity.z;
            
            // Only apply acceleration if:
            // - Moving forward and below max speed, OR
            // - Moving backward (negative input), OR
            // - Trying to slow down (input opposes current velocity)
            bool canAccelerate = (_moveInput.y > 0 && forwardSpeed < maxSpeed) || // Forward and below max
                                 (_moveInput.y < 0 && forwardSpeed > -maxSpeed) || // Backward and below max (in reverse)
                                 (_moveInput.y * forwardSpeed < 0); // Input opposes velocity (braking)
            
            if (canAccelerate)
            {
                Vector3 moveForce = transform.forward * _moveInput.y * moveSpeed;
                _rb.AddForce(moveForce, ForceMode.Acceleration);
            }
        }
        
        // Apply velocity damping to horizontal movement only (don't interfere with gravity/falling)
        // This prevents the floaty feeling without limiting fall speed
        Vector3 currentVelocity = _rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        horizontalVelocity *= (1f - velocityDamping * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
    }
    
    /// <summary>
    /// Detect surface for hover functionality only
    /// </summary>
    private void DetectSurfaceForHover()
    {
        // Cast a ray downward from the object's position
        Vector3 rayStart = transform.position;
        Vector3 rayDirection = -transform.up; // Object's local "down"
        
        if (Physics.Raycast(rayStart, rayDirection, out RaycastHit hit, raycastDistance, groundLayer))
        {
            _isGrounded = true; // Surface detected
            Vector3 surfaceNormal = hit.normal;
            
            // Apply hover force
            ApplyHoverForce(hit, surfaceNormal);
            
            // Debug visualization
            if (showDebugRays)
            {
                Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.green);
                Debug.DrawRay(hit.point, surfaceNormal * 2f, Color.blue);
            }
        }
        else
        {
            _isGrounded = false; // No surface detected
            
            // No surface detected
            if (showDebugRays)
            {
                Debug.DrawRay(rayStart, rayDirection * raycastDistance, Color.red);
            }
        }
    }
    
    /// <summary>
    /// Applies hover force to maintain a constant height above the surface (like a spring)
    /// Uses exponential force when below target height to strongly push away from surface
    /// Never pulls down - only pushes up when below target
    /// </summary>
    private void ApplyHoverForce(RaycastHit hit, Vector3 surfaceNormal)
    {
        // Only apply hover force when below target height (too close to surface)
        if (hit.distance >= hoverHeight)
        {
            return; // Above target height - no hover force needed
        }
        
        // Below target height - calculate exponential push force
        float heightDifference = hoverHeight - hit.distance;
        float normalizedError = heightDifference / hoverHeight;
        float exponentialMultiplier = normalizedError * normalizedError; // Square for exponential growth
        float force = exponentialMultiplier * hoverForce;
        
        // Dampen velocity along the surface normal (prevents oscillation)
        float normalVel = Vector3.Dot(_rb.linearVelocity, surfaceNormal);
        force -= normalVel * hoverDamping;
        
        // Apply force along the surface normal
        _rb.AddForce(surfaceNormal * force, ForceMode.Acceleration);
        
        // Debug: Show hover force
        if (showDebugRays)
        {
            Debug.DrawRay(transform.position, surfaceNormal * (force / hoverForce), Color.cyan);
        }
    }
    
    /// <summary>
    /// Get the current velocity of the vehicle
    /// </summary>
    public Vector3 GetVelocity()
    {
        return _rb != null ? _rb.linearVelocity : Vector3.zero;
    }
    
    /// <summary>
    /// Get the current speed (magnitude of velocity)
    /// </summary>
    public float GetSpeed()
    {
        return _rb != null ? _rb.linearVelocity.magnitude : 0f;
    }
    
    /// <summary>
    /// Get the current speed as a percentage of max speed (0-1)
    /// </summary>
    public float GetNormalizedSpeed()
    {
        if (_rb == null || maxSpeed <= 0f) return 0f;
        return Mathf.Clamp01(_rb.linearVelocity.magnitude / maxSpeed);
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
