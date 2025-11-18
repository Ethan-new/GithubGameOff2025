using UnityEngine;

/// <summary>
/// Component that manages player health, damage, and death/respawn.
/// Attach this to the Player GameObject along with PlayerController.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool initializeOnStart = true; // Initialize health to max on Start

    [Header("Health Regeneration")]
    [SerializeField] private bool enableRegeneration = false;
    [SerializeField] private float regenDelay = 5f; // Time after taking damage before regen starts
    [SerializeField] private float regenRate = 5f; // Health per second
    [SerializeField] private float regenAmount = 1f; // Health per tick
    [SerializeField] private float regenInterval = 0.2f; // Time between regen ticks

    [Header("Death Settings")]
    [SerializeField] private bool respawnOnDeath = true;
    [SerializeField] private float respawnDelay = 3f; // Time before respawning
    [SerializeField] private Transform respawnPoint; // Optional respawn point (uses starting position if null)
    [SerializeField] private bool resetHealthOnRespawn = true;

    [Header("Invincibility")]
    [SerializeField] private bool invincibleOnSpawn = true;
    [SerializeField] private float invincibilityDuration = 2f; // How long invincibility lasts after spawn/respawn

    [Header("Visual/Audio Feedback")]
    [SerializeField] private GameObject deathEffect; // Particle effect to spawn on death
    [SerializeField] private AudioClip damageSound; // Sound played when taking damage
    [SerializeField] private AudioClip deathSound; // Sound played on death
    [SerializeField] private float damageSoundVolume = 1f;
    [SerializeField] private float deathSoundVolume = 1f;

    private PlayerController playerController;
    private PlayerInventory playerInventory;
    private float timeSinceLastDamage = 0f;
    private float timeSinceLastRegen = 0f;
    private bool isDead = false;
    private bool isInvincible = false;
    private float invincibilityTimer = 0f;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private AudioSource audioSource;

    /// <summary>
    /// The current health of the player.
    /// </summary>
    public float CurrentHealth => currentHealth;

    /// <summary>
    /// The maximum health of the player (may be modified by augments).
    /// </summary>
    public float MaxHealth => GetEffectiveMaxHealth();

    /// <summary>
    /// The base maximum health (before augments).
    /// </summary>
    public float BaseMaxHealth => maxHealth;

    /// <summary>
    /// Whether the player is currently alive.
    /// </summary>
    public bool IsAlive => !isDead && currentHealth > 0f;

    /// <summary>
    /// Whether the player is currently invincible.
    /// </summary>
    public bool IsInvincible => isInvincible;

    /// <summary>
    /// Event that is called when the player takes damage.
    /// Parameters: damage amount, new health, max health
    /// </summary>
    public System.Action<float, float, float> OnDamageTaken;

    /// <summary>
    /// Event that is called when the player is healed.
    /// Parameters: heal amount, new health, max health
    /// </summary>
    public System.Action<float, float, float> OnHealed;

    /// <summary>
    /// Event that is called when the player dies.
    /// </summary>
    public System.Action OnDeath;

    /// <summary>
    /// Event that is called when the player respawns.
    /// </summary>
    public System.Action OnRespawn;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerHealth requires a PlayerController component!", this);
        }

        // Find PlayerInventory
        playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory == null)
        {
            playerInventory = GetComponentInChildren<PlayerInventory>();
        }

        // Store starting position/rotation for respawn
        startingPosition = transform.position;
        startingRotation = transform.rotation;

        // Initialize health
        if (initializeOnStart)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        // Create audio source if needed
        if (damageSound != null || deathSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }

        // Set invincibility on spawn if enabled
        if (invincibleOnSpawn)
        {
            SetInvincible(invincibilityDuration);
        }
    }

    private void Start()
    {
        // Ensure health is within bounds
        float effectiveMaxHealth = GetEffectiveMaxHealth();
        currentHealth = Mathf.Clamp(currentHealth, 0f, effectiveMaxHealth);
    }

    private void Update()
    {
        // Update invincibility timer
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
            }
        }

        // Update time since last damage
        if (!isDead)
        {
            timeSinceLastDamage += Time.deltaTime;
        }

        // Handle health regeneration
        if (enableRegeneration && !isDead && IsAlive && currentHealth < GetEffectiveMaxHealth())
        {
            if (timeSinceLastDamage >= regenDelay)
            {
                timeSinceLastRegen += Time.deltaTime;
                if (timeSinceLastRegen >= regenInterval)
                {
                    Heal(regenAmount);
                    timeSinceLastRegen = 0f;
                }
            }
        }
    }

    /// <summary>
    /// Gets the effective max health (base health * augment multipliers).
    /// </summary>
    private float GetEffectiveMaxHealth()
    {
        if (playerInventory == null)
            return maxHealth;

        float healthMultiplier = playerInventory.GetPlayerStatMultiplier(augment => augment.HealthMultiplier);
        return maxHealth * healthMultiplier;
    }

    /// <summary>
    /// Gets the effective damage resistance from augments.
    /// </summary>
    private float GetEffectiveDamageResistance()
    {
        if (playerInventory == null)
            return 0f;

        // Get total damage resistance from all augments (stacked additively)
        float totalResistance = 0f;
        var augments = playerInventory.GetAllPlayerAugments();
        foreach (var kvp in augments)
        {
            int stackCount = kvp.Value;
            totalResistance += kvp.Key.DamageResistance * stackCount;
        }

        // Clamp between 0 and 1 (0% to 100% resistance)
        return Mathf.Clamp01(totalResistance);
    }

    /// <summary>
    /// Makes the player take damage. Returns true if the player died from this damage.
    /// </summary>
    /// <param name="damage">The amount of damage to take.</param>
    /// <returns>True if the player died, false otherwise.</returns>
    public bool TakeDamage(float damage)
    {
        if (!IsAlive || isInvincible)
            return false;

        if (damage <= 0f)
            return false;

        // Apply damage resistance from augments
        float damageResistance = GetEffectiveDamageResistance();
        float actualDamage = damage * (1f - damageResistance);

        // Apply damage
        currentHealth = Mathf.Max(0f, currentHealth - actualDamage);
        timeSinceLastDamage = 0f; // Reset regen timer

        // Play damage sound
        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound, damageSoundVolume);
        }

        // Notify listeners
        float effectiveMaxHealth = GetEffectiveMaxHealth();
        OnDamageTaken?.Invoke(actualDamage, currentHealth, effectiveMaxHealth);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Player took {actualDamage} damage (resisted {damage - actualDamage}). Health: {currentHealth}/{effectiveMaxHealth}");
        #endif

        // Check if player died
        if (currentHealth <= 0f)
        {
            Die();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Heals the player by the specified amount.
    /// </summary>
    /// <param name="healAmount">The amount to heal.</param>
    public void Heal(float healAmount)
    {
        if (!IsAlive || healAmount <= 0f)
            return;

        float effectiveMaxHealth = GetEffectiveMaxHealth();
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(effectiveMaxHealth, currentHealth + healAmount);

        // Only notify if health actually changed
        if (currentHealth != oldHealth)
        {
            OnHealed?.Invoke(healAmount, currentHealth, effectiveMaxHealth);

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"Player healed for {healAmount}. Health: {currentHealth}/{effectiveMaxHealth}");
            #endif
        }
    }

    /// <summary>
    /// Sets the player's health to the maximum value.
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = GetEffectiveMaxHealth();
        OnHealed?.Invoke(0f, currentHealth, GetEffectiveMaxHealth());
    }

    /// <summary>
    /// Kills the player immediately.
    /// </summary>
    public void Kill()
    {
        if (!IsAlive)
            return;

        currentHealth = 0f;
        Die();
    }

    /// <summary>
    /// Sets the player to be invincible for a duration.
    /// </summary>
    /// <param name="duration">How long to be invincible.</param>
    public void SetInvincible(float duration)
    {
        isInvincible = true;
        invincibilityTimer = duration;
    }

    /// <summary>
    /// Handles player death.
    /// </summary>
    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentHealth = 0f;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Player died!");
        #endif

        // Play death sound
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound, deathSoundVolume);
        }

        // Spawn death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Disable player controller
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Notify listeners
        OnDeath?.Invoke();

        // Handle respawn
        if (respawnOnDeath)
        {
            Invoke(nameof(Respawn), respawnDelay);
        }
    }

    /// <summary>
    /// Respawns the player at the respawn point or starting position.
    /// </summary>
    private void Respawn()
    {
        isDead = false;

        // Reset position
        Vector3 respawnPos = respawnPoint != null ? respawnPoint.position : startingPosition;
        transform.position = respawnPos;
        transform.rotation = startingRotation;

        // Reset health
        if (resetHealthOnRespawn)
        {
            currentHealth = GetEffectiveMaxHealth();
        }
        else
        {
            currentHealth = Mathf.Max(1f, currentHealth); // At least 1 HP
        }

        // Reset timers
        timeSinceLastDamage = 0f;
        timeSinceLastRegen = 0f;

        // Set invincibility
        if (invincibleOnSpawn)
        {
            SetInvincible(invincibilityDuration);
        }

        // Re-enable player controller
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // Notify listeners
        OnRespawn?.Invoke();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Player respawned at {respawnPos}. Health: {currentHealth}/{GetEffectiveMaxHealth()}");
        #endif
    }

    private void OnValidate()
    {
        // Ensure health values are valid
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        regenRate = Mathf.Max(0f, regenRate);
        regenAmount = Mathf.Max(0f, regenAmount);
        regenInterval = Mathf.Max(0.01f, regenInterval);
    }
}

