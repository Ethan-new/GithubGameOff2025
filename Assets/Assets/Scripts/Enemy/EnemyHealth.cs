using UnityEngine;

/// <summary>
/// Component that manages enemy health and damage taking.
/// Attach this to enemy GameObjects to enable them to take damage from weapons.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Death Settings")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float deathDelay = 0f;

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
    /// Event that is called when the enemy takes damage.
    /// </summary>
    public System.Action<float> OnDamageTaken;

    /// <summary>
    /// Event that is called when the enemy dies.
    /// </summary>
    public System.Action OnDeath;

    private void Awake()
    {
        // Initialize health to max if not already set
        if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }
    }

    /// <summary>
    /// Makes the enemy take damage. Returns true if the enemy died from this damage.
    /// </summary>
    /// <param name="damage">The amount of damage to take.</param>
    /// <returns>True if the enemy died, false otherwise.</returns>
    public bool TakeDamage(float damage)
    {
        if (!IsAlive)
            return false;

        // Apply damage
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        // Notify listeners
        OnDamageTaken?.Invoke(damage);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        #endif

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

        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{gameObject.name} healed for {healAmount}. Health: {currentHealth}/{maxHealth}");
        #endif
    }

    /// <summary>
    /// Sets the enemy's health to the maximum value.
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
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
    /// Handles enemy death.
    /// </summary>
    private void Die()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{gameObject.name} died!");
        #endif

        // Award score for kill
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddKillScore();
        }

        // Notify listeners
        OnDeath?.Invoke();

        // Destroy or disable the enemy
        if (destroyOnDeath)
        {
            if (deathDelay > 0f)
            {
                Destroy(gameObject, deathDelay);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            // Just disable the GameObject instead of destroying it
            gameObject.SetActive(false);
        }
    }

    private void OnValidate()
    {
        // Ensure health values are valid
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }
}
