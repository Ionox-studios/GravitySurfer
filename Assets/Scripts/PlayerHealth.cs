using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private float fallDamage = 20f; // Damage to take when falling off map

    [Header("UI References")]
    public GameObject deathPanel;

    public event Action<float, float> OnHealthChanged;

    private bool isDead = false;
    public Animator animator; // AL, if i want to add effects: public Animator SlashEffect;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Takes damage and reduces health. Call this from damage sources.
    /// </summary>
    /// <param name="damage">Amount of damage to take</param>
    /// <param name="pushDirection">Optional direction to push the player (will be applied as force)</param>
    public void TakeDamage(float damage, Vector3? pushDirection = null)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"Player took {damage} damage. Current health: {currentHealth}/{maxHealth}");

        if (animator != null) // AL
            animator.SetTrigger("Stun"); // AL
    //      SlashEffect.SetTrigger("Kick"); // AL

        // Apply push force if provided
        if (pushDirection.HasValue)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(pushDirection.Value, ForceMode.Impulse);
                Debug.Log($"Applied push force: {pushDirection.Value}");
            }
        }

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

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

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

    public void Respawn(Vector3 respawnPosition, Quaternion respawnRotation, Transform playerTransform)
    {
        TakeDamage(fallDamage);
        if (currentHealth > 0)
        {
            // Get the rigidbody first
            Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
            
            if (rb != null)
            {
                // CRITICAL: Reset physics FIRST
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                
                // Use Rigidbody.MovePosition/MoveRotation for physics-safe teleportation
                rb.MovePosition(respawnPosition);
                rb.MoveRotation(respawnRotation);
            }
            else
            {
                // Fallback if no rigidbody (shouldn't happen)
                playerTransform.position = respawnPosition;
                playerTransform.rotation = respawnRotation;
            }
        }
    }
}
