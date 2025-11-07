using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    
    private InputAction _quitAction;
    
    void Start()
    {
        // Get the UI action map (or create a Game action map)
        var uiActionMap = inputActions.FindActionMap("UI");
        
        // Find the quit action
        _quitAction = uiActionMap.FindAction("Quit");
        
        // Subscribe to the quit action
        if (_quitAction != null)
        {
            _quitAction.performed += OnQuit;
            uiActionMap.Enable();
        }
        else
        {
            Debug.LogWarning("Quit action not found in InputActions. Make sure 'Quit' action exists in the 'UI' action map.");
        }
    }
    
    private void OnQuit(InputAction.CallbackContext context)
    {
        QuitGame();
    }
    
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        
        #if UNITY_EDITOR
            // If running in the Unity Editor, stop playing
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // If running as a built application, quit
            Application.Quit();
        #endif
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (_quitAction != null)
        {
            _quitAction.performed -= OnQuit;
        }
    }
}
