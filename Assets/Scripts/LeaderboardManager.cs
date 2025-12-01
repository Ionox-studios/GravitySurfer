using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages a leaderboard with top 5 scores, persists in memory while GameSceneManager is alive
/// </summary>
[Serializable]
public class LeaderboardEntry
{
    public string playerTag;  // 3-letter tag like "GZ7", "KTN"
    public float timeInSeconds;

    public LeaderboardEntry(string tag, float time)
    {
        playerTag = tag;
        timeInSeconds = time;
    }

    /// <summary>
    /// Format time as MM:SS
    /// </summary>
    public string GetFormattedTime()
    {
        if (timeInSeconds <= 0)
            return "XXXX";
        
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return string.Format("{0}:{1:00}", minutes, seconds);
    }
}

[Serializable]
public class LeaderboardManager
{
    private const int MAX_ENTRIES = 5;
    
    [SerializeField]
    private List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

    public LeaderboardManager(int levelIndex = 0)
    {
        // Initialize with default entries for this level
        InitializeDefaultLeaderboard(levelIndex);
    }

    private void InitializeDefaultLeaderboard(int levelIndex)
    {
        entries.Clear();
        
        // Different default times for each level (you can customize these)
        switch (levelIndex)
        {
            case 0: // Level 1
                entries.Add(new LeaderboardEntry("GZ7", 92f));   // 1:32
                entries.Add(new LeaderboardEntry("ION", 141f));  // 2:21
                entries.Add(new LeaderboardEntry("KTN", 155f));  // 2:35
                entries.Add(new LeaderboardEntry("AAA", -1f));   // XXXX
                entries.Add(new LeaderboardEntry("YKI", -1f));   // XXXX
                break;
            case 1: // Level 2
                entries.Add(new LeaderboardEntry("GZ7", 105f));  // 1:45
                entries.Add(new LeaderboardEntry("KTN", 138f));  // 2:18
                entries.Add(new LeaderboardEntry("ION", 234f));  // 2:42
                entries.Add(new LeaderboardEntry("AAA", -1f));   // XXXX
                entries.Add(new LeaderboardEntry("YKI", -1f));   // XXXX
                break;
            case 2: // Level 3
                entries.Add(new LeaderboardEntry("ION", 118f));  // 1:58
                entries.Add(new LeaderboardEntry("GZ7", 145f));  // 2:25
                entries.Add(new LeaderboardEntry("KTN", 171f));  // 2:51
                entries.Add(new LeaderboardEntry("AAA", -1f));   // XXXX
                entries.Add(new LeaderboardEntry("YKI", -1f));   // XXXX
                break;
            default:
                // Fallback for any other level
                entries.Add(new LeaderboardEntry("AAA", -1f));
                entries.Add(new LeaderboardEntry("BBB", -1f));
                entries.Add(new LeaderboardEntry("CCC", -1f));
                entries.Add(new LeaderboardEntry("DDD", -1f));
                entries.Add(new LeaderboardEntry("EEE", -1f));
                break;
        }
    }

    /// <summary>
    /// Try to add a new time to the leaderboard. Returns true if it made the top 5.
    /// </summary>
    public bool TryAddEntry(string playerTag, float timeInSeconds)
    {
        LeaderboardEntry newEntry = new LeaderboardEntry(playerTag, timeInSeconds);
        
        // Add the new entry
        entries.Add(newEntry);
        
        // Sort: valid times (> 0) first by time ascending, then invalid times (-1) at the end
        entries = entries
            .OrderBy(e => e.timeInSeconds <= 0 ? float.MaxValue : e.timeInSeconds)
            .ToList();
        
        // Keep only top 5
        if (entries.Count > MAX_ENTRIES)
        {
            entries.RemoveRange(MAX_ENTRIES, entries.Count - MAX_ENTRIES);
        }
        
        // Check if the new entry is still in the top 5
        return entries.Contains(newEntry);
    }

    /// <summary>
    /// Get all leaderboard entries (max 5)
    /// </summary>
    public List<LeaderboardEntry> GetEntries()
    {
        return new List<LeaderboardEntry>(entries);
    }

    /// <summary>
    /// Get the rank position (1-5) of a specific time, or -1 if not in top 5
    /// </summary>
    public int GetRankForTime(float timeInSeconds)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (Mathf.Approximately(entries[i].timeInSeconds, timeInSeconds))
            {
                return i + 1; // Return 1-based rank
            }
        }
        return -1;
    }

    /// <summary>
    /// Reset to default leaderboard
    /// </summary>
    public void Reset(int levelIndex = 0)
    {
        InitializeDefaultLeaderboard(levelIndex);
    }
}
