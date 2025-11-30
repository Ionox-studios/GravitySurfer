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
    private InputAction _attackAction;
    private InputAction _interactAction;
    private InputAction _crouchAction;
    private InputAction _respawnAction;
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
       

        // Find the Attack action
        _attackAction = playerActionMap.FindAction("Attack");

        if (_attackAction != null)
        {
            _attackAction.performed += OnAttack;
        }
        else
        {
            Debug.LogWarning("SimpleInputHandler: Could not find 'Attack' action!");
        }
        
        // Find the Interact action
        _interactAction = playerActionMap.FindAction("Interact");

        if (_interactAction != null)
        {
            _interactAction.performed += OnInteract;
        }
        else
        {
            Debug.LogWarning("SimpleInputHandler: Could not find 'Interact' action!");
        }
        
        // Find the Crouch action
        _crouchAction = playerActionMap.FindAction("Crouch");

        if (_crouchAction != null)
        {
            _crouchAction.started += OnCrouchStarted;
            _crouchAction.canceled += OnCrouchCanceled;
        }
        else
        {
            Debug.LogWarning("SimpleInputHandler: Could not find 'Crouch' action!");
        }
        
        // Find the Respawn action
        _respawnAction = playerActionMap.FindAction("Respawn");

        if (_respawnAction != null)
        {
            _respawnAction.performed += OnRespawn;
        }
        else
        {
            Debug.LogWarning("SimpleInputHandler: Could not find 'Respawn' action!");
        }
    }  

    private void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (vehicleController != null)
        {
            vehicleController.Jump();
        }
    }    
    private void OnAttack(InputAction.CallbackContext context)
    {
        if (vehicleController != null)
        {
            vehicleController.Attack();
        }
    }
    
    private void OnInteract(InputAction.CallbackContext context)
    {
        if (vehicleController != null)
        {
            vehicleController.ActivateBoost();
        }
    }
    
    private void OnCrouchStarted(InputAction.CallbackContext context)
    {
        if (vehicleController != null)
        {
            vehicleController.StartGround();
        }
    }
    
    private void OnCrouchCanceled(InputAction.CallbackContext context)
    {
        if (vehicleController != null)
        {
            vehicleController.StopGround();
        }
    }
    
    private void OnRespawn(InputAction.CallbackContext context)
    {
        RespawnManager respawnManager = GetComponent<RespawnManager>();
        if (respawnManager != null)
        {
            respawnManager.Respawn();
        }
        else
        {
            Debug.LogWarning("SimpleInputHandler: No RespawnManager found on this GameObject!");
        }
    }
  /////   
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
        
        // Unsubscribe from attack event
        if (_attackAction != null)
        {
            _attackAction.performed -= OnAttack;
        }
        
        // Unsubscribe from interact event
        if (_interactAction != null)
        {
            _interactAction.performed -= OnInteract;
        }
        
        // Unsubscribe from crouch events
        if (_crouchAction != null)
        {
            _crouchAction.started -= OnCrouchStarted;
            _crouchAction.canceled -= OnCrouchCanceled;
        }
        
        // Unsubscribe from respawn event
        if (_respawnAction != null)
        {
            _respawnAction.performed -= OnRespawn;
        }
        
        // Clean up - disable actions when destroyed
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }
}
