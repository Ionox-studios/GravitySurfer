using UnityEngine;

/// <summary>
/// Attach to the attack object to detect collisions with the player
/// </summary>
public class EnemyAttackCollider : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    private float _lastHitTime;
    private float _hitCooldown = 0.5f; // Prevent multiple hits in one swing

    private void OnTriggerEnter(Collider other)
    {
        // Check if we hit the player
        if (other.CompareTag("Player") && Time.time - _lastHitTime > _hitCooldown)
        {
            // Try to get PlayerHealth component
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                _lastHitTime = Time.time;
                Debug.Log($"Enemy attack hit player for {damage} damage!");
            }
        }
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
}
