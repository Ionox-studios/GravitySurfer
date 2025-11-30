using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Instant win trigger that loads a specific scene immediately when the player enters.
/// Perfect for creating multiple endings or branching paths in a level.
/// </summary>
public class DirectSceneWinTrigger : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the scene to load when player enters this trigger")]
    [SerializeField] private string targetSceneName;
    
    [Tooltip("Optional delay before loading the scene (in seconds)")]
    [SerializeField] private float transitionDelay = 0f;
    
    [Header("UI Settings")]
    [Tooltip("Show the win panel before transitioning?")]
    [SerializeField] private bool showWinPanel = false;
    
    [Tooltip("Custom message to show on win panel (optional)")]
    [SerializeField] private string customWinMessage = "";
    
    [Header("Debug")]
    [Tooltip("Log when trigger is activated")]
    [SerializeField] private bool debugMode = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Only trigger once
        if (hasTriggered) return;
        
        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            
            if (debugMode)
            {
                Debug.Log($"DirectSceneWinTrigger activated! Loading scene: {targetSceneName}");
            }
            
            // Optional: Show win panel if requested
            if (showWinPanel && GameController.Instance != null)
            {
                // Mark game as ended to stop timer
                GameController.Instance.Win();
            }
            
            // Load the target scene
            if (transitionDelay > 0)
            {
                StartCoroutine(LoadSceneAfterDelay());
            }
            else
            {
                LoadTargetScene();
            }
        }
    }

    private System.Collections.IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(transitionDelay);
        LoadTargetScene();
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("DirectSceneWinTrigger: No target scene name specified!");
            return;
        }
        
        // Reset time scale before loading (in case game was paused)
        Time.timeScale = 1f;
        
        // Load the scene
        SceneManager.LoadScene(targetSceneName);
    }

    // Validation in editor
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"DirectSceneWinTrigger on {gameObject.name}: Target scene name is not set!");
        }
    }

    // Visualize the trigger in the editor
    private void OnDrawGizmos()
    {
        // Draw a colored wireframe to show this is a special trigger
        Gizmos.color = string.IsNullOrEmpty(targetSceneName) ? Color.yellow : Color.cyan;
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }
}
