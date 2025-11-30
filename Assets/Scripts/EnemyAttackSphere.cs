using UnityEngine;

/// <summary>
/// Attach to the attack sphere visual to detect player hits via trigger collision
/// </summary>
public class EnemyAttackSphere : MonoBehaviour
{
    private System.Action<GameObject> _onPlayerHit;
    private bool _isActive = false;

    public void Initialize(System.Action<GameObject> onPlayerHitCallback)
    {
        _onPlayerHit = onPlayerHitCallback;
    }

    public void SetActive(bool active)
    {
        _isActive = active;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive) return;

        if (other.CompareTag("Player"))
        {
            _onPlayerHit?.Invoke(other.gameObject);
        }
    }
}
