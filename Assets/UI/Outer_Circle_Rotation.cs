using UnityEngine;

public class Outer_Circle_Rotation : MonoBehaviour
{    
    [Header("Rotation Settings")]
    [SerializeField] private float minRotation = 10.0f;      // rotation at zero speed
    [SerializeField] private float maxRotation = 10000.0f;      // rotation at max speed
    [SerializeField] private float rotationLerpSpeed = 100.0f; // How quickly rotation changes

    [Header("Reference")]
    [SerializeField] private VehicleController _vehicle; 
    private float _currentRotation;

    void Awake()
    {
        _currentRotation = minRotation;
        
    }

    // Update is called once per frame
    void Update()
    {
        
        // Get normalized speed (0-1 range)
        float speedNormalized = _vehicle.GetNormalizedSpeed();

        float targetRotation = Mathf.Lerp(minRotation, maxRotation, speedNormalized);

        _currentRotation = Mathf.Lerp(_currentRotation, targetRotation, 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime));

        transform.Rotate(0, 0, _currentRotation * Time.deltaTime);
        
    }
}
