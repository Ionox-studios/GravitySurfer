using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.InputSystem;

/// <summary>
/// Handles cutscene playback with skip functionality
/// </summary>
public class CutsceneController : MonoBehaviour
{
    [Header("Input System")]
    [Tooltip("Input Action Asset reference")]
    [SerializeField] private InputActionAsset inputActions;
    
    [Header("Cutscene Settings")]
    [Tooltip("Minimum time before cutscene can be skipped (seconds)")]
    public float minPlayTimeBeforeSkip = 1f;
    
    [Header("Optional: Video Player")]
    [Tooltip("If using Unity VideoPlayer component")]
    public VideoPlayer videoPlayer;
    
    [Header("Optional: Timeline/Animation")]
    [Tooltip("Duration of cutscene if not using VideoPlayer")]
    public float cutsceneDuration = 10f;
    
    [Header("UI")]
    [Tooltip("Optional UI text to show 'Press [key] to skip'")]
    public GameObject skipPromptUI;

    private float cutsceneStartTime;
    private bool hasSkipped = false;
    private InputAction _skipAction;

    void Start()
    {
        cutsceneStartTime = Time.time;
        
        // Setup input action for skipping
        if (inputActions != null)
        {
            var uiActionMap = inputActions.FindActionMap("UI");
            if (uiActionMap != null)
            {
                _skipAction = uiActionMap.FindAction("Start"); // Reuse Start action or create a Skip action
                if (_skipAction == null)
                {
                    _skipAction = uiActionMap.FindAction("Skip");
                }
                
                if (_skipAction != null)
                {
                    _skipAction.performed += OnSkipInput;
                    uiActionMap.Enable();
                }
                else
                {
                    Debug.LogWarning("Skip/Start action not found in UI action map. Add 'Skip' action to InputActions.");
                }
            }
        }
        else
        {
            Debug.LogWarning("InputActionAsset not assigned to CutsceneController!");
        }
        
        // Show skip prompt if available
        if (skipPromptUI != null)
        {
            skipPromptUI.SetActive(true);
        }

        // If using VideoPlayer, subscribe to end event
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnCutsceneEnd;
            videoPlayer.Play();
        }
        else
        {
            // Auto-end after duration
            StartCoroutine(AutoEndCutscene());
        }
    }

    private void OnSkipInput(InputAction.CallbackContext context)
    {
        // Check if enough time has passed to allow skipping
        if (!hasSkipped && Time.time - cutsceneStartTime >= minPlayTimeBeforeSkip)
        {
            SkipCutscene();
        }
    }

    /// <summary>
    /// Skip the cutscene immediately
    /// </summary>
    public void SkipCutscene()
    {
        if (hasSkipped) return;
        
        hasSkipped = true;
        
        Debug.Log("Cutscene skipped");
        
        // Stop video if playing
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
        
        EndCutscene();
    }

    /// <summary>
    /// Called when cutscene naturally ends
    /// </summary>
    private void OnCutsceneEnd(VideoPlayer vp)
    {
        if (!hasSkipped)
        {
            EndCutscene();
        }
    }

    /// <summary>
    /// Auto-end cutscene after duration (when not using VideoPlayer)
    /// </summary>
    private IEnumerator AutoEndCutscene()
    {
        yield return new WaitForSeconds(cutsceneDuration);
        
        if (!hasSkipped)
        {
            EndCutscene();
        }
    }

    /// <summary>
    /// Finish cutscene and transition to next scene
    /// </summary>
    private void EndCutscene()
    {
        // Hide skip prompt
        if (skipPromptUI != null)
        {
            skipPromptUI.SetActive(false);
        }

        // Tell the GameSceneManager to continue
        if (GameSceneManager.Instance != null)
        {
            // Check if this is a pre-race cutscene or post-race cutscene
            // If current level has a racing scene and we're in the pre-race cutscene, load the race
            int currentLevel = GameSceneManager.Instance.GetCurrentLevelIndex();
            if (currentLevel >= 0)
            {
                RacingLevel level = GameSceneManager.Instance.GetLevel(currentLevel);
                string currentScene = SceneManager.GetActiveScene().name;
                
                // If we're in the intro cutscene, load the race
                if (level != null && currentScene == level.cutsceneBeforeRace)
                {
                    SceneManager.LoadScene(level.racingSceneName);
                    return;
                }
            }
            
            // Otherwise, it's a post-race cutscene, continue to next level
            GameSceneManager.Instance.OnCutsceneComplete();
        }
        else
        {
            // Fallback: return to menu if no scene manager
            Debug.LogWarning("GameSceneManager not found, returning to menu");
            SceneManager.LoadScene("Menu");
        }
    }

    void OnDestroy()
    {
        // Cleanup input actions
        if (_skipAction != null)
        {
            _skipAction.performed -= OnSkipInput;
        }
        
        // Cleanup video player
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnCutsceneEnd;
        }
    }
}
