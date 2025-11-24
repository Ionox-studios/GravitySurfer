using UnityEngine;

/// <summary>
/// Directional checkpoint that prevents cheating by ensuring the player
/// travels through it in the correct direction. Place this before the lap trigger.
/// </summary>
public class DirectionalCheckpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [SerializeField] private bool requiresCorrectDirection = true;
    [Tooltip("The forward direction of this checkpoint is the 'correct' direction")]
    
    private bool playerPassedThrough = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (requiresCorrectDirection)
            {
                // Check if player is moving in the correct direction (same as checkpoint's forward)
                Rigidbody playerRb = other.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    // Calculate dot product between player velocity and checkpoint forward
                    float directionDot = Vector3.Dot(playerRb.linearVelocity.normalized, transform.forward);
                    
                    // If dot product > 0, player is moving in the correct direction
                    if (directionDot > 0.3f) // Small threshold to avoid edge cases
                    {
                        playerPassedThrough = true;
                        Debug.Log("Checkpoint passed in correct direction!");
                    }
                    else
                    {
                        Debug.Log("Wrong direction through checkpoint!");
                        playerPassedThrough = false;
                    }
                }
            }
            else
            {
                playerPassedThrough = true;
            }
        }
    }

    /// <summary>
    /// Check if the player has passed through this checkpoint correctly
    /// </summary>
    public bool HasPlayerPassed()
    {
        return playerPassedThrough;
    }

    /// <summary>
    /// Reset the checkpoint state (call this when starting a new lap)
    /// </summary>
    public void ResetCheckpoint()
    {
        playerPassedThrough = false;
    }

    // Visual helper in editor
    private void OnDrawGizmos()
    {
        // Draw an arrow showing the correct direction
        Gizmos.color = playerPassedThrough ? Color.green : Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);
        Gizmos.DrawSphere(transform.position + transform.forward * 5f, 0.3f);
    }
}
