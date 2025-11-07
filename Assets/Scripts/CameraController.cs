using UnityEngine;
using Cinemachine;

public class RacingCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform vehicleTransform;
    
    [Header("Cinemachine")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    
    [Header("Camera Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -6f);
    [SerializeField] private float followSpeed = 10f; // Higher = snappier position follow
    [SerializeField] private float rotationSpeed = 5f; // Higher = snappier rotation follow
    
    [Header("Look Input (Optional)")]
    [SerializeField] private bool allowFreeLook = true;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 60f;
    
    private float _freeLookYaw;
    private float _freeLookPitch;
    
    void LateUpdate()
    {
        if (vehicleTransform == null) return;
        
        // Follow vehicle rotation with some smoothing (only Y-axis for racing)
        float targetYaw = vehicleTransform.eulerAngles.y + _freeLookYaw;
        Quaternion targetRotation = Quaternion.Euler(_freeLookPitch, targetYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        // Position camera behind vehicle using consistent horizontal plane
        Quaternion horizontalRotation = Quaternion.Euler(0f, vehicleTransform.eulerAngles.y, 0f);
        Vector3 offsetPosition = horizontalRotation * offset;
        Vector3 targetPosition = vehicleTransform.position + offsetPosition;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }
    
    public void HandleLookInput(Vector2 lookInput)
    {
        if (!allowFreeLook) return;
        
        _freeLookYaw += lookInput.x * lookSensitivity;
        _freeLookPitch -= lookInput.y * lookSensitivity;
        
        // Clamp look angles
        _freeLookYaw = Mathf.Clamp(_freeLookYaw, -maxLookAngle, maxLookAngle);
        _freeLookPitch = Mathf.Clamp(_freeLookPitch, -30f, 30f);
        
        // Gradually return to center when not looking
        if (Mathf.Abs(lookInput.x) < 0.01f && Mathf.Abs(lookInput.y) < 0.01f)
        {
            _freeLookYaw = Mathf.Lerp(_freeLookYaw, 0f, 2f * Time.deltaTime);
            _freeLookPitch = Mathf.Lerp(_freeLookPitch, 0f, 2f * Time.deltaTime);
        }
    }
}
