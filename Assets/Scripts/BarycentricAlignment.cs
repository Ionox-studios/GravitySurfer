using UnityEngine;

public class BarycentricAlignment : MonoBehaviour
{
    [Header("Surface Detection")]
    [SerializeField] private float detectionDistance = 5f;
    [SerializeField] private LayerMask surfaceLayer;
    [SerializeField] private int rayCount = 5; // Number of rays to cast for better detection
    
    [Header("Alignment Settings")]
    [SerializeField] private float alignmentSpeed = 5f;
    [SerializeField] private float alignmentThreshold = 5f; // Distance to start aligning (should match or be close to detectionDistance)
    [SerializeField] private bool enableAlignment = true;
    
    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.3f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private bool showDebugInfo = false;
    
    private Rigidbody _rb;
    private Vector3 _targetUp = Vector3.up;
    private Vector3 _smoothedUp = Vector3.up;
    private Vector3 _velocityRef = Vector3.zero;
    private bool _isNearSurface = false;
    public Vector3 CurrentUp => _smoothedUp;     // same as target up when settled
public Vector3 CurrentNormal => _smoothedUp; // alias for clarity
    public bool IsAlignmentEnabled => enableAlignment && _isNearSurface;
    
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _smoothedUp = transform.up;
    }
    
    void FixedUpdate()
    {
        if (!enableAlignment)
        {
            _isNearSurface = false;
            return;
        }
        
        DetectAndAlignToSurface();
    }
    
private void DetectAndAlignToSurface()
{
    Vector3 bestNormal = Vector3.up;
    float   bestWallScore = -1f;          // higher = better wall
    float   bestGroundDist = float.MaxValue;
    Vector3 bestGroundNormal = Vector3.up;
    bool    hitAnything = false;
    float   bestDistance = float.MaxValue;

    foreach (var direction in GetRayDirections())
    {
        if (Physics.Raycast(transform.position, direction, out var hit, detectionDistance, surfaceLayer))
        {
            hitAnything = true;

            float upDot = Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)); // 0 = vertical wall, 1 = floor/ceiling

            // WALL-ish: prefer vertical and near
            if (upDot < 0.5f) // tweak threshold (0.4–0.7)
            {
                // score: more vertical + closer is better
                float verticalBonus = 0.5f - upDot;
                float proximity     = 1f - (hit.distance / detectionDistance);
                float score = verticalBonus + proximity;

                if (score > bestWallScore)
                {
                    bestWallScore = score;
                    bestNormal    = hit.normal;
                    bestDistance  = hit.distance;
                }
            }
            else
            {
                // GROUND-ish: track nearest ground as fallback
                if (hit.distance < bestGroundDist)
                {
                    bestGroundDist   = hit.distance;
                    bestGroundNormal = hit.normal;
                }
            }
        }
    }

    if (bestWallScore < 0f) // no wall found → use ground
    {
        bestNormal   = bestGroundNormal;
        bestDistance = bestGroundDist;
    }

    _isNearSurface = hitAnything && bestDistance <= alignmentThreshold;

    _targetUp = _isNearSurface ? bestNormal : Vector3.up;

    // smooth + normalize (important: SmoothDamp shrinks vectors)
    _smoothedUp = Vector3.SmoothDamp(_smoothedUp, _targetUp, ref _velocityRef, smoothTime);
    if (_smoothedUp.sqrMagnitude < 1e-6f) _smoothedUp = Vector3.up;
    else _smoothedUp.Normalize();

    AlignToDirection(_smoothedUp);
}

    
    private Vector3[] GetRayDirections()
    {
        // Create an array of directions to check
        // Down, forward-down, forward, and diagonal directions
        Vector3[] directions = new Vector3[rayCount];
        
        directions[0] = -transform.up; // Straight down
        
        if (rayCount > 1)
        {
            // Forward and angled down
            directions[1] = (transform.forward - transform.up).normalized;
        }
        
        if (rayCount > 2)
        {
            // Forward
            directions[2] = transform.forward;
        }
        
        if (rayCount > 3)
        {
            // Left-forward angled down
            directions[3] = (transform.forward - transform.right - transform.up).normalized;
        }
        
        if (rayCount > 4)
        {
            // Right-forward angled down
            directions[4] = (transform.forward + transform.right - transform.up).normalized;
        }
        
        // Add more rays if needed
        for (int i = 5; i < rayCount; i++)
        {
            // Additional rays in a cone around forward direction
            float angle = (i - 5) * (360f / (rayCount - 5));
            Vector3 dir = Quaternion.AngleAxis(angle, transform.forward) * (transform.forward - transform.up);
            directions[i] = dir.normalized;
        }
        
        return directions;
    }
    
    private void AlignToDirection(Vector3 targetUp)
    {
        // Project the current forward direction onto the plane defined by targetUp
        // This keeps us moving forward along the surface
        Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, targetUp).normalized;
        
        if (projectedForward.sqrMagnitude < 0.01f)
        {
            // If forward is parallel to targetUp, use right vector instead
            projectedForward = Vector3.ProjectOnPlane(transform.right, targetUp).normalized;
        }
        
        // Create rotation where:
        // - Forward stays tangent to the surface (projected forward)
        // - Up points away from surface (surface normal)
        Quaternion targetRotation = Quaternion.LookRotation(projectedForward, targetUp);
        
        // Smoothly rotate towards target
        Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, alignmentSpeed * Time.fixedDeltaTime);
        
        // Apply rotation to rigidbody
        _rb.MoveRotation(newRotation);
    }
    
    // Public method to manually enable/disable alignment
    public void SetAlignmentEnabled(bool enabled)
    {
        enableAlignment = enabled;
    }
    
    // Get the current target up direction (useful for other scripts)
    public Vector3 GetTargetUp()
    {
        return _targetUp;
    }
    
    // Check if currently aligned to a surface
    public bool IsAlignedToSurface()
    {
        return _isNearSurface;
    }
    
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        // Draw the target up direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, _targetUp * 2f);
        
        // Draw the smoothed up direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, _smoothedUp * 2.5f);
        
        // Draw detection sphere
        Gizmos.color = _isNearSurface ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alignmentThreshold);
    }
}
