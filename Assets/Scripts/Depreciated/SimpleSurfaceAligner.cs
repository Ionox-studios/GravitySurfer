using UnityEngine;

/// <summary>
/// Minimal controller with basic movement and surface normal alignment.
/// Aligns the object's bottom to the surface normal as you approach a surface.
/// </summary>
public class SimpleSurfaceAligner : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float velocityDamping = 3f; // How quickly velocity decreases when no input
    
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
    
    [Header("Hover")]
    [SerializeField] private bool enableHover = false; // Toggle hover mode
    [SerializeField] private float hoverHeight = 2f; // Target height to maintain
    [SerializeField] private float hoverForce = 50f; // Spring force to maintain height
    [SerializeField] private float hoverDamping = 5f; // Dampening for hover oscillation
    
    [Header("Attack")]
    [SerializeField] private float attackRange = 5f; // Radius of sphere cast for attack
    [SerializeField] private float attackDelay = 0.5f; // Delay before attack executes
    [SerializeField] private float attackDamage = 25f; // Damage dealt to enemies
    [SerializeField] private float attackForce = 500f; // Force applied to hit enemies
    [SerializeField] private LayerMask enemyLayer = -1; // Layer mask for enemies
    [SerializeField] private bool showAttackDebug = true; // Show attack range visualization
    
    private Rigidbody _rb;
    private Vector2 _moveInput; // Store input for FixedUpdate
    private bool _attackPending = false; // Is an attack queued?
    private float _attackTimer = 0f; // Time until attack executes

    // Public properties for other scripts to access surface state
    public bool IsSurfaceDetected { get; private set; }
    public RaycastHit LastSurfaceHit { get; private set; }
    
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
        
        // Update attack timer if attack is pending
        if (_attackPending)
        {
            _attackTimer -= Time.fixedDeltaTime;
            if (_attackTimer <= 0f)
            {
                ExecuteAttack();
                _attackPending = false;
            }
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
    /// Attack - initiates attack with delay, then sphere casts for enemies
    /// </summary>
    public void Attack()
    {
        if (_attackPending) return; // Already have an attack pending
        
        // Queue the attack
        _attackPending = true;
        _attackTimer = attackDelay;
        
        Debug.Log($"Attack queued! Will execute in {attackDelay} seconds");
    }
    
    /// <summary>
    /// Execute the attack - sphere cast for enemies and damage them
    /// </summary>
    private void ExecuteAttack()
    {
        // Perform sphere cast from player position
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);
        
        int enemiesHit = 0;
        
        foreach (Collider col in hitColliders)
        {
            // Skip self
            if (col.gameObject == gameObject) continue;
            
            // Try to get EnemyBehavior component
            EnemyBehavior enemy = col.GetComponent<EnemyBehavior>();
            
            if (enemy != null)
            {
                enemiesHit++;
                
                // Calculate direction from player to enemy for push force
                Vector3 pushDirection = (col.transform.position - transform.position).normalized;
                
                // Apply damage with push force
                enemy.TakeDamage(attackDamage, pushDirection * attackForce);
                
                Debug.Log($"Attack hit enemy: {col.gameObject.name} for {attackDamage} damage!");
            }
        }
        
        if (showAttackDebug)
        {
            Debug.Log($"Attack executed! Hit {enemiesHit} enemies in range {attackRange}");
        }
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
        
        // Apply velocity damping to slow down when no input
        // This prevents the floaty feeling
        _rb.linearVelocity *= (1f - velocityDamping * Time.fixedDeltaTime);
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
            // Update public state
            IsSurfaceDetected = true;
            LastSurfaceHit = hit;

            // We hit a surface! Get its normal
            Vector3 surfaceNormal = hit.normal;
            
            // Calculate the target rotation that aligns our "up" with the surface normal
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, surfaceNormal) * transform.rotation;
            
            // Smoothly rotate towards the target
            Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(newRotation);
            
            // Apply suction force (always active)
            ApplySuctionForce(hit, surfaceNormal);
            
            // Apply hover force (if enabled, sums with suction)
            if (enableHover)
            {
                ApplyHoverForce(hit, surfaceNormal);
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
            // Update public state
            IsSurfaceDetected = false;

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
    
    // Optional: Visualize in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // Show raycast direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, -transform.up * raycastDistance);
        
        // Show attack range
        if (showAttackDebug)
        {
            Gizmos.color = _attackPending ? Color.red : new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
