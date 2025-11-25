using UnityEngine;

/// <summary>
/// Attach to the attack object to detect collisions with the player
/// </summary>
public class EnemyAttackCollider : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] public float pushForce = 10f; // Force of the vertical push
    private float _lastHitTime;
    private float _hitCooldown = 0.5f; // Prevent multiple hits in one swing
    private System.Action _onHitCallback; // Callback when player is hit

    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit the player
        if (other.CompareTag("Player") && Time.time - _lastHitTime > _hitCooldown)
        {
            // Try to get PlayerHealth component
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Push direction is horizontal only (X and Z, no Y component)
                Vector3 pushDirection = (other.transform.position - transform.position);
                pushDirection.y = 0f; // Remove vertical component
                pushDirection = pushDirection.normalized * pushForce;
                
                playerHealth.TakeDamage(damage, pushDirection);
                _lastHitTime = Time.time;
                Debug.Log($"Enemy attack hit player for {damage} damage!");
                
                // Notify enemy that hit occurred
                _onHitCallback?.Invoke();
            }
        }
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }

    public void SetPushForce(float newPushForce)
    {
        pushForce = newPushForce;
    }

    public void SetOnHitCallback(System.Action callback)
    {
        _onHitCallback = callback;
    }
}
