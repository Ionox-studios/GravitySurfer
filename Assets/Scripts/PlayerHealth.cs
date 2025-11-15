using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("UI References")]
    public GameObject deathPanel;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Takes damage and reduces health. Call this from damage sources.
    /// </summary>
    /// <param name="damage">Amount of damage to take</param>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Player took {damage} damage. Current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Heals the player by the specified amount
    /// </summary>
    /// <param name="amount">Amount to heal</param>
    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Player healed {amount}. Current health: {currentHealth}/{maxHealth}");
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Player died!");

        // Show death panel
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        // Pause the game
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Returns the current health value
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Returns the maximum health value
    /// </summary>
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Returns true if player is dead
    /// </summary>
    public bool IsDead()
    {
        return isDead;
    }
}
