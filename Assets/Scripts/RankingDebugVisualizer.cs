using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Debug visualizer for race rankings - shows rank above each racer in scene view
/// </summary>
public class RankingDebugVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] private bool showRankNumbers = true;
    [Tooltip("Show rank numbers above racers in scene view")]
    
    [SerializeField] private bool showProgressInfo = true;
    [Tooltip("Show lap and node info below rank")]
    
    [SerializeField] private float heightOffset = 3f;
    [Tooltip("How far above the racer to show the label")]
    
    [SerializeField] private Color firstPlaceColor = Color.yellow;
    [SerializeField] private Color secondPlaceColor = Color.cyan;
    [SerializeField] private Color thirdPlaceColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color otherPlaceColor = Color.white;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showRankNumbers) return;
        if (RaceRankingSystem.Instance == null) return;

        var rankings = RaceRankingSystem.Instance.GetAllRankings();
        
        foreach (var (racer, rank) in rankings)
        {
            if (racer == null || !racer.IsAlive()) continue;
            
            Transform racerTransform = racer.GetTransform();
            if (racerTransform == null) continue;

            // Determine color based on rank
            Color rankColor = otherPlaceColor;
            if (rank == 1) rankColor = firstPlaceColor;
            else if (rank == 2) rankColor = secondPlaceColor;
            else if (rank == 3) rankColor = thirdPlaceColor;

            // Position for the label
            Vector3 labelPos = racerTransform.position + Vector3.up * heightOffset;

            // Create label text
            string labelText = $"{GetRankSuffix(rank)}";
            
            if (showProgressInfo)
            {
                labelText += $"\nLap {racer.GetCurrentLap()} | Node {racer.GetCurrentNodeIndex()}";
            }

            // Draw the label
            GUIStyle style = new GUIStyle();
            style.normal.textColor = rankColor;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            Handles.Label(labelPos, labelText, style);

            // Draw a line from racer to label
            Gizmos.color = rankColor;
            Gizmos.DrawLine(racerTransform.position + Vector3.up, labelPos);
        }
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
            case 1: return rank + "st";
            case 2: return rank + "nd";
            case 3: return rank + "rd";
            default: return rank + "th";
        }
    }
#endif
}
