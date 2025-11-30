using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central manager for handling scene transitions between menu, racing, and cutscenes
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    [Header("Scene Configuration")]
    [Tooltip("List of all racing levels in the game")]
    public List<RacingLevel> racingLevels = new List<RacingLevel>();

    private int currentLevelIndex = -1;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Load a specific racing level by index
    /// </summary>
    public void LoadRacingLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < racingLevels.Count)
        {
            currentLevelIndex = levelIndex;
            
            // Check if there's a cutscene before the race
            string cutsceneBefore = racingLevels[levelIndex].cutsceneBeforeRace;
            
            if (!string.IsNullOrEmpty(cutsceneBefore))
            {
                // Load the intro cutscene first
                SceneManager.LoadScene(cutsceneBefore);
            }
            else
            {
                // Go directly to the race
                SceneManager.LoadScene(racingLevels[levelIndex].racingSceneName);
            }
        }
        else
        {
            Debug.LogError($"Invalid level index: {levelIndex}");
        }
    }

    /// <summary>
    /// Load a specific racing level by name
    /// </summary>
    public void LoadRacingLevel(string sceneName)
    {
        int index = racingLevels.FindIndex(level => level.racingSceneName == sceneName);
        if (index >= 0)
        {
            LoadRacingLevel(index);
        }
        else
        {
            Debug.LogError($"Racing level not found: {sceneName}");
        }
    }

    /// <summary>
    /// Called when player completes a race - transitions to cutscene
    /// </summary>
    public void OnRaceComplete()
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < racingLevels.Count)
        {
            string cutsceneName = racingLevels[currentLevelIndex].cutsceneAfterRace;
            
            if (!string.IsNullOrEmpty(cutsceneName))
            {
                SceneManager.LoadScene(cutsceneName);
            }
            else
            {
                // No cutscene, go to next level or menu
                LoadNextLevel();
            }
        }
    }

    /// <summary>
    /// Called when cutscene ends - loads next level or returns to menu
    /// </summary>
    public void OnCutsceneComplete()
    {
        LoadNextLevel();
    }

    /// <summary>
    /// Load the next racing level in sequence, or return to menu if completed all
    /// </summary>
    private void LoadNextLevel()
    {
        currentLevelIndex++;
        
        if (currentLevelIndex < racingLevels.Count)
        {
            LoadRacingLevel(currentLevelIndex);
        }
        else
        {
            // All levels complete, return to main menu
            LoadMainMenu();
        }
    }

    /// <summary>
    /// Load the main menu scene
    /// </summary>
    public void LoadMainMenu()
    {
        currentLevelIndex = -1;
        SceneManager.LoadScene("Menu");
    }

    /// <summary>
    /// Get the number of racing levels available
    /// </summary>
    public int GetLevelCount()
    {
        return racingLevels.Count;
    }

    /// <summary>
    /// Get racing level data by index
    /// </summary>
    public RacingLevel GetLevel(int index)
    {
        if (index >= 0 && index < racingLevels.Count)
        {
            return racingLevels[index];
        }
        return null;
    }
    
    /// <summary>
    /// Get the current level index
    /// </summary>
    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }
}

[System.Serializable]
public class RacingLevel
{
    public string levelName;              // Display name for UI
    public string cutsceneBeforeRace;     // Scene name for cutscene before race (can be empty)
    public string racingSceneName;        // Scene name for the racing level
    public string cutsceneAfterRace;      // Scene name for cutscene after race (can be empty)
    public Sprite levelThumbnail;         // Optional thumbnail for level select
    public bool isUnlocked = true;        // For progression system (future)
}
