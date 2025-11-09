using UnityEngine;

/// <summary>
/// Advanced buoyancy system using 4 raycast points positioned around the player.
/// Creates realistic wave riding physics - riding a wave pushes you forward,
/// while going against waves creates resistance.
/// </summary>
public class WaveBuoyancy : MonoBehaviour
{
    [Header("Buoyancy Points (Clockwise from Back-Right)")]
    [SerializeField] private Transform backRightPoint;
    [SerializeField] private Transform backLeftPoint;
    [SerializeField] private Transform frontLeftPoint;
    [SerializeField] private Transform frontRightPoint;
    
    [Header("Buoyancy Settings")]
    [SerializeField] private float buoyancyForce = 100f; // Spring force per unit of depth
    [SerializeField] private float buoyancyDamping = 10f; // Dampening to prevent oscillation
    [SerializeField] private float targetHeightAboveWave = 0.5f; // Target height above wave surface
    [SerializeField] private float maxBuoyancyForcePerPoint = 500f; // Maximum force per point
    [SerializeField] private float raycastDistance = 20f; // How far to check for waves above/below
    [SerializeField] private LayerMask waveLayer = -1; // Layer(s) to detect as waves
    [SerializeField] private bool showDebugRays = true;
    
    [Header("References")]
    [SerializeField] private SimpleSurfaceAligner surfaceAligner; // Reference to existing hover system
    
    private Rigidbody _rb;
    private bool _isInWave = false; // Whether any point is currently in a wave
    
    // Store info about each buoyancy point
    private struct BuoyancyPointData
    {
        public Transform transform;
        public float depth; // How far below the wave surface (0 = at surface, positive = below)
        public Vector3 waveNormal;
        public bool isSubmerged;
    }
    
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        
        if (_rb == null)
        {
            Debug.LogError("WaveBuoyancy requires a Rigidbody component!");
            return;
        }
        
        // Validate buoyancy points
        if (backRightPoint == null || backLeftPoint == null || frontLeftPoint == null || frontRightPoint == null)
        {
            Debug.LogError("WaveBuoyancy requires all 4 buoyancy points to be assigned!");
            return;
        }
        
        // Auto-find surface aligner if not assigned
        if (surfaceAligner == null)
        {
            surfaceAligner = GetComponent<SimpleSurfaceAligner>();
        }
        
        Debug.Log("WaveBuoyancy initialized with 4-point system on " + gameObject.name);
    }
    
    void FixedUpdate()
    {
        if (_rb == null) return;
        
        ProcessMultiPointBuoyancy();
    }
    
    /// <summary>
    /// Process buoyancy at all 4 points and apply forces
    /// </summary>
    private void ProcessMultiPointBuoyancy()
    {
        // Collect data for all 4 points
        BuoyancyPointData[] points = new BuoyancyPointData[4];
        points[0] = CalculatePointBuoyancy(backRightPoint);
        points[1] = CalculatePointBuoyancy(backLeftPoint);
        points[2] = CalculatePointBuoyancy(frontLeftPoint);
        points[3] = CalculatePointBuoyancy(frontRightPoint);
        
        // Check if any point is submerged
        _isInWave = false;
        foreach (var point in points)
        {
            if (point.isSubmerged)
            {
                _isInWave = true;
                break;
            }
        }
        
        // Apply forces at each submerged point
        foreach (var point in points)
        {
            if (point.isSubmerged)
            {
                ApplyBuoyancyForceAtPoint(point);
            }
        }
    }
    
    /// <summary>
    /// Calculate buoyancy data for a single point
    /// </summary>
    private BuoyancyPointData CalculatePointBuoyancy(Transform point)
    {
        BuoyancyPointData data = new BuoyancyPointData();
        data.transform = point;
        data.depth = 0f;
        data.isSubmerged = false;
        
        if (point == null) return data;
        
        // Raycast origin is below the point by the target height
        Vector3 raycastOrigin = point.position - new Vector3(0, targetHeightAboveWave, 0);
        
        // Cast upward to find waves above this point
        RaycastHit[] hits = Physics.RaycastAll(raycastOrigin, Vector3.up, raycastDistance, waveLayer);
        
        // Find the highest wave surface
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
        
        if (highestWaveHit.HasValue)
        {
            RaycastHit hit = highestWaveHit.Value;
            
            // Calculate depth: how far the point is below the wave surface
            // Positive depth = point is below the wave (needs upward force)
            data.depth = hit.point.y - point.position.y;
            data.waveNormal = hit.normal;
            data.isSubmerged = data.depth > 0f;
            
            // Debug visualization
            if (showDebugRays)
            {
                Color rayColor = data.isSubmerged ? Color.cyan : Color.green;
                Debug.DrawRay(raycastOrigin, Vector3.up * hit.distance, rayColor);
                Debug.DrawRay(hit.point, hit.normal * 1f, Color.blue);
            }
        }
        else
        {
            // No wave detected above this point
            if (showDebugRays)
            {
                Debug.DrawRay(raycastOrigin, Vector3.up * raycastDistance, Color.red);
            }
        }
        
        return data;
    }
    
    /// <summary>
    /// Apply buoyancy force at a specific point based on its depth
    /// </summary>
    private void ApplyBuoyancyForceAtPoint(BuoyancyPointData pointData)
    {
        if (!pointData.isSubmerged || pointData.depth <= 0f) return;
        
        // Spring force proportional to depth
        float springForce = pointData.depth * buoyancyForce;
        
        // Clamp force to prevent instability
        springForce = Mathf.Clamp(springForce, 0f, maxBuoyancyForcePerPoint);
        
        // Calculate velocity at this point (considering rotation)
        Vector3 pointVelocity = _rb.GetPointVelocity(pointData.transform.position);
        float verticalVelocity = Vector3.Dot(pointVelocity, Vector3.up);
        
        // Damping force to prevent oscillation
        float dampingForce = -verticalVelocity * buoyancyDamping;
        
        // Total upward force
        float totalForce = springForce + dampingForce;
        Vector3 forceVector = Vector3.up * totalForce;
        
        // Apply force at the point position (this creates torque naturally)
        _rb.AddForceAtPosition(forceVector, pointData.transform.position, ForceMode.Acceleration);
        
        // Debug visualization
        if (showDebugRays)
        {
            Debug.DrawRay(pointData.transform.position, forceVector.normalized * 2f, Color.magenta);
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
        // Draw lines connecting the 4 points to show the buoyancy quad
        if (backRightPoint != null && backLeftPoint != null && frontLeftPoint != null && frontRightPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(backRightPoint.position, backLeftPoint.position);
            Gizmos.DrawLine(backLeftPoint.position, frontLeftPoint.position);
            Gizmos.DrawLine(frontLeftPoint.position, frontRightPoint.position);
            Gizmos.DrawLine(frontRightPoint.position, backRightPoint.position);
            
            // Draw spheres at each point
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(backRightPoint.position, 0.2f);
            Gizmos.DrawWireSphere(backLeftPoint.position, 0.2f);
            Gizmos.DrawWireSphere(frontLeftPoint.position, 0.2f);
            Gizmos.DrawWireSphere(frontRightPoint.position, 0.2f);
            
            // Draw raycast origins (below each point by target height)
            Gizmos.color = Color.green;
            Vector3 offset = new Vector3(0, targetHeightAboveWave, 0);
            Gizmos.DrawWireSphere(backRightPoint.position - offset, 0.15f);
            Gizmos.DrawWireSphere(backLeftPoint.position - offset, 0.15f);
            Gizmos.DrawWireSphere(frontLeftPoint.position - offset, 0.15f);
            Gizmos.DrawWireSphere(frontRightPoint.position - offset, 0.15f);
        }
    }
}
