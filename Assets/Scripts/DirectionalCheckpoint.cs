using UnityEngine;

/// <summary>
/// Directional checkpoint that prevents cheating by ensuring the player
/// travels through it in the correct direction. Place this before the lap trigger.
/// </summary>
public class DirectionalCheckpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [SerializeField] private bool requiresCorrectDirection = true;
    
    [Header("Direction Setup")]
    [SerializeField] private Transform directionMarker;
    [Tooltip("Optional: Point this transform toward the correct direction. If null, uses this object's forward")]
    
    [SerializeField] private DirectionMode mode = DirectionMode.UseForward;
    [Tooltip("UseForward = Blue arrow | UseRight = Red arrow | UseUp = Green arrow | CustomDirection = Set your own | TowardMarker = Point to marker")]
    
    [SerializeField] private Vector3 customDirection = Vector3.forward;
    [Tooltip("Used when mode is 'Custom Direction'")]
    
    [SerializeField][Range(0f, 1f)] private float directionThreshold = 0.3f;
    [Tooltip("How aligned the player must be (0 = any angle, 1 = perfectly aligned). Yellow arrow = waiting, Green arrow = passed")]
    
    public enum DirectionMode
    {
        UseForward,         // Blue arrow in scene view
        UseRight,           // Red arrow in scene view
        UseUp,              // Green arrow in scene view
        CustomDirection,    // Custom vector
        TowardMarker       // Cyan line to marker in scene view
    }
    
    private bool playerPassedThrough = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (requiresCorrectDirection)
            {
                // Check if player is moving in the correct direction
                Rigidbody playerRb = other.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    Vector3 correctDirection = GetCorrectDirection();
                    
                    // Calculate dot product between player velocity and correct direction
                    float directionDot = Vector3.Dot(playerRb.linearVelocity.normalized, correctDirection.normalized);
                    
                    // If dot product > threshold, player is moving in the correct direction
                    if (directionDot > directionThreshold)
                    {
                        playerPassedThrough = true;
                        Debug.Log($"Checkpoint passed! Alignment: {directionDot:F2}");
                    }
                    else
                    {
                        Debug.Log($"Wrong direction! Alignment: {directionDot:F2} (need > {directionThreshold})");
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
    
    private Vector3 GetCorrectDirection()
    {
        switch (mode)
        {
            case DirectionMode.UseForward:
                return transform.forward;
            case DirectionMode.UseRight:
                return transform.right;
            case DirectionMode.UseUp:
                return transform.up;
            case DirectionMode.CustomDirection:
                return customDirection;
            case DirectionMode.TowardMarker:
                if (directionMarker != null)
                    return (directionMarker.position - transform.position).normalized;
                else
                    return transform.forward;
            default:
                return transform.forward;
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
        Vector3 correctDirection = GetCorrectDirection();
        
        // Draw an arrow showing the correct direction
        Gizmos.color = playerPassedThrough ? Color.green : Color.yellow;
        Gizmos.DrawRay(transform.position, correctDirection * 5f);
        Gizmos.DrawSphere(transform.position + correctDirection * 5f, 0.3f);
        
        // Draw axis helpers when using transform axes
        if (mode == DirectionMode.UseForward || mode == DirectionMode.UseRight || mode == DirectionMode.UseUp)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.right * 2f);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.up * 2f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
        
        // Draw line to marker if using TowardMarker mode
        if (mode == DirectionMode.TowardMarker && directionMarker != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, directionMarker.position);
        }
    }
}
