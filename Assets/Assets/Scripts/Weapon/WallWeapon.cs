using UnityEngine;

/// <summary>
/// Component for weapons that are mounted on walls and can be picked up by the player.
/// Assign a weapon prefab or GameObject in the Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
public class WallWeapon : Interactable
{
    [Header("Wall Weapon Settings")]
    [SerializeField] private Weapon weapon; // Drag the weapon GameObject or prefab here
    [SerializeField] private string weaponPrompt = "Press E to Buy Weapon";
    [SerializeField] private string ammoPrompt = "Press E to Buy Ammo";
    [SerializeField] private int cost = 500; // Cost to purchase this weapon
    [SerializeField] private int ammoCost = 200; // Cost to purchase ammo if player already has the weapon
    [SerializeField] private int ammoAmount = 300; // Amount of ammo to give when buying ammo

    private bool hasBeenPickedUp = false;
    private PlayerController cachedPlayerController; // Cache player controller for prompt updates
    private GameObject lastInteractor; // Cache last interactor for prompt updates

    /// <summary>
    /// The weapon assigned to this wall weapon.
    /// </summary>
    public Weapon Weapon => weapon;

    private void Awake()
    {
        // Ensure weapon is not equipped when on wall
        if (weapon != null)
        {
            weapon.OnUnequip();
        }

        // Don't set interactionPrompt field here - it's computed dynamically in the InteractionPrompt property
        // This ensures the prompt updates based on whether player has the weapon
    }

    /// <summary>
    /// Checks if the player already has this weapon in their inventory.
    /// </summary>
    private bool PlayerHasWeapon(GameObject player)
    {
        if (weapon == null || player == null)
            return false;

        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController == null)
            return false;

        return playerController.HasWeapon(weapon);
    }

    /// <summary>
    /// Gets the current cost based on whether the player has the weapon or not.
    /// </summary>
    private int GetCurrentCost(GameObject player)
    {
        if (PlayerHasWeapon(player))
            return ammoCost;
        return cost;
    }

    /// <summary>
    /// Whether this object can currently be interacted with.
    /// Returns true if the weapon hasn't been picked up, so OnLookAt() is called and prompt can update.
    /// The actual money check happens in OnInteract().
    /// </summary>
    public override bool CanInteract
    {
        get
        {
            // Always return true (unless picked up) so OnLookAt() gets called and prompt can update
            // The actual money/availability check happens in OnInteract()
            return !hasBeenPickedUp && weapon != null;
        }
    }

    /// <summary>
    /// The display name shown to the player when looking at this object.
    /// Includes weapon name and cost. Updates based on whether player has the weapon.
    /// </summary>
    public override string InteractionPrompt
    {
        get
        {
            // Always compute the prompt dynamically
            return GetCurrentPrompt();
        }
    }

    /// <summary>
    /// Computes the current interaction prompt based on player state.
    /// </summary>
    private string GetCurrentPrompt()
    {
        if (hasBeenPickedUp)
            return string.Empty;

        // Try to get player from cached interactor first, then cached controller, then find it
        GameObject player = null;
        
        if (lastInteractor != null)
        {
            player = lastInteractor;
        }
        else if (cachedPlayerController != null)
        {
            player = cachedPlayerController.gameObject;
        }
        else
        {
            // Try to find player
            UpdatePlayerReference();
            player = cachedPlayerController != null ? cachedPlayerController.gameObject : null;
        }

        bool playerHasWeapon = false;
        
        if (player != null && weapon != null)
        {
            playerHasWeapon = PlayerHasWeapon(player);
        }
        
        int currentCost = player != null ? GetCurrentCost(player) : cost;
        int currentMoney = MoneyManager.Instance?.CurrentMoney ?? 0;
        bool hasEnoughMoney = currentMoney >= currentCost;
        
        if (!hasEnoughMoney)
        {
            int needed = currentCost - currentMoney;
            string itemType = playerHasWeapon ? "ammo" : "weapon";
            return $"Not enough money! Need ${currentCost} for {itemType} (You have ${currentMoney}) - Need ${needed} more";
        }

        // Update prompt based on whether player has the weapon
        if (playerHasWeapon)
        {
            if (weapon != null && !string.IsNullOrEmpty(weapon.WeaponName))
            {
                return $"{ammoPrompt}: {weapon.WeaponName} (${ammoCost})";
            }
            else
            {
                return $"{ammoPrompt} (${ammoCost})";
            }
        }
        else
        {
            if (weapon != null && !string.IsNullOrEmpty(weapon.WeaponName))
            {
                return $"{weaponPrompt}: {weapon.WeaponName} (${cost})";
            }
            else
            {
                return $"{weaponPrompt} (${cost})";
            }
        }
    }

    /// <summary>
    /// Called when the player interacts with this wall weapon.
    /// </summary>
    public override void OnInteract(GameObject interactor)
    {
        // Cache the interactor for prompt updates
        if (interactor != null)
        {
            lastInteractor = interactor;
            PlayerController pc = interactor.GetComponent<PlayerController>();
            if (pc != null)
            {
                cachedPlayerController = pc;
            }
        }
        
        if (hasBeenPickedUp || weapon == null)
            return;

        // Check if player has enough money
        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("WallWeapon: MoneyManager instance not found. Cannot purchase.");
            return;
        }

        // Get the PlayerController from the interactor
        PlayerController playerController = interactor.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("WallWeapon: Interactor does not have a PlayerController component.");
            return;
        }

        // Check if player already has the weapon
        bool playerHasWeapon = PlayerHasWeapon(interactor);
        int currentCost = GetCurrentCost(interactor);

        Debug.Log($"WallWeapon OnInteract: playerHasWeapon={playerHasWeapon}, currentCost={currentCost}, weapon={weapon?.WeaponName}");

        // Check if player has enough money for the purchase
        if (MoneyManager.Instance.CurrentMoney < currentCost)
        {
            int currentMoney = MoneyManager.Instance.CurrentMoney;
            int needed = currentCost - currentMoney;
            string purchaseType = playerHasWeapon ? "ammo" : "weapon";
            Debug.LogWarning($"Not enough money to buy {purchaseType}! Need ${currentCost}, you have ${currentMoney}. You need ${needed} more.");
            return;
        }

        // Spend the money
        if (!MoneyManager.Instance.SpendMoney(currentCost))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"WallWeapon: Failed to spend ${currentCost}. Player may not have enough money.");
#endif
            return;
        }

        if (playerHasWeapon)
        {
            // Player already has the weapon - sell them ammo instead
            if (weapon.AmmoType != null)
            {
                PlayerInventory playerInventory = interactor.GetComponent<PlayerInventory>();
                if (playerInventory == null)
                {
                    playerInventory = interactor.GetComponentInChildren<PlayerInventory>();
                }
                if (playerInventory == null)
                {
                    playerInventory = interactor.GetComponentInParent<PlayerInventory>();
                }

                if (playerInventory != null)
                {
                    bool ammoAdded = playerInventory.AddAmmo(weapon.AmmoType, ammoAmount);
                    if (ammoAdded)
                    {
                        Debug.Log($"Player purchased {ammoAmount} {weapon.AmmoType.name} ammo for ${ammoCost}");
                    }
                    else
                    {
                        Debug.LogWarning($"WallWeapon: Failed to add ammo (inventory might be full). Refunding money.");
                        MoneyManager.Instance.AddMoney(currentCost);
                    }
                }
                else
                {
                    Debug.LogWarning("WallWeapon: Could not find PlayerInventory to add ammo. Refunding money.");
                    MoneyManager.Instance.AddMoney(currentCost);
                }
            }
            else
            {
                Debug.LogWarning($"WallWeapon: Weapon '{weapon.WeaponName}' has no ammo type. Refunding money.");
                MoneyManager.Instance.AddMoney(currentCost);
            }
        }
        else
        {
            // Player doesn't have the weapon - sell them the weapon
            bool weaponAdded = playerController.AddWeapon(weapon);
            
            if (weaponAdded)
            {
                // Give the player ammo with the weapon purchase
                if (weapon.AmmoType != null)
                {
                    PlayerInventory playerInventory = interactor.GetComponent<PlayerInventory>();
                    if (playerInventory == null)
                    {
                        playerInventory = interactor.GetComponentInChildren<PlayerInventory>();
                    }
                    if (playerInventory == null)
                    {
                        playerInventory = interactor.GetComponentInParent<PlayerInventory>();
                    }

                    if (playerInventory != null)
                    {
                        playerInventory.AddAmmo(weapon.AmmoType, ammoAmount);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"Player received {ammoAmount} {weapon.AmmoType.name} ammo with weapon purchase");
#endif
                    }
                    else
                    {
                        Debug.LogWarning("WallWeapon: Could not find PlayerInventory to add ammo.");
                    }
                }

                // Auto-equip the purchased weapon
                // Find which slot the weapon was added to by checking all slots
                int weaponSlot = -1;
                string weaponNameToFind = weapon.WeaponName;
                
                // Check up to 10 slots (reasonable max for weapon inventory)
                for (int i = 0; i < 10; i++)
                {
                    Weapon slotWeapon = playerController.GetWeapon(i);
                    if (slotWeapon != null && slotWeapon.WeaponName == weaponNameToFind)
                    {
                        weaponSlot = i;
                        break;
                    }
                }

                // If we found the slot, switch to it
                if (weaponSlot >= 0)
                {
                    // SwitchToWeaponSlot uses 1-based indexing, so add 1
                    playerController.SwitchToWeaponSlot(weaponSlot + 1);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"Auto-equipped purchased weapon: {weapon.WeaponName} in slot {weaponSlot + 1}");
#endif
                }
                else
                {
                    Debug.LogWarning($"WallWeapon: Could not find slot for purchased weapon {weapon.WeaponName} to auto-equip.");
                }

                // Don't destroy the wall weapon - it should stay on the wall for ammo purchases
                // hasBeenPickedUp = true;
                // OnPickedUp();
                
                Debug.Log($"Player purchased wall weapon: {weapon.WeaponName} for ${cost}");
            }
            else
            {
                // Refund the money since we couldn't add the weapon
                MoneyManager.Instance.AddMoney(currentCost);
                
                Debug.LogWarning($"Failed to add weapon {weapon.WeaponName} to player inventory. Inventory may be full. Money refunded.");
            }
        }
    }


    /// <summary>
    /// Called when the player looks at this object. Updates cached player reference and prompt.
    /// </summary>
    public override void OnLookAt()
    {
        base.OnLookAt();
        
        // Try to find the player that's looking at us
        // The player should be the one with the camera that's doing the raycast
        UpdatePlayerReference();
        
        // Force update the base field so it's current (in case UI reads the field directly)
        UpdatePromptField();
    }

    /// <summary>
    /// Updates the cached player reference by finding the PlayerController in the scene.
    /// </summary>
    private void UpdatePlayerReference()
    {
        if (cachedPlayerController == null)
        {
            cachedPlayerController = FindFirstObjectByType<PlayerController>();
        }
        
        // Also try finding by tag as fallback
        if (cachedPlayerController == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                cachedPlayerController = playerObj.GetComponent<PlayerController>();
            }
        }
    }

    /// <summary>
    /// Updates the base interactionPrompt field with the current dynamic value.
    /// This ensures the prompt is always up-to-date even if the property isn't called.
    /// </summary>
    private void UpdatePromptField()
    {
        // Get the current prompt value directly (avoid infinite recursion)
        string currentPrompt = GetCurrentPrompt();
        
        // Update the base field (this ensures compatibility if something reads the field directly)
        interactionPrompt = currentPrompt;
    }

    /// <summary>
    /// Called when the player stops looking at this object.
    /// </summary>
    public override void OnLookAway()
    {
        base.OnLookAway();
        // Keep the cached reference for efficiency
    }


    /// <summary>
    /// Called when this weapon is picked up. Removes the wall weapon component behavior.
    /// The weapon GameObject will be reparented to the player's weapon holder by AddWeapon.
    /// </summary>
    private void OnPickedUp()
    {
        // Disable interaction
        canInteract = false;
        
        // Remove this component - the weapon will be handled by PlayerController.AddWeapon
        Destroy(this);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        
        // Draw additional gizmo to indicate this is a wall weapon
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
    }
}

