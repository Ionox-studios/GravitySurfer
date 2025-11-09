using System.Diagnostics;
using UnityEngine;

public class HoverVehicleController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float drag = 2f;
    
    [Header("Turning")]
    [SerializeField] private float turnSpeed = 100f;
    [SerializeField] private float turnDrag = 3f;
    
    [Header("Hover")]
    [SerializeField] private float hoverHeight = 2f;
    [SerializeField] private float hoverForce = 300f;
    [SerializeField] private float hoverDamping = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool showDebugRays = false;
    
    [Header("Tilt")]
    [SerializeField] private float tiltAngle = 15f;
    [SerializeField] private float tiltSpeed = 2f;
    
    [Header("Stability")]
    [SerializeField] private float stabilityForce = 50f;
    [SerializeField] private Transform visualModel; // Separate visual from physics
    
    private Rigidbody _rb;
    private float _currentSpeed;
    private float _targetTilt;
    private BarycentricAlignment _barycentricAlignment;
    
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _barycentricAlignment = GetComponent<BarycentricAlignment>();
        
        // Set up rigidbody for vehicle physics (inheriting mass and damping from component)
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // CRITICAL: Lock X and Z rotation to prevent flipping (unless using barycentric alignment)
        UpdateRigidbodyConstraints();
    }
    
    void FixedUpdate()
    {
        _currentSpeed = _rb.linearVelocity.magnitude;
        
        // Update constraints in FixedUpdate to sync with physics
        UpdateRigidbodyConstraints();
    }
    
    void UpdateRigidbodyConstraints()
    {
        if (_barycentricAlignment != null && _barycentricAlignment.IsAlignmentEnabled)
        {
            // Allow full rotation when using barycentric alignment
            _rb.constraints = RigidbodyConstraints.None;
            UnityEngine.Debug.Log("Barycentric alignment enabled; unlocking rigidbody rotation.");
        }
        else
        {
            // Lock X and Z rotation for standard hover mode, but allow Y rotation for turning
            //_rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            // Note: Y rotation is intentionally NOT frozen so the vehicle can turn
        }
    }
    
    public void Move(Vector2 input)
    {
        // Forward/backward acceleration
        float forwardInput = input.y;
        float turnInput = input.x;
        
        // Apply acceleration in forward direction
        Vector3 acceleration = transform.forward * forwardInput * this.acceleration;
        _rb.AddForce(acceleration, ForceMode.Acceleration);
        
        // Limit max speed
        Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity);
        localVelocity.z = Mathf.Clamp(localVelocity.z, -maxSpeed, maxSpeed);
        _rb.linearVelocity = transform.TransformDirection(localVelocity);
        
        // Apply custom drag
        _rb.linearVelocity *= (1f - drag * Time.fixedDeltaTime);
        
        // Turning (like steering) - only if not using barycentric alignment
        if (Mathf.Abs(forwardInput) > 0.1f && !(_barycentricAlignment != null && _barycentricAlignment.IsAlignmentEnabled))
        {
            float turn = turnInput * turnSpeed * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            _rb.MoveRotation(_rb.rotation * turnRotation);
        }
        
        // Banking/tilting when turning
        _targetTilt = -turnInput * tiltAngle;
        
        // Apply hovering force
        ApplyHoverForce();
        
        // Keep vehicle upright
        ApplyStabilityForce();
        
        // Apply tilt to visual model only
        ApplyVisualTilt();
    }
    
// HoverVehicleController.ApplyHoverForce()
private void ApplyHoverForce()
{
    // Use barycentric up/normal if available, else fall back to world up
    Vector3 upDir = (_barycentricAlignment != null && _barycentricAlignment.IsAlignedToSurface())
        ? _barycentricAlignment.CurrentNormal   // surface normal
        : Vector3.up;

    Vector3 rayStart   = transform.position;
    float   rayDistance = hoverHeight * 2f;

    if (Physics.Raycast(rayStart, -upDir, out RaycastHit hit, rayDistance, groundLayer))
    {
        float heightDifference = hoverHeight - hit.distance;
        float force = heightDifference * hoverForce;

        // Dampen only the velocity component along the normal
        float normalVel = Vector3.Dot(_rb.linearVelocity, upDir);
        force -= normalVel * hoverDamping;

        _rb.AddForce(upDir * force, ForceMode.Acceleration);
    }
}

    
    private void ApplyStabilityForce()
    {
        // Skip stability if using barycentric alignment (it handles rotation)
        if (_barycentricAlignment != null && _barycentricAlignment.IsAlignmentEnabled)
        {
            UnityEngine.Debug.Log("Barycentric alignment enabled; skipping stability force.");
            return;
        }
        
        // Keep the vehicle level (prevent pitch and roll)
        Vector3 up = transform.up;
        float angle = Vector3.Angle(up, Vector3.up);
        
        if (angle > 0.1f)
        {
            Vector3 correctionTorque = Vector3.Cross(up, Vector3.up) * stabilityForce;
            _rb.AddTorque(correctionTorque, ForceMode.Acceleration);
        }
    }
    
    private void ApplyVisualTilt()
    {
        if (visualModel == null) return;
        
        // Smoothly tilt the VISUAL model when turning (banking effect)
        // The physics body stays upright, only the model tilts
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, _targetTilt);
        visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, targetRotation, tiltSpeed * Time.fixedDeltaTime);
    }
    
    public float GetCurrentSpeed()
    {
        return _currentSpeed;
    }
    
    public Vector3 GetVelocity()
    {
        return _rb.linearVelocity;
    }
}
