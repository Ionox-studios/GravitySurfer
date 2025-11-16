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

    [Header("Attack")]
    [SerializeField] private GameObject attackObject;     // object that swings to hit player
    [SerializeField] private float attackInterval = 1f;   // time between attacks
    [SerializeField] private float attackSwingTime = 0.5f; // how long the swing takes
    [SerializeField] private float attackDamage = 10f;    // damage dealt to player

    private VehicleController _vehicle;
    private int _currentIndex;
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

    // Attack behavior
    private float _attackTimer;
    private bool _isSwinging;
    private float _swingTimer;
    private Quaternion _attackStartRotation;
    private Quaternion _attackEndRotation;

    void Awake()
    {
        _vehicle = GetComponent<VehicleController>();
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("EnemyRacerAI: No waypoints assigned!", this);
            enabled = false;
        }
        _stuckCheckPosition = transform.position;

        // Initialize attack object rotation
        if (attackObject != null)
        {
            _attackStartRotation = attackObject.transform.localRotation;
            _attackEndRotation = attackObject.transform.localRotation * Quaternion.Euler(0f, 0f, 180f); // Swing 180 degrees
            
            // Set damage on attack collider if it exists
            EnemyAttackCollider attackCollider = attackObject.GetComponent<EnemyAttackCollider>();
            if (attackCollider != null)
            {
                attackCollider.SetDamage(attackDamage);
            }
        }
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

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
                    // We're stuck! Start unstuck routine
                    _isUnsticking = true;
                    _unstuckPhase = 0;
                    _unstuckTimer = 0f;
                    _randomTurnDirection = Random.Range(0, 2) == 0 ? -1f : 1f; // Random left or right
                    Debug.Log("Enemy stuck! Starting unstuck routine.");
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
            _currentIndex = (_currentIndex + 1) % waypoints.Length;
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
            Debug.Log("Capture duration complete. Starting 30s cooldown.");
            return;
        }

        // Calculate the exact target position (to the side and slightly in front)
        Vector3 sideDirection = _captureOnRight ? player.right : -player.right;
        Vector3 exactTargetOffset = sideDirection * captureDistance + player.forward * captureForwardOffset;
        
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
                Debug.Log("Enemy reached exact capture position!");
            }
        }
        else
        {
            // Now locked at exact position, allow player to move left/right within range
            Vector3 playerRight = player.right;
            float lateralOffset = Vector3.Dot(_captureOffset, playerRight);
            
            // Clamp the lateral offset to captureRange
            lateralOffset = Mathf.Clamp(lateralOffset, -captureRange, captureRange);
            
            // Get forward and up components (locked to player)
            Vector3 playerForward = player.forward;
            Vector3 playerUp = player.up;
            
            float forwardOffset = Vector3.Dot(_captureOffset, playerForward);
            float upOffset = Vector3.Dot(_captureOffset, playerUp);
            
            // Reconstruct the clamped offset
            _captureOffset = playerRight * lateralOffset + playerForward * forwardOffset + playerUp * upOffset;
        }
        
        // Set position directly locked to player
        transform.position = player.position + _captureOffset;
        
        // Match player's rotation
        transform.rotation = player.rotation;

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

            // End swing and reset
            if (t >= 1f)
            {
                _isSwinging = false;
                _attackTimer = 0f;
                attackObject.transform.localRotation = Quaternion.identity;
            }
        }
        // Start new swing if interval has passed
        else if (_attackTimer >= attackInterval)
        {
            _isSwinging = true;
            _swingTimer = 0f;
        }
    }
}
