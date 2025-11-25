using UnityEngine;

[RequireComponent(typeof(VehicleController))]
public class EnemyBehavior : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointRadius = 5f;   // how close before switching
    [SerializeField] private float lookAheadDistance = 10f; // how far ahead along path to aim (optional)

    [Header("Driving")]
    [SerializeField] private float maxSteerAngle = 45f;   // degrees; used to normalize steering
    [SerializeField] private float baseThrottle = 1.0f;   // default forward input
    [SerializeField] private float cornerThrottleFactor = 0.5f; // reduce throttle on sharp turns
    [SerializeField] private float steeringLerp = 5f;     // smooth steering
    [SerializeField] private float throttleLerp = 5f;     // smooth throttle

    [Header("Stuck Detection")]
    [SerializeField] private float stuckTime = 3f;        // how long to wait before unstuck routine
    [SerializeField] private float stuckThreshold = 0.5f; // minimum distance moved to not be stuck
    [SerializeField] private float backupTime = 0.5f;     // how long to back up
    [SerializeField] private float forwardTime = 1f;      // how long to push forward after turning

    [Header("Player Capture")]
    [SerializeField] private Transform player;            // reference to player transform
    [SerializeField] private float captureRadius = 15f;   // distance to trigger capture
    [SerializeField] private float captureDistance = 3f;  // how far to the side of the player
    [SerializeField] private float captureForwardOffset = 2f; // how far in front of the player to position
    [SerializeField] private float captureDuration = 5f;  // how long to stay captured
    [SerializeField] private float captureRange = 5f;     // range within which to stay next to player
    [SerializeField] private float captureLerpTime = 2f;  // time in seconds to lerp to capture position
    [SerializeField] private float captureStartDelay = 10f; // delay before first capture can happen
    [SerializeField] private float captureCooldown = 30f;   // cooldown after capture ends before next capture
    [SerializeField] private float releaseDistance = 15f;  // how far to move away after hitting player
    [SerializeField] private float releaseWaitTime = 2f;   // how long to wait after taking damage before re-approaching

    [Header("Attack")]
    [SerializeField] private GameObject attackObject;     // object that swings to hit player
    [SerializeField] private float attackInterval = 1f;   // time between attacks
    [SerializeField] private float attackSwingTime = 0.5f; // how long the swing takes
    [SerializeField] private float attackDamage = 10f;    // damage dealt to player
    [SerializeField] private float attackPushForce = 10f; // push force applied to player

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Respawn")]
    [SerializeField] private float respawnYThreshold = 5f; // If below this Y, respawn
    [SerializeField] private float respawnDamage = 20f;
    [SerializeField] private int maxStuckCount = 3; // How many times stuck before respawn
    [SerializeField] private float stuckCountResetTime = 15f; // Time window to count stuck events

    private VehicleController _vehicle;
    private int _currentIndex;
    private int _currentLap;
    private float _currentSteer;
    private float _currentThrottle;

    // Stuck detection
    private Vector3 _stuckCheckPosition;
    private float _stuckTimer;
    private bool _isUnsticking;
    private float _unstuckTimer;
    private int _unstuckPhase; // 0 = backup, 1 = turn, 2 = forward
    private float _randomTurnDirection;

    // Capture behavior
    private bool _isCapturing;
    private float _captureTimer;
    private bool _captureOnRight; // true = right side, false = left side
    private Vector3 _captureOffset; // the locked offset from player
    private float _gameTimer; // tracks time since game start
    private float _captureCooldownTimer; // tracks cooldown between captures
    private bool _hasReachedCapturePosition; // tracks if enemy has lerped to exact position
    private float _captureLerpTimer; // tracks lerp progress
    private Vector3 _captureStartOffset; // initial offset when capture starts
    private bool _isReleasing; // true when enemy is moving away after hit
    private Vector3 _releaseTargetPosition; // where to move to when releasing
    private float _releaseTimer; // tracks time spent in release state
    private bool _isWaitingAfterDamage; // true when waiting after taking damage (no target position)
    private Vector3 _capturePlayerLockPosition; // player's local position when enemy locked on
    private float _capturePlayerLockLateral; // player's lateral position when locked

    // Attack behavior
    private float _attackTimer;
    private bool _isSwinging;
    private float _swingTimer;
    private Quaternion _attackStartRotation;
    private Quaternion _attackEndRotation;

    // Respawn tracking
    private Vector3 _lastNodePosition; // Position of last waypoint passed
    private int _stuckCount; // Counts how many times stuck in time window
    private float _stuckCountTimer; // Timer for stuck count window
    private Rigidbody _rb;

    void Awake()
    {
        _vehicle = GetComponent<VehicleController>();
        _rb = GetComponent<Rigidbody>();
        
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("EnemyRacerAI: No waypoints assigned!", this);
            enabled = false;
        }
        
        _stuckCheckPosition = transform.position;
        
        // Initialize respawn tracking
        if (waypoints != null && waypoints.Length > 0)
        {
            _lastNodePosition = waypoints[0].position;
        }
        _stuckCount = 0;
        _stuckCountTimer = 0f;

        // Initialize health
        currentHealth = maxHealth;

        // Initialize attack object rotation
        if (attackObject != null)
        {
            _attackStartRotation = attackObject.transform.localRotation;
            _attackEndRotation = attackObject.transform.localRotation * Quaternion.Euler(0f, 0f, 180f); // Swing 180 degrees
            
            // Set damage and push force on attack collider if it exists
            EnemyAttackCollider attackCollider = attackObject.GetComponent<EnemyAttackCollider>();
            if (attackCollider != null)
            {
                attackCollider.SetDamage(attackDamage);
                attackCollider.SetPushForce(attackPushForce);
                attackCollider.SetOnHitCallback(OnPlayerHit);
            }
        }
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // Check for respawn conditions
        CheckRespawnConditions();

        // Track game time
        _gameTimer += Time.fixedDeltaTime;
        
        // Update cooldown timer
        if (_captureCooldownTimer > 0f)
        {
            _captureCooldownTimer -= Time.fixedDeltaTime;
        }

        // Check for player capture
        if (player != null && !_isCapturing && !_isUnsticking)
        {
            // Only allow capture after initial delay and if cooldown has expired
            if (_gameTimer >= captureStartDelay && _captureCooldownTimer <= 0f)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                
                if (distanceToPlayer <= captureRadius)
                {
                    // Start capture!
                    _isCapturing = true;
                    _captureTimer = 0f;
                    _captureLerpTimer = 0f;
                    _hasReachedCapturePosition = false;
                    
                    // Determine which side of the player we're on
                    Vector3 toEnemy = transform.position - player.position;
                    Vector3 playerRight = player.right;
                    float dot = Vector3.Dot(toEnemy, playerRight);
                    _captureOnRight = dot > 0; // positive = right, negative = left
                    
                    // Store initial offset
                    _captureStartOffset = transform.position - player.position;
                    _captureOffset = _captureStartOffset;
                    
                    // Lock the player's current position
                    _capturePlayerLockPosition = player.position;
                    _capturePlayerLockLateral = 0f;
                    
                    Debug.Log($"Enemy capturing player! Side: {(_captureOnRight ? "Right" : "Left")}");
                }
            }
        }

        // Execute capture behavior if active
        if (_isCapturing)
        {
            ExecuteCaptureBehavior();
            UpdateAttack();
            return;
        }

        // Check if we're stuck (compare to position from 3 seconds ago, not last frame)
        if (!_isUnsticking)
        {
            _stuckTimer += Time.fixedDeltaTime;
            
            if (_stuckTimer >= stuckTime)
            {
                float distanceMoved = Vector3.Distance(transform.position, _stuckCheckPosition);
                
                if (distanceMoved < stuckThreshold)
                {
                    // We're stuck! Increment stuck counter
                    _stuckCount++;
                    Debug.Log($"Enemy stuck! Count: {_stuckCount}/{maxStuckCount}");
                    
                    // Check if we've been stuck too many times
                    if (_stuckCount >= maxStuckCount)
                    {
                        RespawnEnemy();
                        _stuckCount = 0;
                        _stuckCountTimer = 0f;
                        return;
                    }
                    
                    // Start unstuck routine
                    _isUnsticking = true;
                    _unstuckPhase = 0;
                    _unstuckTimer = 0f;
                    _randomTurnDirection = Random.Range(0, 2) == 0 ? -1f : 1f; // Random left or right
                }
                
                // Reset check timer and position
                _stuckTimer = 0f;
                _stuckCheckPosition = transform.position;
            }
        }

        // Execute unstuck routine if active
        if (_isUnsticking)
        {
            ExecuteUnstuckRoutine();
            return;
        }

        // Normal pathfinding behavior
        ExecuteNormalBehavior();
    }

    private void ExecuteUnstuckRoutine()
    {
        _unstuckTimer += Time.fixedDeltaTime;

        switch (_unstuckPhase)
        {
            case 0: // Backup phase
                _vehicle.Move(new Vector2(0f, -1f)); // Back up
                if (_unstuckTimer >= backupTime)
                {
                    _unstuckPhase = 1;
                    _unstuckTimer = 0f;
                }
                break;

            case 1: // Turn phase (instant)
                _unstuckPhase = 2;
                _unstuckTimer = 0f;
                break;

            case 2: // Forward push phase
                _vehicle.Move(new Vector2(_randomTurnDirection, 1f)); // Forward with turn
                if (_unstuckTimer >= forwardTime)
                {
                    // End unstuck routine
                    _isUnsticking = false;
                    _stuckTimer = 0f;
                    _stuckCountTimer = 0f; // Reset timer after successful unstuck
                    Debug.Log("Enemy unstuck routine complete.");
                }
                break;
        }
    }

    private void ExecuteNormalBehavior()
    {
        // 1. Get current target waypoint
        Transform target = waypoints[_currentIndex];

        // If we're close enough (XZ plane only), advance to next
        Vector3 toTarget = target.position - transform.position;
        float distanceXZ = Mathf.Sqrt(toTarget.x * toTarget.x + toTarget.z * toTarget.z);

        if (distanceXZ < waypointRadius)
        {
            // Save this node position before advancing
            _lastNodePosition = waypoints[_currentIndex].position;
            
            _currentIndex++;
            
            // Loop waypoints and increment lap
            if (_currentIndex >= waypoints.Length)
            {
                _currentIndex = 0;
                _currentLap++;
                Debug.Log($"Enemy completed lap {_currentLap}");
            }
            
            target = waypoints[_currentIndex];
            toTarget = target.position - transform.position;
        }

        // project on local plane so we don't get weird vertical steering
        Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, transform.up);

        flatToTarget.Normalize();

        // 2. Compute desired steering based on angle to target direction
        float signedAngle = Vector3.SignedAngle(transform.forward, flatToTarget, transform.up);
        float desiredSteer = Mathf.Clamp(signedAngle / maxSteerAngle, -1f, 1f);

        // 3. Adjust throttle based on how hard we are turning
        float cornerSlowdown = Mathf.Lerp(1f, cornerThrottleFactor, Mathf.InverseLerp(0f, 1f, Mathf.Abs(desiredSteer)));
        float desiredThrottle = baseThrottle * cornerSlowdown;

        // 4. Smooth inputs so it doesn't jitter
        _currentSteer = Mathf.Lerp(_currentSteer, desiredSteer, steeringLerp * Time.fixedDeltaTime);
        _currentThrottle = Mathf.Lerp(_currentThrottle, desiredThrottle, throttleLerp * Time.fixedDeltaTime);

        // 5. Feed inputs into your vehicle controller
        _vehicle.Move(new Vector2(_currentSteer, _currentThrottle));
    }

    private void ExecuteCaptureBehavior()
    {
        if (player == null)
        {
            _isCapturing = false;
            return;
        }

        _captureTimer += Time.fixedDeltaTime;

        // Check if capture duration is complete
        if (_captureTimer >= captureDuration)
        {
            _isCapturing = false;
            _captureCooldownTimer = captureCooldown; // Start cooldown
            
            // Find closest node ahead and credit skipped nodes
            FindClosestForwardNode();
            
            Debug.Log("Capture duration complete. Starting 30s cooldown.");
            return;
        }

        // Handle release and re-approach
        if (_isReleasing)
        {
            _releaseTimer += Time.fixedDeltaTime;
            
            // If waiting after taking damage (no target position, just wait)
            if (_isWaitingAfterDamage)
            {
                if (_releaseTimer >= releaseWaitTime)
                {
                    // Wait complete, reset and prepare to re-approach
                    _isReleasing = false;
                    _isWaitingAfterDamage = false;
                    _hasReachedCapturePosition = false;
                    _captureLerpTimer = 0f;
                    _captureStartOffset = transform.position - player.position;
                    Debug.Log("Enemy finished waiting after damage and ready to re-approach!");
                }
                else
                {
                    // Just wait, don't move (vehicle inputs already set to zero)
                    _vehicle.Move(Vector2.zero);
                }
            }
            else
            {
                // Move away from player to release position (after landing a hit)
                Vector3 toTarget = _releaseTargetPosition - transform.position;
                float distance = toTarget.magnitude;
                
                if (distance < waypointRadius)
                {
                    // Reached release position, reset and prepare to re-approach
                    _isReleasing = false;
                    _hasReachedCapturePosition = false;
                    _captureLerpTimer = 0f;
                    _captureStartOffset = transform.position - player.position;
                    Debug.Log("Enemy released player and ready to re-approach!");
                }
                else
                {
                    // Navigate to release position using next node as forward reference
                    Transform releaseNextNode = waypoints[_currentIndex];
                    Vector3 toReleaseNode = releaseNextNode.position - transform.position;
                    Vector3 nodeForward = Vector3.ProjectOnPlane(toReleaseNode, transform.up).normalized;
                    
                    Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, transform.up).normalized;
                    float signedAngle = Vector3.SignedAngle(nodeForward, flatToTarget, transform.up);
                    float steer = Mathf.Clamp(signedAngle / maxSteerAngle, -1f, 1f);
                    
                    _vehicle.Move(new Vector2(steer, baseThrottle));
                }
            }
            return;
        }

        // Get the next waypoint to determine forward direction
        Transform nextNode = waypoints[_currentIndex];
        Vector3 toNextNode = nextNode.position - player.position;
        Vector3 pathForward = Vector3.ProjectOnPlane(toNextNode, transform.up).normalized;
        
        // Calculate right direction relative to the path
        Vector3 pathRight = Vector3.Cross(transform.up, pathForward).normalized;
        
        // Calculate the exact target position (to the side and slightly in front)
        Vector3 sideDirection = _captureOnRight ? pathRight : -pathRight;
        Vector3 exactTargetOffset = sideDirection * captureDistance + pathForward * captureForwardOffset;
        
        if (!_hasReachedCapturePosition)
        {
            // Smoothly lerp to the exact position over captureLerpTime seconds
            _captureLerpTimer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(_captureLerpTimer / captureLerpTime);
            _captureOffset = Vector3.Lerp(_captureStartOffset, exactTargetOffset, t);
            
            // Check if lerp is complete
            if (t >= 1f)
            {
                _captureOffset = exactTargetOffset;
                _hasReachedCapturePosition = true;
                // Lock the enemy's world position (laterally)
                _capturePlayerLockPosition = transform.position;
                Debug.Log("Enemy reached exact capture position!");
            }
        }
        else
        {
            // Calculate the lateral offset by projecting enemy's locked position onto path's right axis
            Vector3 playerToEnemyLock = _capturePlayerLockPosition - player.position;
            
            // Remove the forward component to get pure lateral offset (relative to path)
            Vector3 pathForwardComponent = pathForward * Vector3.Dot(playerToEnemyLock, pathForward);
            Vector3 lateralOffset = playerToEnemyLock - pathForwardComponent;
            
            // Get the lateral distance (relative to path right)
            float lateralDistance = lateralOffset.magnitude * Mathf.Sign(Vector3.Dot(lateralOffset, pathRight));
            
            // Only adjust if beyond captureRange
            if (Mathf.Abs(lateralDistance) > captureRange)
            {
                // Clamp to captureRange and update the locked position
                float clampedDistance = Mathf.Sign(lateralDistance) * captureRange;
                Vector3 newLateralOffset = pathRight * clampedDistance;
                _capturePlayerLockPosition = player.position + newLateralOffset + pathForwardComponent;
                lateralDistance = clampedDistance;
            }
            
            // Position enemy at the locked lateral position, but always match path's forward offset
            _captureOffset = pathRight * lateralDistance + pathForward * captureForwardOffset;
        }
        
        // Set position directly locked to player
        transform.position = player.position + _captureOffset;
        
        // Orient enemy to face along the path direction
        transform.rotation = Quaternion.LookRotation(pathForward, transform.up);

        // Set vehicle inputs to zero since we're overriding position
        _vehicle.Move(Vector2.zero);
    }

    private void UpdateAttack()
    {
        if (attackObject == null || player == null) return;

        // Only attack when capture is active and we've reached position
        if (!_isCapturing || !_hasReachedCapturePosition) return;

        // Update attack timer
        _attackTimer += Time.fixedDeltaTime;

        // Handle active swing
        if (_isSwinging)
        {
            _swingTimer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(_swingTimer / attackSwingTime);

            // Calculate direction to player
            Vector3 directionToPlayer = player.position - attackObject.transform.position;
            directionToPlayer.Normalize();
            
            // Create rotation that looks at player, then rotate 180 degrees around forward axis
            Quaternion lookAtPlayer = Quaternion.LookRotation(directionToPlayer);
            Quaternion swing = Quaternion.Euler(t * 180f, 0f, 0f); // Rotate 180 degrees
            attackObject.transform.rotation = lookAtPlayer * swing;

            // End swing and reset (but stay locked on player if no hit)
            if (t >= 1f)
            {
                _isSwinging = false;
                _attackTimer = 0f;
                attackObject.transform.localRotation = Quaternion.identity;
                // Note: We do NOT release here - only release when OnPlayerHit() is called
            }
        }
        // Start new swing if interval has passed
        else if (_attackTimer >= attackInterval)
        {
            _isSwinging = true;
            _swingTimer = 0f;
        }
    }

    /// <summary>
    /// Finds the closest waypoint ahead of the enemy after attack ends.
    /// Credits all skipped nodes and updates lap count if necessary.
    /// </summary>
    private void FindClosestForwardNode()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        float closestDistance = float.MaxValue;
        int closestIndex = _currentIndex;

        // Check all waypoints to find the closest one
        for (int i = 0; i < waypoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, waypoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        // Calculate how many nodes we're skipping
        int nodesSkipped = 0;
        int startIndex = _currentIndex;
        
        // Count nodes from current position to new position (going forward)
        if (closestIndex >= _currentIndex)
        {
            // Simple case: closest is ahead in same lap
            nodesSkipped = closestIndex - _currentIndex;
        }
        else
        {
            // Wrapped around: we've crossed the lap boundary
            nodesSkipped = (waypoints.Length - _currentIndex) + closestIndex;
            _currentLap++;
            Debug.Log($"Enemy crossed lap boundary during attack! Now on lap {_currentLap}");
        }

        // Update current index to the closest node
        _currentIndex = closestIndex;

        Debug.Log($"Enemy resumed at node {_currentIndex}, skipped {nodesSkipped} nodes");
    }

    /// <summary>
    /// Called when the enemy successfully hits the player
    /// </summary>
    private void OnPlayerHit()
    {
        if (!_isCapturing || player == null) return;

        // Calculate release position (move away from player)
        Vector3 awayDirection = (transform.position - player.position).normalized;
        _releaseTargetPosition = transform.position + awayDirection * releaseDistance;
        
        _isReleasing = true;
        Debug.Log("Enemy hit player! Releasing and will re-approach if time remains.");
    }

    /// <summary>
    /// Takes damage and reduces health. Call this from damage sources.
    /// </summary>
    /// <param name="damage">Amount of damage to take</param>
    /// <param name="pushDirection">Optional direction to push the enemy (will be applied as force)</param>
    public void TakeDamage(float damage, Vector3? pushDirection = null)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Enemy took {damage} damage. Current health: {currentHealth}/{maxHealth}");

        // Apply push force if provided
        if (pushDirection.HasValue)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(pushDirection.Value, ForceMode.Impulse);
                Debug.Log($"Applied push force to enemy: {pushDirection.Value}");
            }
        }

        // Release close lock and wait if capturing (pushed by damage force)
        if (_isCapturing && player != null)
        {
            _isReleasing = true;
            _isWaitingAfterDamage = true;
            _releaseTimer = 0f;
            Debug.Log("Enemy took damage while capturing! Waiting 2 seconds before re-approaching.");
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Heals the enemy by the specified amount
    /// </summary>
    /// <param name="amount">Amount to heal</param>
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Enemy healed {amount}. Current health: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Checks if enemy should respawn (fell off or stuck)
    /// </summary>
    private void CheckRespawnConditions()
    {
        // Check if fell below Y threshold
        if (transform.position.y < respawnYThreshold)
        {
            RespawnEnemy();
            return;
        }

        // Update stuck count timer and reset if time window expired
        if (_stuckCount > 0)
        {
            _stuckCountTimer += Time.fixedDeltaTime;
            
            if (_stuckCountTimer >= stuckCountResetTime)
            {
                // Time window expired, reset stuck count
                _stuckCount = 0;
                _stuckCountTimer = 0f;
                Debug.Log("Stuck count reset (time window expired)");
            }
        }
    }

    /// <summary>
    /// Respawns the enemy at their last node position and deals damage
    /// </summary>
    private void RespawnEnemy()
    {
        Debug.Log($"Enemy respawning at last node position: {_lastNodePosition}");
        
        // Move to last node position
        transform.position = _lastNodePosition + Vector3.up * 2f; // Slightly above to avoid clipping
        transform.rotation = Quaternion.identity;
        
        // Reset velocity
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        
        // Take damage
        TakeDamage(respawnDamage);
        
        // Reset stuck counters
        _stuckCount = 0;
        _stuckCountTimer = 0f;
        
        Debug.Log($"Enemy respawned! Took {respawnDamage} damage. Health: {currentHealth}/{maxHealth}");
    }

    private void Die()
    {
        Debug.Log("Enemy died!");
        
        // Destroy the enemy game object
        Destroy(gameObject);
    }

    /// <summary>
    /// Returns the current health value
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Returns the maximum health value
    /// </summary>
    public float GetMaxHealth()
    {
        return maxHealth;
    }
}
