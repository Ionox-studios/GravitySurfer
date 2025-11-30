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
    
    [Header("Boost")]
    [SerializeField] private float boostDuration = 3f; // How long boost lasts in seconds
    [SerializeField] private float boostForceMultiplier = 2f; // Multiplier for movement force during boost
    [SerializeField] private float boostCooldown = 30f; // Cooldown time before boost can be used again
    
    [Header("Jump System")]
    [SerializeField] private float jumpCooldown = 1f; // Cooldown between jumps in seconds
    
    [Header("Attack System")]
    [SerializeField] private float attackCooldown = 1f; // Cooldown between attacks in seconds
    
    [Header("Raycasting (for hover & grounded detection)")]
    [SerializeField] private float raycastDistance = 10f;
    [SerializeField] private LayerMask groundLayer; // Set to Enemy layer in Inspector
    [SerializeField] private bool showDebugRays = true;
    
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
    [SerializeField] private LayerMask enemyLayer; // Layer mask for enemies - set to Enemy layer in Inspector
    [SerializeField] private bool showAttackDebug = true; // Show attack range visualization
    [SerializeField] private int maxEnemiesPerAttack = 20; // Max enemies that can be hit in one attack
    
    [Header("Ground Slam")]
    [SerializeField] private float groundRotationSpeed = 200f; // Speed of downward rotation when grounding
    [SerializeField] private float groundPullForce = 50f; // Downward force applied when grounding
    [SerializeField] private bool enableGroundMechanic = true; // Toggle ground slam on/off
    
    private Rigidbody _rb;
    private SurfaceAttraction _surfaceAttraction; // Reference to surface attraction component
    private Vector2 _moveInput; // Store input for FixedUpdate
    private bool _isGrounded; // Track if we detected a surface this frame
    private bool _attackPending = false; // Is an attack queued?
    private float _attackTimer = 0f; // Time until attack executes
    public Animator animator; // AL
    public Animator SlashEffect; //AL
    
    // Jump system variables
    private float _lastJumpTime = -999f; // Time of last jump
    
    // Attack system variables
    private float _lastAttackTime = -999f; // Time of last attack
    private Collider[] _attackResults; // Reusable buffer for attack overlap results
    
    [Header("Jump Effects")]
    [SerializeField] private ParticleSystem jumpParticleSystem; // Particle effect on jump
    [SerializeField] private AudioSource jumpAudioSource; // Sound effect on jump
    
    // Boost system variables
    private bool _isBoostActive = false; // Is boost currently active
    private float _boostTimeRemaining = 0f; // Time remaining on current boost
    private float _boostCooldownRemaining = 0f; // Cooldown time remaining before boost can be used again
    private bool _boostAvailable = true; // Is boost ready to use
    
    // Ground slam variables
    private bool _isGrounding = false; // Is ground slam currently active
    
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _surfaceAttraction = GetComponent<SurfaceAttraction>();
        
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
        // Update boost timer
        UpdateBoost();
        
        // Apply grounding mechanic if active
        if (_isGrounding && enableGroundMechanic)
        {
            ApplyGroundMechanic();
        }
        
        // Apply movement
        ApplyMovement();
        
        // Handle hover (if enabled) - requires surface detection
        if (enableHover)
        {
            DetectSurfaceForHover();
        }
        
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
            //Debug.Log($"Move input received: {input}");
        }
    }
    
    /// <summary>
    /// Jump - applies an impulse force in the object's up direction
    /// Infinite jumps with cooldown
    /// </summary>
    public void Jump()
    {
        if (_rb == null) return;
        
        // Check if cooldown is ready
        float timeSinceLastJump = Time.time - _lastJumpTime;
        bool cooldownReady = timeSinceLastJump >= jumpCooldown;
        
        if (!cooldownReady)
        {
            Debug.Log($"Jump on cooldown! Wait {jumpCooldown - timeSinceLastJump:F1}s");
            return;
        }
        
        // Execute jump
        _rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        _lastJumpTime = Time.time;
        
        if (animator != null) // AL
            animator.SetTrigger("Jump"); // AL
        
        // Play jump particle effect
        if (jumpParticleSystem != null)
        {
            jumpParticleSystem.Play();
            Debug.Log("Playing jump particles!");
        }
        else
        {
            Debug.LogWarning("Jump particle system not assigned!");
        }
        
        // Play jump sound effect
        if (jumpAudioSource != null)
            jumpAudioSource.Play();
        
        Debug.Log("Jump!");
    }


    
    public void Attack() //AL
    {
        // Check cooldown
        if (Time.time - _lastAttackTime < attackCooldown)
        {
            Debug.Log($"Attack on cooldown! {(attackCooldown - (Time.time - _lastAttackTime)):F1}s remaining");
            return;
        }
        
        // Execute attack immediately
        ExecuteAttack();
        
        Debug.Log("Attack executed immediately!");
    }
    
    /// <summary>
    /// Activate boost mode - doubles force and disables max speed for duration
    /// </summary>
    public void ActivateBoost()
    {
        // Check if boost is available (not on cooldown)
        if (!_boostAvailable)
        {
            Debug.Log($"Boost on cooldown! {_boostCooldownRemaining:F1}s remaining");
            return;
        }
        
        if (_isBoostActive)
        {
            // Already boosting - ignore
            Debug.Log("Boost already active!");
        }
        else
        {
            // Start new boost
            _isBoostActive = true;
            _boostTimeRemaining = boostDuration;
            _boostAvailable = false;
            _boostCooldownRemaining = boostCooldown;
            Debug.Log($"Boost activated! Duration: {boostDuration}s, Force multiplier: {boostForceMultiplier}x");
        }
    }
    
    /// <summary>
    /// Start ground slam - rapidly rotate down and pull vehicle to ground
    /// Call this when crouch button is pressed
    /// </summary>
    public void StartGround()
    {
        if (!enableGroundMechanic) return;
        _isGrounding = true;
    }
    
    /// <summary>
    /// Stop ground slam
    /// Call this when crouch button is released
    /// </summary>
    public void StopGround()
    {
        _isGrounding = false;
    }
    
    /// <summary>
    /// Apply downward rotation and force while grounding
    /// </summary>
    private void ApplyGroundMechanic()
    {
        if (_rb == null) return;
        
        // Apply strong downward force (world down)
        _rb.AddForce(Vector3.down * groundPullForce, ForceMode.Acceleration);
        
        // Rotate so the vehicle's up points toward world up (standing upright)
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, groundRotationSpeed * Time.fixedDeltaTime);
    }
    
    /// <summary>
    /// Update boost timer each fixed update
    /// </summary>
    private void UpdateBoost()
    {
        // Update active boost timer
        if (_isBoostActive)
        {
            _boostTimeRemaining -= Time.fixedDeltaTime;
            
            if (_boostTimeRemaining <= 0f)
            {
                _isBoostActive = false;
                _boostTimeRemaining = 0f;
                Debug.Log("Boost ended! Cooldown started.");
            }
        }
        
        // Update cooldown timer
        if (!_boostAvailable)
        {
            _boostCooldownRemaining -= Time.fixedDeltaTime;
            
            if (_boostCooldownRemaining <= 0f)
            {
                _boostAvailable = true;
                _boostCooldownRemaining = 0f;
                Debug.Log("Boost recharged and ready!");
            }
        }
    }
    
    /// <summary>
    /// Execute the attack - sphere cast for enemies and damage them
    /// </summary>
    private void ExecuteAttack()
    {
        // Update last attack time for cooldown
        _lastAttackTime = Time.time;
        
        // Trigger animation and slash effect
        if (animator != null)
            animator.SetTrigger("isAttack1");
        
        if (SlashEffect != null)
            SlashEffect.SetTrigger("Kick");
        
        // Perform sphere overlap from player position (NonAlloc version to avoid GC)
        // Initialize buffer if needed
        if (_attackResults == null || _attackResults.Length != maxEnemiesPerAttack)
            _attackResults = new Collider[maxEnemiesPerAttack];
        
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, attackRange, _attackResults, enemyLayer);
        
        int enemiesHit = 0;
        
        for (int i = 0; i < numHits; i++)
        {
            Collider col = _attackResults[i];
            
            // Skip self
            if (col.gameObject == gameObject) continue;
            
            // Try to get EnemyBehavior component
            EnemyBehavior enemy = col.GetComponent<EnemyBehavior>();
            
            if (enemy != null)
            {
                enemiesHit++;
                
                // Calculate direction from player to enemy (horizontal component only)
                Vector3 toEnemy = col.transform.position - transform.position;
                Vector3 horizontalDirection = new Vector3(toEnemy.x, 0f, toEnemy.z).normalized;
                Vector3 pushDirection = horizontalDirection * attackForce;
                
                // Apply damage with push force
                enemy.TakeDamage(attackDamage, pushDirection);
                
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
            
            // Check if on wave road - if so, disable max speed limit
            bool onWaveRoad = _surfaceAttraction != null && _surfaceAttraction.IsOnWaveRoad();
            
            // Only apply acceleration if:
            // - Boost is active (no speed limit), OR
            // - On a wave road (no speed limit), OR
            // - Moving forward and below max speed, OR
            // - Moving backward (negative input), OR
            // - Trying to slow down (input opposes current velocity)
            bool canAccelerate = _isBoostActive || // No speed limit during boost
                                 onWaveRoad || // No speed limit on wave roads
                                 (_moveInput.y > 0 && forwardSpeed < maxSpeed) || // Forward and below max
                                 (_moveInput.y < 0 && forwardSpeed > -maxSpeed) || // Backward and below max (in reverse)
                                 (_moveInput.y * forwardSpeed < 0); // Input opposes velocity (braking)
            
            if (canAccelerate)
            {
                // Apply boost multiplier if active
                float currentMoveSpeed = _isBoostActive ? moveSpeed * boostForceMultiplier : moveSpeed;
                Vector3 moveForce = transform.forward * _moveInput.y * currentMoveSpeed;
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
    
    /// <summary>
    /// Check if boost is currently active
    /// </summary>
    public bool IsBoostActive()
    {
        return _isBoostActive;
    }
    
    /// <summary>
    /// Get remaining boost time
    /// </summary>
    public float GetBoostTimeRemaining()
    {
        return _boostTimeRemaining;
    }
    
    /// <summary>
    /// Check if boost is available (not on cooldown)
    /// </summary>
    public bool IsBoostAvailable()
    {
        return _boostAvailable;
    }
    
    /// <summary>
    /// Get remaining cooldown time
    /// </summary>
    public float GetBoostCooldownRemaining()
    {
        return _boostCooldownRemaining;
    }
    
    /// <summary>
    /// Get boost duration setting
    /// </summary>
    public float GetBoostDuration()
    {
        return boostDuration;
    }
    
    /// <summary>
    /// Get boost cooldown setting
    /// </summary>
    public float GetBoostCooldown()
    {
        return boostCooldown;
    }
    
    /// <summary>
    /// Get the jump cooldown duration
    /// </summary>
    public float GetJumpCooldown()
    {
        return jumpCooldown;
    }
    
    /// <summary>
    /// Get the time when the last jump occurred
    /// </summary>
    public float GetLastJumpTime()
    {
        return _lastJumpTime;
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
    
    // Show attack gizmo even when not selected (during attack)
    void OnDrawGizmos()
    {
        if (!showAttackDebug) return;
        
        // Show bright red sphere when attack is about to execute (last 0.1s of timer)
        if (_attackPending && _attackTimer <= 0.1f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Also draw a solid sphere to make it more visible
            Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, attackRange);
        }
        // Show yellow sphere during attack delay
        else if (_attackPending)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawSphere(transform.position, attackRange);
        }
    }
}
