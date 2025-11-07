using UnityEngine;

public class BarycentricAlignment : MonoBehaviour
{
    [Header("Alignment Settings")]
    [SerializeField] private bool enableAlignment = true;
    [SerializeField] private float alignmentSpeed = 5f;
    [SerializeField] private float raycastDistance = 5f;
    
    [Header("Raycast Positions")]
    [SerializeField] private float raycastWidth = 1f; // Distance from center to left/right
    [SerializeField] private float raycastForwardOffset = 1f; // How far forward from center
    [SerializeField] private LayerMask surfaceLayer;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRays = true;
    [SerializeField] private bool showDebugInfo = false;
    
    private Rigidbody _rb;
    private Transform _transform;
    
    // Raycast hit storage
    private RaycastHit _leftHit;
    private RaycastHit _rightHit;
    private bool _leftHitValid;
    private bool _rightHitValid;
    
    public bool IsAlignmentEnabled => enableAlignment;
    
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _transform = transform;
    }
    
    void FixedUpdate()
    {
        if (!enableAlignment) return;
        
        PerformRaycasts();
        
        if (_leftHitValid && _rightHitValid)
        {
            AlignToSurface();
        }
    }
    
    private void PerformRaycasts()
    {
        // Calculate raycast start positions in world space
        Vector3 forward = _transform.forward;
        Vector3 right = _transform.right;
        Vector3 centerPos = _transform.position + forward * raycastForwardOffset;
        
        Vector3 leftStart = centerPos - right * raycastWidth;
        Vector3 rightStart = centerPos + right * raycastWidth;
        
        // Use the vehicle's down direction for raycasting
        Vector3 rayDirection = -_transform.up;
        
        // Perform raycasts
        _leftHitValid = Physics.Raycast(leftStart, rayDirection, out _leftHit, raycastDistance, surfaceLayer);
        _rightHitValid = Physics.Raycast(rightStart, rayDirection, out _rightHit, raycastDistance, surfaceLayer);
        
        // Debug visualization
        if (showDebugRays)
        {
            Color leftColor = _leftHitValid ? Color.green : Color.red;
            Color rightColor = _rightHitValid ? Color.green : Color.red;
            
            Debug.DrawRay(leftStart, rayDirection * (_leftHitValid ? _leftHit.distance : raycastDistance), leftColor);
            Debug.DrawRay(rightStart, rayDirection * (_rightHitValid ? _rightHit.distance : raycastDistance), rightColor);
            
            if (_leftHitValid)
            {
                Debug.DrawRay(_leftHit.point, _leftHit.normal * 0.5f, Color.cyan);
            }
            if (_rightHitValid)
            {
                Debug.DrawRay(_rightHit.point, _rightHit.normal * 0.5f, Color.cyan);
            }
        }
    }
    
    private void AlignToSurface()
    {
        // Get surface normals using barycentric coordinates for precision
        Vector3 leftNormal = GetBarycentricNormal(_leftHit);
        Vector3 rightNormal = GetBarycentricNormal(_rightHit);
        
        // Average the normals to get the surface orientation
        Vector3 averageNormal = (leftNormal + rightNormal).normalized;
        
        // Calculate the target "up" direction (perpendicular to surface)
        Vector3 targetUp = averageNormal;
        
        // Calculate the target "forward" direction
        // Project current forward onto the surface plane
        Vector3 currentForward = _transform.forward;
        Vector3 targetForward = Vector3.ProjectOnPlane(currentForward, targetUp).normalized;
        
        // If the projection is too small (nearly perpendicular), use velocity direction instead
        if (targetForward.sqrMagnitude < 0.1f)
        {
            Vector3 velocityDir = _rb.linearVelocity.normalized;
            targetForward = Vector3.ProjectOnPlane(velocityDir, targetUp).normalized;
        }
        
        // Construct target rotation
        Quaternion targetRotation = Quaternion.LookRotation(targetForward, targetUp);
        
        // Store current velocity (to maintain it after rotation)
        Vector3 currentVelocity = _rb.linearVelocity;
        float velocityMagnitude = currentVelocity.magnitude;
        
        // Smoothly rotate the rigidbody
        Quaternion newRotation = Quaternion.Slerp(_rb.rotation, targetRotation, alignmentSpeed * Time.fixedDeltaTime);
        _rb.MoveRotation(newRotation);
        
        // Maintain velocity magnitude but update direction to match new forward
        // This is crucial for loops - velocity rotates with the vehicle
        if (velocityMagnitude > 0.1f)
        {
            // Calculate how much we rotated
            Quaternion rotationDelta = newRotation * Quaternion.Inverse(_rb.rotation);
            
            // Rotate the velocity vector by the same amount
            Vector3 rotatedVelocity = rotationDelta * currentVelocity;
            _rb.linearVelocity = rotatedVelocity;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Aligning to surface. Normal: {averageNormal}, Forward: {targetForward}");
        }
    }
    
    private Vector3 GetBarycentricNormal(RaycastHit hit)
    {
        // If we hit a mesh collider, use barycentric coordinates for precise normal
        MeshCollider meshCollider = hit.collider as MeshCollider;
        
        if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            Mesh mesh = meshCollider.sharedMesh;
            Vector3 barycentricCoord = hit.barycentricCoordinate;
            
            // Get the triangle indices
            int[] triangles = mesh.triangles;
            int triangleIndex = hit.triangleIndex * 3;
            
            if (triangleIndex >= 0 && triangleIndex + 2 < triangles.Length)
            {
                int i0 = triangles[triangleIndex];
                int i1 = triangles[triangleIndex + 1];
                int i2 = triangles[triangleIndex + 2];
                
                // Get the normals at each vertex
                Vector3[] normals = mesh.normals;
                
                if (i0 < normals.Length && i1 < normals.Length && i2 < normals.Length)
                {
                    Vector3 n0 = normals[i0];
                    Vector3 n1 = normals[i1];
                    Vector3 n2 = normals[i2];
                    
                    // Interpolate normal using barycentric coordinates
                    Vector3 interpolatedNormal = n0 * barycentricCoord.x +
                                                 n1 * barycentricCoord.y +
                                                 n2 * barycentricCoord.z;
                    
                    // Transform to world space
                    Vector3 worldNormal = hit.transform.TransformDirection(interpolatedNormal).normalized;
                    
                    return worldNormal;
                }
            }
        }
        
        // Fallback to the standard hit normal
        return hit.normal;
    }
    
    public void SetAlignmentEnabled(bool enabled)
    {
        enableAlignment = enabled;
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugRays) return;
        
        // Draw the raycast positions
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 centerPos = transform.position + forward * raycastForwardOffset;
        
        Vector3 leftPos = centerPos - right * raycastWidth;
        Vector3 rightPos = centerPos + right * raycastWidth;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(leftPos, 0.1f);
        Gizmos.DrawWireSphere(rightPos, 0.1f);
        
        // Draw line between them
        Gizmos.DrawLine(leftPos, rightPos);
    }
}
