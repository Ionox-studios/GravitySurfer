using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple input handler for VehicleController.
/// Uses Unity Input System - same pattern as InputHandler.cs
/// </summary>
public class SimpleInputHandler : MonoBehaviour
{
    public VehicleController vehicleController;
    
    [SerializeField] private InputActionAsset inputActions;
    
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private Vector2 _currentMoveInput;
    
    void Start()
    {
        // Auto-find the controller component if not assigned
        if (vehicleController == null)
        {
            vehicleController = GetComponent<VehicleController>();
        }
        
        if (vehicleController == null)
        {
            Debug.LogError("SimpleInputHandler: No VehicleController found!");
            return;
        }
        
        if (inputActions == null)
        {
            Debug.LogError("SimpleInputHandler: Input Actions asset not assigned!");
            return;
        }
        
        // Get the action map
        var playerActionMap = inputActions.FindActionMap("Player");
        
        if (playerActionMap == null)
        {
            Debug.LogError("SimpleInputHandler: Could not find 'Player' action map!");
            return;
        }
        
        // Find the Move action
        _moveAction = playerActionMap.FindAction("Move");
        
        if (_moveAction == null)
        {
            Debug.LogError("SimpleInputHandler: Could not find 'Move' action!");
            return;
        }
        
        // Find the Jump action
        _jumpAction = playerActionMap.FindAction("Jump");
        
        if (_jumpAction == null)
        {
            Debug.LogWarning("SimpleInputHandler: Could not find 'Jump' action!");
        }
        else
        {
            // Subscribe to jump button press
            _jumpAction.performed += OnJump;
        }
        
        // Enable the action map
        playerActionMap.Enable();
        
        Debug.Log("SimpleInputHandler initialized successfully!");
    }
    
    private void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (vehicleController != null)
        {
            vehicleController.Jump();
        }
    }
    
    void Update()
    {
        if (_moveAction == null) return;
        
        // Read input value
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        _currentMoveInput = moveInput;
        
        // Debug to see if input is coming through
        if (moveInput.magnitude > 0.1f)
        {
            Debug.Log($"Input detected: {moveInput}");
        }
    }
    
    void FixedUpdate()
    {
        // Physics-based movement in FixedUpdate
        if (vehicleController != null)
        {
            vehicleController.Move(_currentMoveInput);
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from jump event
        if (_jumpAction != null)
        {
            _jumpAction.performed -= OnJump;
        }
        
        // Clean up - disable actions when destroyed
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }
}
