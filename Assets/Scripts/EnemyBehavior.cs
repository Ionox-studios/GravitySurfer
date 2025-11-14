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

    void Awake()
    {
        _vehicle = GetComponent<VehicleController>();
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("EnemyRacerAI: No waypoints assigned!", this);
            enabled = false;
        }
        _stuckCheckPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

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
}
