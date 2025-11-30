using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages race rankings for all racers (player + enemies)
/// Calculates positions based on lap number and track progress
/// </summary>
public class RaceRankingSystem : MonoBehaviour
{
    public static RaceRankingSystem Instance;

    [Header("Racer References")]
    [SerializeField] private List<GameObject> racerObjects;
    [Tooltip("Add all racer GameObjects here (player + enemies)")]
    
    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [Tooltip("How often to recalculate rankings (in seconds)")]
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private List<IRacer> racers = new List<IRacer>();
    private Dictionary<IRacer, int> currentRankings = new Dictionary<IRacer, int>();
    private float updateTimer = 0f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        InitializeRacers();
    }

    void Update()
    {
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateRankings();
        }
    }

    /// <summary>
    /// Initialize all racers from the racer objects list
    /// </summary>
    private void InitializeRacers()
    {
        racers.Clear();
        
        if (racerObjects == null || racerObjects.Count == 0)
        {
            Debug.LogWarning("RaceRankingSystem: No racer objects assigned!");
            return;
        }

        foreach (var obj in racerObjects)
        {
            if (obj == null) continue;
            
            IRacer racer = obj.GetComponent<IRacer>();
            if (racer != null)
            {
                racers.Add(racer);
                Debug.Log($"Registered racer: {racer.GetRacerName()}");
            }
            else
            {
                Debug.LogWarning($"GameObject {obj.name} does not implement IRacer interface!");
            }
        }

        Debug.Log($"RaceRankingSystem initialized with {racers.Count} racers");
    }

    /// <summary>
    /// Update rankings for all racers
    /// </summary>
    private void UpdateRankings()
    {
        // Filter out dead racers and sort by race progress
        var aliveRacers = racers.Where(r => r.IsAlive()).ToList();
        
        if (aliveRacers.Count == 0) return;

        // Sort racers by progress (higher is better)
        aliveRacers.Sort((a, b) => CompareRaceProgress(b, a));

        // Assign rankings
        currentRankings.Clear();
        for (int i = 0; i < aliveRacers.Count; i++)
        {
            currentRankings[aliveRacers[i]] = i + 1; // 1st, 2nd, 3rd, etc.
        }

        if (showDebugInfo)
        {
            PrintRankings();
        }
    }

    /// <summary>
    /// Compare race progress between two racers
    /// Returns positive if racer A is ahead, negative if racer B is ahead
    /// </summary>
    private int CompareRaceProgress(IRacer a, IRacer b)
    {
        // First compare by lap number (higher lap = ahead)
        int lapComparison = a.GetCurrentLap().CompareTo(b.GetCurrentLap());
        if (lapComparison != 0)
            return lapComparison;

        // If on same lap, compare by node index (higher node = ahead)
        int nodeComparison = a.GetCurrentNodeIndex().CompareTo(b.GetCurrentNodeIndex());
        if (nodeComparison != 0)
            return nodeComparison;

        // If on same node, compare by distance to next node (closer to next = ahead)
        float distanceA = GetDistanceToNextNode(a);
        float distanceB = GetDistanceToNextNode(b);
        
        // Closer distance means further ahead, so reverse the comparison
        return distanceB.CompareTo(distanceA);
    }

    /// <summary>
    /// Get distance from racer to their next waypoint
    /// </summary>
    private float GetDistanceToNextNode(IRacer racer)
    {
        if (racer == null) return float.MaxValue;
        
        Vector3? nextWaypoint = racer.GetNextWaypointPosition();
        if (!nextWaypoint.HasValue)
            return float.MaxValue;
        
        Transform racerTransform = racer.GetTransform();
        if (racerTransform == null)
            return float.MaxValue;
        
        // Calculate actual distance to next waypoint
        return Vector3.Distance(racerTransform.position, nextWaypoint.Value);
    }

    /// <summary>
    /// Get the current rank of a specific racer
    /// </summary>
    /// <param name="racer">The racer to check</param>
    /// <returns>Current rank (1 = 1st place), or -1 if not found/dead</returns>
    public int GetRacerRank(IRacer racer)
    {
        if (racer == null || !racer.IsAlive())
            return -1;

        if (currentRankings.TryGetValue(racer, out int rank))
            return rank;

        return -1;
    }

    /// <summary>
    /// Get the current rank of a specific racer by GameObject
    /// </summary>
    public int GetRacerRank(GameObject racerObject)
    {
        if (racerObject == null) return -1;
        
        IRacer racer = racerObject.GetComponent<IRacer>();
        return GetRacerRank(racer);
    }

    /// <summary>
    /// Get all current rankings as a sorted list
    /// </summary>
    /// <returns>List of (racer, rank) tuples sorted by rank</returns>
    public List<(IRacer racer, int rank)> GetAllRankings()
    {
        var rankings = new List<(IRacer racer, int rank)>();
        
        foreach (var kvp in currentRankings)
        {
            rankings.Add((kvp.Key, kvp.Value));
        }
        
        rankings.Sort((a, b) => a.rank.CompareTo(b.rank));
        return rankings;
    }

    /// <summary>
    /// Get the total number of alive racers
    /// </summary>
    public int GetAliveRacerCount()
    {
        return racers.Count(r => r.IsAlive());
    }

    /// <summary>
    /// Print current rankings to console (debug)
    /// </summary>
    private void PrintRankings()
    {
        Debug.Log("=== RACE RANKINGS ===");
        var rankings = GetAllRankings();
        foreach (var (racer, rank) in rankings)
        {
            Debug.Log($"{rank}. {racer.GetRacerName()} - Lap {racer.GetCurrentLap()}, Node {racer.GetCurrentNodeIndex()}");
        }
        Debug.Log("====================");
    }

    /// <summary>
    /// Manually add a racer to the system (useful for dynamic spawning)
    /// </summary>
    public void RegisterRacer(IRacer racer)
    {
        if (racer == null) return;
        
        if (!racers.Contains(racer))
        {
            racers.Add(racer);
            Debug.Log($"Registered new racer: {racer.GetRacerName()}");
        }
    }

    /// <summary>
    /// Manually remove a racer from the system
    /// </summary>
    public void UnregisterRacer(IRacer racer)
    {
        if (racer == null) return;
        
        if (racers.Contains(racer))
        {
            racers.Remove(racer);
            currentRankings.Remove(racer);
            Debug.Log($"Unregistered racer: {racer.GetRacerName()}");
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo || currentRankings == null) return;

        // Draw ranking numbers above each racer
        foreach (var kvp in currentRankings)
        {
            IRacer racer = kvp.Key;
            int rank = kvp.Value;
            
            if (racer != null && racer.IsAlive())
            {
                Vector3 pos = racer.GetTransform().position + Vector3.up * 3f;
                UnityEngine.GUIStyle style = new UnityEngine.GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 20;
                
#if UNITY_EDITOR
                UnityEditor.Handles.Label(pos, $"{rank}", style);
#endif
            }
        }
    }
}
