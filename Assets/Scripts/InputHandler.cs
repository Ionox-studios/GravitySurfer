using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public HoverVehicleController VehicleController;
    public RacingCameraController CameraController;

    [SerializeField] private InputActionAsset inputActions;
    
    private InputAction _moveAction, _lookAction, _jumpAction, _attackAction;
    
    void Start()
    {
        // Get the action map
        var playerActionMap = inputActions.FindActionMap("Player");
        
        // Find actions within the action map
        _moveAction = playerActionMap.FindAction("Move");
        _lookAction = playerActionMap.FindAction("Look");
        _jumpAction = playerActionMap.FindAction("Jump");
        _attackAction = playerActionMap.FindAction("Attack"); // AL
        
        // Enable the action map
        playerActionMap.Enable();

        Cursor.visible = false;
    }
    
    void Update()
    {
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        Vector2 lookInput = _lookAction.ReadValue<Vector2>();

        // Movement handled in FixedUpdate for physics
        _currentMoveInput = moveInput;
        
        // Camera can update in regular Update
        CameraController.HandleLookInput(lookInput);
    }
    
    private Vector2 _currentMoveInput;
    
    void FixedUpdate()
    {
        // Physics-based movement should be in FixedUpdate
        VehicleController.Move(_currentMoveInput);
    }
    
    void OnDestroy()
    {
        // Clean up - disable actions when destroyed
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }
}
