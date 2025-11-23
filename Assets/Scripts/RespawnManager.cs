using UnityEngine;
using System.Collections.Generic;

public class RespawnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SurfaceAttraction surfaceAttraction;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private List<GameObject> roadNodes;
    
    [Header("Respawn Settings")]
    [SerializeField] private float respawnHeightOffset = 5f;
    
    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private bool hasSafePosition = false;
    private bool isRespawning = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    void Start()
    {
        // Auto-find components if not assigned
        if (surfaceAttraction == null) surfaceAttraction = GetComponent<SurfaceAttraction>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        
        // Try finding in parent if still null
        if (surfaceAttraction == null) surfaceAttraction = GetComponentInParent<SurfaceAttraction>();
        if (playerHealth == null) playerHealth = GetComponentInParent<PlayerHealth>();
        
        // Always initialize with current position as a fallback
        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;
        hasSafePosition = true;
    }

    void Update()
    {
        if (surfaceAttraction != null)
        {
            // Update safe position whenever the long raycast from SurfaceAttraction is hitting
            if (surfaceAttraction.IsSurfaceDetected())
            {
                UpdateSafePosition();
                
                // If we were respawning, we are now safely back on the ground
                if (isRespawning)
                {
                    isRespawning = false;
                    CancelInvoke(nameof(ResetRespawnState));
                }
            }
        }
    }

    private void UpdateSafePosition()
    {
        if (roadNodes == null || roadNodes.Count == 0) return;

        Vector3 currentPos = transform.position;
        Vector3 bestPoint = Vector3.zero;
        float minDstSqr = float.MaxValue;
        bool found = false;

        foreach (var node in roadNodes)
        {
            if (node == null) continue;
            
            // Use world position, not local position
            Vector3 nodeWorldPos = node.transform.position;
            float dstSqr = (nodeWorldPos - currentPos).sqrMagnitude;
            
            if (dstSqr < minDstSqr)
            {
                minDstSqr = dstSqr;
                bestPoint = nodeWorldPos;
                found = true;
            }
        }

        if (found)
        {
            // Only update if we moved significantly to avoid jitter
            if (Vector3.SqrMagnitude(bestPoint - (lastSafePosition - Vector3.up * respawnHeightOffset)) > 1f)
            {
                lastSafePosition = bestPoint + Vector3.up * respawnHeightOffset;
                lastSafeRotation = Quaternion.identity; // Always upright rotation
                hasSafePosition = true;
            }
        }
    }

    public void Respawn()
    {
        // Prevent multiple triggers/damage while already handling a respawn
        if (isRespawning) return;
        
        isRespawning = true;
        // Safety timeout: allow respawning again after 3 seconds if we somehow miss the road
        Invoke(nameof(ResetRespawnState), 3f);

        if (playerHealth != null)
        {
            if (hasSafePosition)
            {
                // Pass the transform to PlayerHealth so it can teleport the correct object
                playerHealth.Respawn(lastSafePosition, lastSafeRotation, transform);
            }
            else
            {
                // Fallback with upright rotation
                playerHealth.Respawn(new Vector3(0, 10, 0), Quaternion.identity, transform);
            }
        }
    }

    private void ResetRespawnState()
    {
        isRespawning = false;
    }

    void OnDrawGizmos()
    {
        if (hasSafePosition)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lastSafePosition, 1f);
            Gizmos.DrawLine(lastSafePosition, lastSafePosition - Vector3.up * respawnHeightOffset);
        }
        
        if (roadNodes != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var node in roadNodes)
            {
                if (node != null) Gizmos.DrawWireSphere(node.transform.position, 0.5f);
            }
        }
    }
}
