using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Player racer component - implements IRacer for the player
/// Tracks player's position in the race using lap data from GameController
/// and track progress from RespawnManager's road nodes
/// </summary>
public class PlayerRacer : MonoBehaviour, IRacer
{
    [Header("Racer Info")]
    [SerializeField] private string racerName = "Player";
    [Tooltip("Name to display in rankings")]
    
    [Header("References")]
    [SerializeField] private RespawnManager respawnManager;
    [Tooltip("Reference to get road node tracking")]
    
    private int currentNodeIndex = 0;
    private List<GameObject> roadNodes;

    void Start()
    {
        // Auto-find RespawnManager if not assigned
        if (respawnManager == null)
        {
            respawnManager = GetComponent<RespawnManager>();
        }
        
        // Get road nodes from RespawnManager using reflection
        if (respawnManager != null)
        {
            FieldInfo field = typeof(RespawnManager).GetField("roadNodes", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                roadNodes = field.GetValue(respawnManager) as List<GameObject>;
            }
        }
        
        if (roadNodes == null)
        {
            Debug.LogWarning("PlayerRacer: Could not get road nodes from RespawnManager!");
        }
        
        // Register with ranking system
        if (RaceRankingSystem.Instance != null)
        {
            RaceRankingSystem.Instance.RegisterRacer(this);
        }
    }

    void Update()
    {
        UpdateCurrentNode();
    }

    /// <summary>
    /// Update which node the player is closest to
    /// </summary>
    private void UpdateCurrentNode()
    {
        if (roadNodes == null || roadNodes.Count == 0) return;

        Vector3 currentPos = transform.position;
        float minDistanceSqr = float.MaxValue;
        int closestIndex = currentNodeIndex;

        // Find the closest node
        for (int i = 0; i < roadNodes.Count; i++)
        {
            if (roadNodes[i] == null) continue;
            
            Vector3 nodePos = roadNodes[i].transform.position;
            float distanceSqr = (nodePos - currentPos).sqrMagnitude;
            
            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
                closestIndex = i;
            }
        }

        currentNodeIndex = closestIndex;
    }

    // ============= IRacer Interface Implementation =============
    
    /// <summary>
    /// Get current lap from GameController
    /// </summary>
    public int GetCurrentLap()
    {
        if (GameController.Instance != null)
        {
            return GameController.Instance.GetCurrentLap();
        }
        return 1;
    }
    
    /// <summary>
    /// Get current node index
    /// </summary>
    public int GetCurrentNodeIndex()
    {
        return currentNodeIndex;
    }
    
    /// <summary>
    /// Get total number of nodes
    /// </summary>
    public int GetTotalNodes()
    {
        return roadNodes != null ? roadNodes.Count : 0;
    }
    
    /// <summary>
    /// Get player transform
    /// </summary>
    public Transform GetTransform()
    {
        return transform;
    }
    
    /// <summary>
    /// Check if player is still alive
    /// </summary>
    public bool IsAlive()
    {
        // Check if player has health component
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            return health.GetCurrentHealth() > 0f;
        }
        
        // Default to alive if no health component
        return true;
    }
    
    /// <summary>
    /// Get racer name
    /// </summary>
    public string GetRacerName()
    {
        return racerName;
    }
    
    /// <summary>
    /// Get the position of the next road node
    /// </summary>
    public Vector3? GetNextWaypointPosition()
    {
        if (roadNodes == null || roadNodes.Count == 0)
            return null;
        
        // Get next node index (wrap around if at end)
        int nextIndex = (currentNodeIndex + 1) % roadNodes.Count;
        
        if (roadNodes[nextIndex] != null)
        {
            return roadNodes[nextIndex].transform.position;
        }
        
        return null;
    }

    void OnDestroy()
    {
        // Unregister from ranking system
        if (RaceRankingSystem.Instance != null)
        {
            RaceRankingSystem.Instance.UnregisterRacer(this);
        }
    }
}
