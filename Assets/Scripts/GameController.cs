using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class GameController : MonoBehaviour
{
    public static GameController Instance;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    
    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public GameObject startPanel;
    public GameObject gameOverPanel;
    public GameObject winPanel;
    public GameObject deathPanel;

    [Header("Player Reference")]
    public Transform player;

    private InputAction _quitAction;
    private InputAction _startAction;
    private InputAction _restartAction;
    
    private float timer = 0f;
    private bool gameStarted = false;
    private bool gameEnded = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        // Get the UI action map
        var uiActionMap = inputActions.FindActionMap("UI");
        
        // Find and subscribe to actions
        _quitAction = uiActionMap.FindAction("Quit");
        _startAction = uiActionMap.FindAction("Start");
        _restartAction = uiActionMap.FindAction("Restart");
        
        if (_quitAction != null)
        {
            _quitAction.performed += OnQuit;
        }
        else
        {
            Debug.LogWarning("Quit action not found in InputActions.");
        }

        if (_startAction != null)
        {
            _startAction.performed += OnStart;
        }
        else
        {
            Debug.LogWarning("Start action not found. Add 'Start' action to 'UI' action map.");
        }

        if (_restartAction != null)
        {
            _restartAction.performed += OnRestart;
        }
        else
        {
            Debug.LogWarning("Restart action not found. Add 'Restart' action to 'UI' action map.");
        }

        uiActionMap.Enable();

        // Setup game state
        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (deathPanel != null) deathPanel.SetActive(false);
        
        // Pause time until game starts
        Time.timeScale = 0f;
    }

    void Update()
    {
        // Update timer if game is running
        if (gameStarted && !gameEnded)
        {
            timer += Time.deltaTime;
            UpdateTimerDisplay();

            // Check if player fell below Y = -100
            if (player != null && player.position.y < -100f)
            {
                // Try to respawn instead of game over
                RespawnManager respawnManager = player.GetComponent<RespawnManager>();
                if (respawnManager != null)
                {
                    respawnManager.Respawn();
                }
                else
                {
                    GameOver();
                }
            }
        }
    }

    private void OnStart(InputAction.CallbackContext context)
    {
        if (!gameStarted)
        {
            StartGame();
        }
    }

    private void OnRestart(InputAction.CallbackContext context)
    {
        if (gameEnded)
        {
            RestartGame();
        }
    }

    void StartGame()
    {
        gameStarted = true;
        if (startPanel != null) startPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);
        int milliseconds = Mathf.FloorToInt((timer * 100f) % 100f);
        
        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }

    void GameOver()
    {
        if (gameEnded) return;
        
        gameEnded = true;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Win()
    {
        if (gameEnded) return;
        
        gameEnded = true;
        if (winPanel != null) winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
        if (_startAction != null)
        {
            _startAction.performed -= OnStart;
        }
        if (_restartAction != null)
        {
            _restartAction.performed -= OnRestart;
        }
    }
}
