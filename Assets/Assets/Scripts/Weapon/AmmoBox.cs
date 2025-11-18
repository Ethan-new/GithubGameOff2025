using UnityEngine;

/// <summary>
/// Ammo box pickup that adds ammo to the player's inventory when touched.
/// Attach this to a GameObject with a Collider (set as Trigger) and optionally a visual mesh.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AmmoBox : MonoBehaviour
{
    [Header("Ammo Settings")]
    [SerializeField] private AmmoType ammoType; // Type of ammo this box provides
    [SerializeField] private int ammoAmount = 250; // Amount of ammo to give (will be set to this amount, not added)
    [SerializeField] private bool setToAmount = true; // If true, sets ammo to this amount. If false, adds this amount.

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRadius = 2f; // Radius for pickup detection (if using overlap sphere)
    [SerializeField] private bool useTrigger = true; // Use OnTriggerEnter (requires trigger collider)
    [SerializeField] private bool destroyOnPickup = true; // Destroy the ammo box after pickup
    [SerializeField] private float respawnTime = 0f; // Time before respawning (0 = don't respawn, only works if destroyOnPickup is false)

    [Header("Visual Effects")]
    [SerializeField] private bool canRotate = true;
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private bool canBob = true;
    [SerializeField] private float bobSpeed = 1f;
    [SerializeField] private float bobAmount = 0.1f;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private float pickupSoundVolume = 1f;

    [Header("Particle Effects")]
    [SerializeField] private GameObject pickupEffect; // Particle effect to spawn on pickup

    private Vector3 startPosition;
    private float bobOffset = 0f;
    private bool isPickedUp = false;
    private float respawnTimer = 0f;
    private AudioSource audioSource;
    private Renderer[] renderers;
    private Collider boxCollider;

    private void Awake()
    {
        startPosition = transform.position;
        boxCollider = GetComponent<Collider>();

        // Ensure collider is set up correctly
        if (useTrigger && boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }
        else if (!useTrigger && boxCollider != null)
        {
            boxCollider.isTrigger = false;
        }

        // Get renderers for hiding/showing during respawn
        renderers = GetComponentsInChildren<Renderer>();

        // Create audio source if needed
        if (pickupSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.5f; // 3D sound
            }
        }
    }

    private void Update()
    {
        // Visual effects
        if (!isPickedUp)
        {
            if (canRotate)
            {
                transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);
            }

            if (canBob)
            {
                bobOffset += bobSpeed * Time.deltaTime;
                Vector3 bobPosition = startPosition;
                bobPosition.y += Mathf.Sin(bobOffset) * bobAmount;
                transform.position = bobPosition;
            }
        }

        // Handle respawn timer
        if (isPickedUp && respawnTime > 0f && !destroyOnPickup)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                Respawn();
            }
        }

        // Manual pickup check (if not using trigger)
        if (!useTrigger && !isPickedUp)
        {
            CheckForPlayerPickup();
        }
    }

    /// <summary>
    /// Checks for player pickup using overlap sphere (for non-trigger colliders).
    /// </summary>
    private void CheckForPlayerPickup()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius);
        foreach (Collider col in colliders)
        {
            PlayerController player = col.GetComponent<PlayerController>();
            if (player != null)
            {
                TryPickup(player);
                break;
            }
        }
    }

    /// <summary>
    /// Called when a collider enters the trigger.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!useTrigger || isPickedUp)
            return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            TryPickup(player);
        }
    }

    /// <summary>
    /// Attempts to give ammo to the player.
    /// </summary>
    private void TryPickup(PlayerController player)
    {
        if (ammoType == null)
        {
            Debug.LogWarning($"AmmoBox on {gameObject.name} has no ammo type assigned!", this);
            return;
        }

        // Try multiple ways to find PlayerInventory
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = player.GetComponentInChildren<PlayerInventory>();
        }
        if (inventory == null)
        {
            inventory = player.GetComponentInParent<PlayerInventory>();
        }
        if (inventory == null)
        {
            // Last resort: search the entire scene
            inventory = FindFirstObjectByType<PlayerInventory>();
        }

        if (inventory == null)
        {
            Debug.LogError($"AmmoBox: Could not find PlayerInventory component! Please add a PlayerInventory component to the Player GameObject ({player.gameObject.name}) or one of its children.", this);
            return;
        }

        // Give ammo to player
        if (setToAmount)
        {
            // Set ammo to the specified amount
            int currentAmmo = inventory.GetAmmoCount(ammoType);
            int ammoToAdd = ammoAmount - currentAmmo;
            if (ammoToAdd > 0)
            {
                inventory.AddAmmo(ammoType, ammoToAdd);
            }
        }
        else
        {
            // Add the specified amount
            inventory.AddAmmo(ammoType, ammoAmount);
        }

        // Play pickup sound
        PlayPickupSound();

        // Spawn pickup effect
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }

        // Handle pickup
        isPickedUp = true;

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            // Hide the ammo box
            SetVisible(false);
            if (respawnTime > 0f)
            {
                respawnTimer = respawnTime;
            }
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Player picked up {ammoAmount} {ammoType.AmmoName} from ammo box");
        #endif
    }

    /// <summary>
    /// Respawns the ammo box after the respawn timer.
    /// </summary>
    private void Respawn()
    {
        isPickedUp = false;
        SetVisible(true);
        respawnTimer = 0f;
    }

    /// <summary>
    /// Sets the visibility of the ammo box.
    /// </summary>
    private void SetVisible(bool visible)
    {
        if (renderers != null)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        if (boxCollider != null)
        {
            boxCollider.enabled = visible;
        }
    }

    /// <summary>
    /// Plays the pickup sound.
    /// </summary>
    private void PlayPickupSound()
    {
        if (pickupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pickupSound, pickupSoundVolume);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw pickup radius in editor
        if (!useTrigger)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}

