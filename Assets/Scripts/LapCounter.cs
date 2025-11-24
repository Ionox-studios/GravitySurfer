using UnityEngine;

/// <summary>
/// Lap counter trigger that increments laps when player passes through
/// Only counts if the player has passed through the directional checkpoint first
/// </summary>
public class LapCounter : MonoBehaviour
{
    [Header("Checkpoint Reference")]
    [SerializeField] private DirectionalCheckpoint checkpoint;
    [Tooltip("Reference to the directional checkpoint that must be passed before counting a lap")]

    [Header("Lap Settings")]
    [SerializeField] private int totalLaps = 3;
    [Tooltip("Total number of laps to complete the race")]

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Check if checkpoint was passed (prevents backwards cheating)
            if (checkpoint != null && !checkpoint.HasPlayerPassed())
            {
                Debug.Log("Cannot count lap - checkpoint not passed!");
                return;
            }

            // Increment the lap in GameController
            if (GameController.Instance != null)
            {
                GameController.Instance.IncrementLap();
                
                // Reset the checkpoint for the next lap
                if (checkpoint != null)
                {
                    checkpoint.ResetCheckpoint();
                }
            }
            else
            {
                Debug.LogError("GameController instance not found!");
            }
        }
    }

    // Visual helper in editor
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>()?.size ?? Vector3.one);
    }
}
