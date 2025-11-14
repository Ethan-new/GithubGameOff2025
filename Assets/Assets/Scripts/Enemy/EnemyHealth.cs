using UnityEngine;
using System;

/// <summary>
/// Health system for enemies. Handles taking damage and death.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Death Settings")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 2f;

    [Header("Events")]
    [SerializeField] private bool enableDebugLogs = true;

    /// <summary>
    /// Event that is called when the enemy takes damage.
    /// Parameters: damage amount, current health, max health
    /// </summary>
    public event Action<float, float, float> OnDamageTaken;

    /// <summary>
    /// Event that is called when the enemy dies.
    /// </summary>
    public event Action OnDeath;

    /// <summary>
    /// The current health of the enemy.
    /// </summary>
    public float CurrentHealth => currentHealth;

    /// <summary>
    /// The maximum health of the enemy.
    /// </summary>
    public float MaxHealth => maxHealth;

    /// <summary>
    /// Whether the enemy is currently alive.
    /// </summary>
    public bool IsAlive => currentHealth > 0f;

    /// <summary>
    /// The health percentage (0-1).
    /// </summary>
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;

    private void Awake()
    {
        // Initialize health to max if not set
        if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }
    }

    /// <summary>
    /// Makes the enemy take damage.
    /// </summary>
    /// <param name="damage">The amount of damage to take.</param>
    /// <returns>True if the enemy died from this damage, false otherwise.</returns>
    public bool TakeDamage(float damage)
    {
        if (!IsAlive)
            return false;

        if (damage <= 0f)
            return false;

        // Apply damage
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth); // Clamp to 0

        if (enableDebugLogs)
        {
            Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        }

        // Invoke damage event
        OnDamageTaken?.Invoke(damage, currentHealth, maxHealth);

        // Check if enemy died
        if (currentHealth <= 0f)
        {
            Die();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Heals the enemy by the specified amount.
    /// </summary>
    /// <param name="healAmount">The amount to heal.</param>
    public void Heal(float healAmount)
    {
        if (!IsAlive)
            return;

        if (healAmount <= 0f)
            return;

        currentHealth += healAmount;
        currentHealth = Mathf.Min(maxHealth, currentHealth); // Clamp to max

        if (enableDebugLogs)
        {
            Debug.Log($"{gameObject.name} healed for {healAmount}. Health: {currentHealth}/{maxHealth}");
        }
    }

    /// <summary>
    /// Sets the enemy's health to a specific value.
    /// </summary>
    /// <param name="health">The health value to set.</param>
    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
    }

    /// <summary>
    /// Sets the maximum health and optionally adjusts current health proportionally.
    /// </summary>
    /// <param name="newMaxHealth">The new maximum health.</param>
    /// <param name="adjustCurrentHealth">If true, adjusts current health proportionally.</param>
    public void SetMaxHealth(float newMaxHealth, bool adjustCurrentHealth = false)
    {
        if (adjustCurrentHealth && maxHealth > 0f)
        {
            float healthPercentage = currentHealth / maxHealth;
            currentHealth = newMaxHealth * healthPercentage;
        }

        maxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    /// <summary>
    /// Kills the enemy immediately.
    /// </summary>
    public void Kill()
    {
        if (!IsAlive)
            return;

        currentHealth = 0f;
        Die();
    }

    /// <summary>
    /// Handles the enemy's death.
    /// </summary>
    private void Die()
    {
        if (!IsAlive)
            return;

        if (enableDebugLogs)
        {
            Debug.Log($"{gameObject.name} has died!");
        }

        // Invoke death event
        OnDeath?.Invoke();

        // Disable movement if it exists
        EnemyMovement movement = GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        // Destroy or disable the enemy
        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Resets the enemy's health to maximum.
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    private void OnValidate()
    {
        // Ensure health values are valid in the editor
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }
}

