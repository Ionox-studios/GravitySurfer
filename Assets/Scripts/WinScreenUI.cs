using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays the leaderboard on the win screen
/// </summary>
public class WinScreenUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI leaderboardText;
    
    [Header("Display Settings")]
    [SerializeField] private bool highlightPlayerEntry = true;
    [SerializeField] private string highlightPrefix = ">>> ";
    [SerializeField] private string highlightSuffix = " <<<";

    private float playerFinishTime = -1f;

    void OnEnable()
    {
        // Update the display when the win panel is shown
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        int currentLevelIndex = 0;
        
        // Get player's finish time from GameController
        if (GameController.Instance != null)
        {
            playerFinishTime = GameController.Instance.GetTimer();
            currentLevelIndex = GameController.Instance.GetLevelIndex();
        }

        // Get and display leaderboard for the current level
        if (GameSceneManager.Instance != null && leaderboardText != null)
        {
            LeaderboardManager leaderboard = GameSceneManager.Instance.GetLeaderboard(currentLevelIndex);
            if (leaderboard != null)
            {
                DisplayLeaderboard(leaderboard);
            }
        }
    }

    private void DisplayLeaderboard(LeaderboardManager leaderboard)
    {
        List<LeaderboardEntry> entries = leaderboard.GetEntries();
        
        // Start with finish message and player time
        int minutes = Mathf.FloorToInt(playerFinishTime / 60f);
        int seconds = Mathf.FloorToInt(playerFinishTime % 60f);
        string displayText = $"!!! FINISHED !!!\n\nYour Time: {minutes}:{seconds:00}\n\n";
        
        // Add leaderboard
        displayText += "BEST SCORES:\n";
        
        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry entry = entries[i];
            string line = $"{entry.playerTag,-6} {entry.GetFormattedTime()}";
            
            // Highlight player's entry if it matches their time
            if (highlightPlayerEntry && 
                playerFinishTime > 0 && 
                Mathf.Approximately(entry.timeInSeconds, playerFinishTime))
            {
                line = highlightPrefix + line + highlightSuffix;
            }
            
            displayText += line + "\n";
        }
        
        leaderboardText.text = displayText;
    }

    /// <summary>
    /// Manually refresh the display (can be called from other scripts)
    /// </summary>
    public void Refresh()
    {
        UpdateDisplay();
    }
}
