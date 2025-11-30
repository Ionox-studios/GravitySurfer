using UnityEngine;

/// <summary>
/// Interface for anything that can race (player or AI)
/// </summary>
public interface IRacer
{
    /// <summary>
    /// Current lap number (1-indexed)
    /// </summary>
    int GetCurrentLap();
    
    /// <summary>
    /// Current waypoint/node index on the track
    /// </summary>
    int GetCurrentNodeIndex();
    
    /// <summary>
    /// Total number of waypoints/nodes in the track
    /// </summary>
    int GetTotalNodes();
    
    /// <summary>
    /// Transform of the racer
    /// </summary>
    Transform GetTransform();
    
    /// <summary>
    /// Is this racer still alive/active in the race?
    /// </summary>
    bool IsAlive();
    
    /// <summary>
    /// Name to display in rankings
    /// </summary>
    string GetRacerName();
    
    /// <summary>
    /// Get the position of the next waypoint/node this racer is heading toward
    /// Returns null if no next waypoint available
    /// </summary>
    Vector3? GetNextWaypointPosition();
}
