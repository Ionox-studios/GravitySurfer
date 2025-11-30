using UnityEngine;
using TMPro;

/// <summary>
/// UI component to display race rankings
/// Shows the player's current position and total racers
/// </summary>
public class RaceRankingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI rankingText;
    [Tooltip("Text to display current ranking (e.g., '1st / 4')")]
    
    [SerializeField] private GameObject playerObject;
    [Tooltip("Reference to the player GameObject to get their rank")]
    
    [Header("Settings")]
    [SerializeField] private bool showDetailedRankings = false;
    [Tooltip("Show all racer positions, not just player")]
    
    [SerializeField] private float updateInterval = 0.1f;
    [Tooltip("How often to update the UI (in seconds)")]
    
    private IRacer playerRacer;
    private float updateTimer = 0f;

    void Start()
    {
        // Get the player's IRacer component
        if (playerObject != null)
        {
            playerRacer = playerObject.GetComponent<IRacer>();
            if (playerRacer == null)
            {
                Debug.LogWarning("RaceRankingUI: Player object does not have IRacer component!");
            }
        }
        else
        {
            Debug.LogWarning("RaceRankingUI: No player object assigned!");
        }
    }

    void Update()
    {
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateRankingDisplay();
        }
    }

    /// <summary>
    /// Update the ranking text display
    /// </summary>
    private void UpdateRankingDisplay()
    {
        if (rankingText == null) return;
        if (RaceRankingSystem.Instance == null) return;

        if (showDetailedRankings)
        {
            ShowDetailedRankings();
        }
        else
        {
            ShowPlayerRanking();
        }
    }

    /// <summary>
    /// Show only the player's rank
    /// </summary>
    private void ShowPlayerRanking()
    {
        if (playerRacer == null || !playerRacer.IsAlive())
        {
            rankingText.text = "-- / --";
            return;
        }

        int playerRank = RaceRankingSystem.Instance.GetRacerRank(playerRacer);
        int totalRacers = RaceRankingSystem.Instance.GetAliveRacerCount();

        if (playerRank > 0)
        {
            rankingText.text = $"{GetRankSuffix(playerRank)} / {totalRacers}";
        }
        else
        {
            rankingText.text = "-- / --";
        }
    }

    /// <summary>
    /// Show detailed rankings of all racers
    /// </summary>
    private void ShowDetailedRankings()
    {
        var rankings = RaceRankingSystem.Instance.GetAllRankings();
        
        if (rankings.Count == 0)
        {
            rankingText.text = "No active racers";
            return;
        }

        string displayText = "=== RANKINGS ===\n";
        
        foreach (var (racer, rank) in rankings)
        {
            string racerName = racer.GetRacerName();
            bool isPlayer = racer == playerRacer;
            
            // Highlight player in the list
            if (isPlayer)
            {
                displayText += $"<color=yellow>{GetRankSuffix(rank)}. {racerName}</color>\n";
            }
            else
            {
                displayText += $"{GetRankSuffix(rank)}. {racerName}\n";
            }
        }

        rankingText.text = displayText;
    }

    /// <summary>
    /// Convert rank number to string with proper suffix (1st, 2nd, 3rd, etc.)
    /// </summary>
    private string GetRankSuffix(int rank)
    {
        if (rank <= 0) return "--";
        
        // Special cases for 11th, 12th, 13th
        if (rank % 100 >= 11 && rank % 100 <= 13)
        {
            return rank + "th";
        }
        
        // Regular cases
        switch (rank % 10)
        {
            case 1: return rank + "st place";
            case 2: return rank + "nd place";
            case 3: return rank + "rd place";
            default: return rank + "th place";
        }
    }
}
