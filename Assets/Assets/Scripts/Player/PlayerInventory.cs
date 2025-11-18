using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the player's inventory including ammo, player augments, and weapon augments.
/// Attach this component to the Player GameObject or a child object.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("Starting Inventory")]
    [SerializeField] private List<AmmoType> startingAmmoTypes = new List<AmmoType>();
    [SerializeField] private List<int> startingAmmoAmounts = new List<int>();
    [SerializeField] private List<PlayerAugment> startingPlayerAugments = new List<PlayerAugment>();
    [SerializeField] private List<WeaponAugment> startingWeaponAugments = new List<WeaponAugment>();

    [Header("Max Inventory Limits")]
    [SerializeField] private int maxAmmoPerType = 999; // Maximum ammo per type
    [SerializeField] private int maxPlayerAugments = 10; // Maximum number of different player augments
    [SerializeField] private int maxWeaponAugments = 10; // Maximum number of different weapon augments per weapon

    // Ammo inventory: ammo type -> quantity
    private Dictionary<AmmoType, int> ammoInventory = new Dictionary<AmmoType, int>();

    // Player augments: augment -> stack count
    private Dictionary<PlayerAugment, int> playerAugments = new Dictionary<PlayerAugment, int>();

    // Weapon augments: weapon -> list of augments
    private Dictionary<Weapon, List<WeaponAugment>> weaponAugments = new Dictionary<Weapon, List<WeaponAugment>>();

    // Cached references
    private PlayerController playerController;

    // Events for UI updates
    public System.Action<AmmoType, int> OnAmmoChanged;
    public System.Action<PlayerAugment> OnPlayerAugmentAdded;
    public System.Action<PlayerAugment> OnPlayerAugmentRemoved;
    public System.Action<Weapon, WeaponAugment> OnWeaponAugmentAdded;
    public System.Action<Weapon, WeaponAugment> OnWeaponAugmentRemoved;

    private void Awake()
    {
        // Find PlayerController
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
        }

        // Initialize starting ammo
        for (int i = 0; i < startingAmmoTypes.Count && i < startingAmmoAmounts.Count; i++)
        {
            if (startingAmmoTypes[i] != null)
            {
                AddAmmo(startingAmmoTypes[i], startingAmmoAmounts[i]);
            }
        }

        // Initialize starting player augments
        foreach (var augment in startingPlayerAugments)
        {
            if (augment != null)
            {
                AddPlayerAugment(augment);
            }
        }

        // Initialize starting weapon augments (these will be applied when weapons are added)
        // Note: Starting weapon augments are stored but not applied until a weapon is equipped
    }

    #region Ammo Management

    /// <summary>
    /// Adds ammo of the specified type to the inventory.
    /// </summary>
    public bool AddAmmo(AmmoType ammoType, int amount)
    {
        if (ammoType == null || amount <= 0)
            return false;

        if (ammoInventory.ContainsKey(ammoType))
        {
            ammoInventory[ammoType] = Mathf.Min(ammoInventory[ammoType] + amount, maxAmmoPerType);
        }
        else
        {
            ammoInventory[ammoType] = Mathf.Min(amount, maxAmmoPerType);
        }

        OnAmmoChanged?.Invoke(ammoType, ammoInventory[ammoType]);
        return true;
    }

    /// <summary>
    /// Removes ammo of the specified type from the inventory.
    /// </summary>
    public bool RemoveAmmo(AmmoType ammoType, int amount)
    {
        if (ammoType == null || amount <= 0)
            return false;

        if (!ammoInventory.ContainsKey(ammoType))
            return false;

        if (ammoInventory[ammoType] < amount)
            return false;

        ammoInventory[ammoType] -= amount;
        if (ammoInventory[ammoType] <= 0)
        {
            ammoInventory.Remove(ammoType);
        }

        OnAmmoChanged?.Invoke(ammoType, ammoInventory.ContainsKey(ammoType) ? ammoInventory[ammoType] : 0);
        return true;
    }

    /// <summary>
    /// Gets the amount of ammo of the specified type in the inventory.
    /// </summary>
    public int GetAmmoCount(AmmoType ammoType)
    {
        if (ammoType == null || !ammoInventory.ContainsKey(ammoType))
            return 0;

        return ammoInventory[ammoType];
    }

    /// <summary>
    /// Checks if the inventory has at least the specified amount of ammo.
    /// </summary>
    public bool HasAmmo(AmmoType ammoType, int amount)
    {
        return GetAmmoCount(ammoType) >= amount;
    }

    /// <summary>
    /// Gets all ammo types in the inventory.
    /// </summary>
    public Dictionary<AmmoType, int> GetAllAmmo()
    {
        return new Dictionary<AmmoType, int>(ammoInventory);
    }

    #endregion

    #region Player Augment Management

    /// <summary>
    /// Adds a player augment to the inventory. Returns true if successful.
    /// </summary>
    public bool AddPlayerAugment(PlayerAugment augment)
    {
        if (augment == null)
            return false;

        // Check if we've reached the max number of different augments
        if (playerAugments.Count >= maxPlayerAugments && !playerAugments.ContainsKey(augment))
        {
            Debug.LogWarning($"Cannot add player augment: Maximum number of different augments ({maxPlayerAugments}) reached.");
            return false;
        }

        // Check stack limit
        if (playerAugments.ContainsKey(augment))
        {
            if (playerAugments[augment] >= augment.MaxStack)
            {
                Debug.LogWarning($"Cannot add player augment: Maximum stack ({augment.MaxStack}) reached for {augment.AugmentName}.");
                return false;
            }
            playerAugments[augment]++;
        }
        else
        {
            playerAugments[augment] = 1;
        }

        // Apply augment effects
        ApplyPlayerAugment(augment);
        OnPlayerAugmentAdded?.Invoke(augment);

        return true;
    }

    /// <summary>
    /// Removes a player augment from the inventory. Returns true if successful.
    /// </summary>
    public bool RemovePlayerAugment(PlayerAugment augment)
    {
        if (augment == null || !playerAugments.ContainsKey(augment))
            return false;

        // Remove augment effects
        RemovePlayerAugmentEffects(augment);

        playerAugments[augment]--;
        if (playerAugments[augment] <= 0)
        {
            playerAugments.Remove(augment);
        }
        else
        {
            // Re-apply remaining stacks
            ApplyPlayerAugment(augment);
        }

        OnPlayerAugmentRemoved?.Invoke(augment);
        return true;
    }

    /// <summary>
    /// Gets the stack count of a player augment.
    /// </summary>
    public int GetPlayerAugmentStack(PlayerAugment augment)
    {
        if (augment == null || !playerAugments.ContainsKey(augment))
            return 0;

        return playerAugments[augment];
    }

    /// <summary>
    /// Checks if the player has a specific augment.
    /// </summary>
    public bool HasPlayerAugment(PlayerAugment augment)
    {
        return augment != null && playerAugments.ContainsKey(augment);
    }

    /// <summary>
    /// Gets all player augments in the inventory.
    /// </summary>
    public Dictionary<PlayerAugment, int> GetAllPlayerAugments()
    {
        return new Dictionary<PlayerAugment, int>(playerAugments);
    }

    /// <summary>
    /// Applies a player augment's effects to the PlayerController.
    /// </summary>
    private void ApplyPlayerAugment(PlayerAugment augment)
    {
        if (playerController == null)
            return;

        // Calculate total multipliers from all stacks
        int stackCount = playerAugments.ContainsKey(augment) ? playerAugments[augment] : 1;
        
        // Apply multipliers (this is a simplified version - you may want to cache base values)
        // Note: This assumes PlayerController exposes setters for these values
        // For now, we'll just store the augment - actual application would need PlayerController modifications
        augment.Apply();
    }

    /// <summary>
    /// Removes a player augment's effects from the PlayerController.
    /// </summary>
    private void RemovePlayerAugmentEffects(PlayerAugment augment)
    {
        if (playerController == null)
            return;

        augment.Remove();
    }

    /// <summary>
    /// Gets the combined multiplier for a player stat from all augments.
    /// </summary>
    public float GetPlayerStatMultiplier(System.Func<PlayerAugment, float> getMultiplier)
    {
        float totalMultiplier = 1f;
        foreach (var kvp in playerAugments)
        {
            int stackCount = kvp.Value;
            float augmentMultiplier = getMultiplier(kvp.Key);
            // Stack multiplicatively
            totalMultiplier *= Mathf.Pow(augmentMultiplier, stackCount);
        }
        return totalMultiplier;
    }

    #endregion

    #region Weapon Augment Management

    /// <summary>
    /// Adds a weapon augment to a specific weapon. Returns true if successful.
    /// </summary>
    public bool AddWeaponAugment(Weapon weapon, WeaponAugment augment)
    {
        if (weapon == null || augment == null)
            return false;

        // Initialize weapon augment list if needed
        if (!weaponAugments.ContainsKey(weapon))
        {
            weaponAugments[weapon] = new List<WeaponAugment>();
        }

        // Check if we've reached the max number of different augments for this weapon
        if (weaponAugments[weapon].Count >= maxWeaponAugments)
        {
            Debug.LogWarning($"Cannot add weapon augment: Maximum number of augments ({maxWeaponAugments}) reached for weapon {weapon.WeaponName}.");
            return false;
        }

        // Check stack limit
        int stackCount = 0;
        foreach (var existingAugment in weaponAugments[weapon])
        {
            if (existingAugment == augment)
            {
                stackCount++;
            }
        }

        if (stackCount >= augment.MaxStack)
        {
            Debug.LogWarning($"Cannot add weapon augment: Maximum stack ({augment.MaxStack}) reached for {augment.AugmentName} on {weapon.WeaponName}.");
            return false;
        }

        weaponAugments[weapon].Add(augment);
        ApplyWeaponAugment(weapon, augment);
        OnWeaponAugmentAdded?.Invoke(weapon, augment);

        return true;
    }

    /// <summary>
    /// Removes a weapon augment from a specific weapon. Returns true if successful.
    /// </summary>
    public bool RemoveWeaponAugment(Weapon weapon, WeaponAugment augment)
    {
        if (weapon == null || augment == null || !weaponAugments.ContainsKey(weapon))
            return false;

        if (!weaponAugments[weapon].Remove(augment))
            return false;

        RemoveWeaponAugmentEffects(weapon, augment);
        
        // Re-apply remaining augments to recalculate stats
        ReapplyWeaponAugments(weapon);
        
        OnWeaponAugmentRemoved?.Invoke(weapon, augment);
        return true;
    }

    /// <summary>
    /// Gets all augments for a specific weapon.
    /// </summary>
    public List<WeaponAugment> GetWeaponAugments(Weapon weapon)
    {
        if (weapon == null || !weaponAugments.ContainsKey(weapon))
            return new List<WeaponAugment>();

        return new List<WeaponAugment>(weaponAugments[weapon]);
    }

    /// <summary>
    /// Checks if a weapon has a specific augment.
    /// </summary>
    public bool HasWeaponAugment(Weapon weapon, WeaponAugment augment)
    {
        if (weapon == null || augment == null || !weaponAugments.ContainsKey(weapon))
            return false;

        return weaponAugments[weapon].Contains(augment);
    }

    /// <summary>
    /// Removes all augments from a weapon (called when weapon is removed from inventory).
    /// </summary>
    public void ClearWeaponAugments(Weapon weapon)
    {
        if (weapon == null || !weaponAugments.ContainsKey(weapon))
            return;

        foreach (var augment in weaponAugments[weapon])
        {
            RemoveWeaponAugmentEffects(weapon, augment);
        }

        weaponAugments.Remove(weapon);
    }

    /// <summary>
    /// Applies a weapon augment's effects to a weapon.
    /// </summary>
    private void ApplyWeaponAugment(Weapon weapon, WeaponAugment augment)
    {
        // Apply augment effects
        // Note: This would require Weapon class to have methods for applying stat modifications
        // For now, we'll just call the augment's Apply method
        augment.Apply();
    }

    /// <summary>
    /// Removes a weapon augment's effects from a weapon.
    /// </summary>
    private void RemoveWeaponAugmentEffects(Weapon weapon, WeaponAugment augment)
    {
        augment.Remove();
    }

    /// <summary>
    /// Reapplies all augments for a weapon (used when removing an augment to recalculate stats).
    /// </summary>
    private void ReapplyWeaponAugments(Weapon weapon)
    {
        if (weapon == null || !weaponAugments.ContainsKey(weapon))
            return;

        // Remove all effects first
        foreach (var augment in weaponAugments[weapon])
        {
            RemoveWeaponAugmentEffects(weapon, augment);
        }

        // Re-apply all augments
        foreach (var augment in weaponAugments[weapon])
        {
            ApplyWeaponAugment(weapon, augment);
        }
    }

    /// <summary>
    /// Gets the combined multiplier for a weapon stat from all augments on that weapon.
    /// </summary>
    public float GetWeaponStatMultiplier(Weapon weapon, System.Func<WeaponAugment, float> getMultiplier)
    {
        if (weapon == null || !weaponAugments.ContainsKey(weapon))
            return 1f;

        float totalMultiplier = 1f;
        foreach (var augment in weaponAugments[weapon])
        {
            float augmentMultiplier = getMultiplier(augment);
            totalMultiplier *= augmentMultiplier;
        }
        return totalMultiplier;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Called when a weapon is added to the player's inventory. Applies any starting weapon augments.
    /// </summary>
    public void OnWeaponAdded(Weapon weapon)
    {
        if (weapon == null)
            return;

        // Apply any starting weapon augments to this weapon
        foreach (var augment in startingWeaponAugments)
        {
            if (augment != null)
            {
                AddWeaponAugment(weapon, augment);
            }
        }
    }

    /// <summary>
    /// Called when a weapon is removed from the player's inventory. Clears its augments.
    /// </summary>
    public void OnWeaponRemoved(Weapon weapon)
    {
        if (weapon == null)
            return;

        ClearWeaponAugments(weapon);
    }

    #endregion
}

