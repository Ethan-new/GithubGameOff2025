using UnityEngine;

/// <summary>
/// Fire mode for weapons - determines if weapon fires once per press or continuously.
/// </summary>
public enum FireMode
{
    SingleFire,  // Fires once per button press
    FullAuto    // Fires continuously while button is held
}

/// <summary>
/// Base class for all weapons in the game.
/// </summary>
public class Weapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] protected string weaponName = "Weapon";
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float attackCooldown = 0.1f; // Reduced for faster firing
    [SerializeField] protected float attackRange = 5f;
    [SerializeField] protected FireMode fireMode = FireMode.FullAuto; // Single fire or full auto

    [Header("Position Settings")]
    [SerializeField] protected Vector3 positionOffset = Vector3.zero;
    [SerializeField] protected Vector3 rotationOffset = Vector3.zero;
    [SerializeField] protected Transform firePoint;

    [Header("Audio Settings")]
    [SerializeField] protected AudioClip fireSound; // Sound played when weapon fires
    [SerializeField] protected float fireSoundVolume = 1f; // Volume of the fire sound (0-1)

    [Header("Recoil Settings")]
    [SerializeField] protected Vector3 recoilPosition = new Vector3(0f, 0f, -0.1f); // How much weapon moves back on recoil
    [SerializeField] protected Vector3 recoilRotation = new Vector3(-5f, 0f, 0f); // How much weapon rotates up on recoil
    [SerializeField] protected float recoilDuration = 0.05f; // How long recoil takes (reduced for faster firing)
    [SerializeField] protected float recoverySpeed = 2f; // How fast weapon recovers (higher = faster recovery)
    [SerializeField] protected float maxRecoilStack = 3f; // Maximum recoil stack multiplier
    [SerializeField] protected AnimationCurve recoilCurve; // Recoil animation curve (ease-out)
    [SerializeField] protected AnimationCurve recoveryCurve; // Recovery animation curve (ease-in)

    [Header("Raycast Settings")]
    [SerializeField] protected LayerMask hitLayers = -1; // All layers by default
    [SerializeField] protected QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;

    protected float lastAttackTime = 0f;
    protected bool isEquipped = false;
    protected Camera playerCamera;
    private Coroutine recoilCoroutine = null;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private float currentRecoilAmount = 0f; // Current recoil stack (0 to maxRecoilStack)
    private AudioSource audioSource; // Audio source for playing sounds
    private bool allowRecoilTransformUpdate = true; // Whether recoil can directly update transform (false when external animations are controlling it)

    /// <summary>
    /// The name of the weapon.
    /// </summary>
    public string WeaponName => weaponName;

    /// <summary>
    /// The damage this weapon deals.
    /// </summary>
    public float Damage => damage;

    /// <summary>
    /// Whether this weapon is currently equipped.
    /// </summary>
    public bool IsEquipped => isEquipped;

    /// <summary>
    /// The fire mode of this weapon (SingleFire or FullAuto).
    /// </summary>
    public FireMode FireMode => fireMode;

    /// <summary>
    /// Called when the weapon is equipped.
    /// </summary>
    public virtual void OnEquip()
    {
        isEquipped = true;
        gameObject.SetActive(true);
        ApplyPositionOffset();
        
        // Store base position/rotation for recoil
        baseLocalPosition = positionOffset;
        baseLocalRotation = Quaternion.Euler(rotationOffset);
    }

    /// <summary>
    /// Called when the weapon is unequipped.
    /// </summary>
    public virtual void OnUnequip()
    {
        isEquipped = false;
        // Hide weapon when unequipped (but keep it in inventory)
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Applies the position and rotation offsets to the weapon transform.
    /// </summary>
    protected virtual void ApplyPositionOffset()
    {
        transform.localPosition = positionOffset;
        transform.localRotation = Quaternion.Euler(rotationOffset);
        
        // Update base position/rotation for recoil
        baseLocalPosition = positionOffset;
        baseLocalRotation = Quaternion.Euler(rotationOffset);
    }

    /// <summary>
    /// Gets the position offset for this weapon.
    /// </summary>
    public Vector3 GetPositionOffset()
    {
        return positionOffset;
    }

    /// <summary>
    /// Gets the rotation offset for this weapon.
    /// </summary>
    public Vector3 GetRotationOffset()
    {
        return rotationOffset;
    }

    /// <summary>
    /// Attempts to attack with this weapon. Returns true if the attack was successful.
    /// Note: Recoil animation does not prevent firing - only the cooldown limits fire rate.
    /// You can fire again as soon as the cooldown expires, even if recoil is still playing.
    /// </summary>
    public virtual bool Attack()
    {
        // Check cooldown (this is the only thing preventing rapid fire)
        if (Time.time - lastAttackTime < attackCooldown)
        {
            return false;
        }

        // Perform attack
        lastAttackTime = Time.time;
        PerformAttack();
        
        // Play fire sound
        PlayFireSound();
        
        // Trigger recoil animation (purely visual, doesn't block future attacks)
        TriggerRecoil();
        
        return true;
    }

    /// <summary>
    /// Override this method to implement weapon-specific attack behavior.
    /// </summary>
    protected virtual void PerformAttack()
    {
        // Get the camera for raycast origin and direction
        Camera camera = GetCamera();
        if (camera == null)
        {
            Debug.LogWarning($"{weaponName}: No camera found for raycast attack!");
            return;
        }

        // Shoot raycast from camera center (screen center) forward
        Vector3 rayOrigin = camera.transform.position;
        Vector3 rayDirection = camera.transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, attackRange, hitLayers, queryTriggerInteraction))
        {
            // Check for crit zone on the hit collider
            CritZone critZone = hit.collider.GetComponent<CritZone>();
            float finalDamage = damage;
            bool isCrit = false;
            
            if (critZone != null)
            {
                finalDamage = damage * critZone.DamageMultiplier;
                isCrit = true;
                Debug.Log($"{weaponName} CRIT HIT: {critZone.ZoneName} on {hit.collider.name} at distance {hit.distance:F2}m for {finalDamage} damage (x{critZone.DamageMultiplier})");
            }
            else
            {
                Debug.Log($"{weaponName} hit: {hit.collider.name} at distance {hit.distance:F2}m for {damage} damage");
            }
            
            // Apply damage to enemy if it has EnemyHealth component
            EnemyHealth enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                // Try to get it from parent in case the collider is a child object
                enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
            }
            
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(finalDamage);
            }
        }
        else
        {
            Debug.Log($"{weaponName} fired - no hit");
        }

        // Visual debug: Draw raycast in scene view
        Debug.DrawRay(rayOrigin, rayDirection * attackRange, Color.red, 0.1f);
    }

    /// <summary>
    /// Gets the camera to use for raycast attacks. Tries to find the main camera or a camera in the scene.
    /// </summary>
    protected virtual Camera GetCamera()
    {
        // Cache camera reference
        if (playerCamera == null)
        {
            // Try to find camera in parent (PlayerController should have it)
            playerCamera = GetComponentInParent<Camera>();
            
            // If not found, try main camera
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
            
            // Last resort: find any camera in the scene
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }
        }
        
        return playerCamera;
    }

    /// <summary>
    /// Checks if the weapon can attack (not on cooldown).
    /// </summary>
    public bool CanAttack()
    {
        return Time.time - lastAttackTime >= attackCooldown;
    }

    /// <summary>
    /// Triggers the recoil animation. Can be called rapidly for fast firing.
    /// Recoil does not prevent firing - it's purely visual.
    /// </summary>
    protected virtual void TriggerRecoil()
    {
        if (!isEquipped || !gameObject.activeInHierarchy)
            return;

        // Add to recoil stack (capped at max)
        currentRecoilAmount = Mathf.Min(currentRecoilAmount + 1f, maxRecoilStack);

        // Start recoil animation if not already running
        if (recoilCoroutine == null)
        {
            recoilCoroutine = StartCoroutine(AnimateRecoil());
        }
        // If already running, it will just add to the stack and continue animating
    }

    /// <summary>
    /// Coroutine that animates the weapon recoil and continuous recovery.
    /// </summary>
    protected virtual System.Collections.IEnumerator AnimateRecoil()
    {
        // Continuous loop that handles both recoil application and recovery
        while (currentRecoilAmount > 0f)
        {
            // Only update transform directly if allowed (not when external animations are controlling it)
            if (allowRecoilTransformUpdate)
            {
                // Apply current recoil amount smoothly
                float recoilMultiplier = currentRecoilAmount;
                Vector3 targetPosition = baseLocalPosition + (recoilPosition * recoilMultiplier);
                Quaternion targetRotation = baseLocalRotation * Quaternion.Euler(recoilRotation * recoilMultiplier);

                // Smoothly interpolate to target position
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * 20f);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * 20f);
            }

            // Reduce recoil stack over time (recovery)
            currentRecoilAmount = Mathf.Max(0f, currentRecoilAmount - recoverySpeed * Time.deltaTime);

            yield return null;
        }

        // Ensure we're exactly at base position/rotation (only if we're controlling the transform)
        if (allowRecoilTransformUpdate)
        {
            transform.localPosition = baseLocalPosition;
            transform.localRotation = baseLocalRotation;
        }
        currentRecoilAmount = 0f;

        recoilCoroutine = null;
    }

    /// <summary>
    /// Stops any active recoil animation and resets weapon to base position.
    /// </summary>
    public void StopRecoil()
    {
        if (recoilCoroutine != null)
        {
            StopCoroutine(recoilCoroutine);
            recoilCoroutine = null;
        }
        
        currentRecoilAmount = 0f;
        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;
    }

    /// <summary>
    /// Checks if the weapon currently has active recoil animation.
    /// </summary>
    public bool HasActiveRecoil()
    {
        return recoilCoroutine != null && currentRecoilAmount > 0f;
    }

    /// <summary>
    /// Gets the current recoil position offset (for combining with other animations).
    /// </summary>
    public Vector3 GetRecoilPositionOffset()
    {
        if (currentRecoilAmount > 0f)
        {
            float recoilMultiplier = currentRecoilAmount;
            return recoilPosition * recoilMultiplier;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Gets the current recoil rotation offset (for combining with other animations).
    /// </summary>
    public Vector3 GetRecoilRotationOffset()
    {
        if (currentRecoilAmount > 0f)
        {
            float recoilMultiplier = currentRecoilAmount;
            return recoilRotation * recoilMultiplier;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Sets whether the recoil animation can directly update the transform.
    /// Set to false when external animations (like walk) are controlling the transform.
    /// </summary>
    public void SetAllowRecoilTransformUpdate(bool allow)
    {
        allowRecoilTransformUpdate = allow;
    }

    /// <summary>
    /// Plays the fire sound if one is assigned.
    /// </summary>
    protected virtual void PlayFireSound()
    {
        if (fireSound == null)
            return;

        // Get or create audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                // Create audio source if one doesn't exist
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound (can be changed to 1f for 3D)
            }
        }

        // Play the fire sound
        audioSource.PlayOneShot(fireSound, fireSoundVolume);
    }

    protected virtual void Awake()
    {
        // Find fire point if not assigned
        if (firePoint == null)
        {
            firePoint = transform;
        }
        
        // Initialize base position/rotation
        baseLocalPosition = positionOffset;
        baseLocalRotation = Quaternion.Euler(rotationOffset);
        
        // Initialize default animation curves if not set
        if (recoilCurve == null || recoilCurve.length == 0)
        {
            // Create ease-out curve (fast start, slow end)
            recoilCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 2f),  // Start at 0 with upward tangent
                new Keyframe(1f, 1f, 0f, 0f)  // End at 1 with flat tangent
            );
        }
        
        if (recoveryCurve == null || recoveryCurve.length == 0)
        {
            // Create ease-in curve (slow start, fast end)
            recoveryCurve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),  // Start at 0 with flat tangent
                new Keyframe(1f, 1f, 2f, 0f)   // End at 1 with upward tangent
            );
        }
    }

    protected virtual void OnDisable()
    {
        // Stop recoil animation if weapon is disabled
        StopRecoil();
    }
}

