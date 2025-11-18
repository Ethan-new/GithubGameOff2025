using UnityEngine;

/// <summary>
/// Augment that modifies weapon stats and abilities.
/// </summary>
[CreateAssetMenu(fileName = "New Weapon Augment", menuName = "Game/Weapon Augment")]
public class WeaponAugment : Augment
{
    [Header("Weapon Stat Modifications")]
    [SerializeField] private float damageMultiplier = 1f; // Multiplier for damage (1 = no change)
    [SerializeField] private float fireRateMultiplier = 1f; // Multiplier for fire rate (1 = no change, 1.5 = 50% faster)
    [SerializeField] private float reloadSpeedMultiplier = 1f; // Multiplier for reload speed (1 = no change, 1.5 = 50% faster)
    [SerializeField] private float magazineSizeMultiplier = 1f; // Multiplier for magazine size (1 = no change, 1.5 = 50% more)
    [SerializeField] private float rangeMultiplier = 1f; // Multiplier for attack range
    [SerializeField] private float recoilReduction = 0f; // Flat recoil reduction (0-1, where 0.5 = 50% less recoil)
    [SerializeField] private float accuracyMultiplier = 1f; // Multiplier for accuracy (1 = no change, 1.5 = 50% more accurate)

    /// <summary>
    /// Multiplier for damage (1 = no change, 1.5 = 50% more damage).
    /// </summary>
    public float DamageMultiplier => damageMultiplier;

    /// <summary>
    /// Multiplier for fire rate (1 = no change, 1.5 = 50% faster).
    /// </summary>
    public float FireRateMultiplier => fireRateMultiplier;

    /// <summary>
    /// Multiplier for reload speed (1 = no change, 1.5 = 50% faster).
    /// </summary>
    public float ReloadSpeedMultiplier => reloadSpeedMultiplier;

    /// <summary>
    /// Multiplier for magazine size (1 = no change, 1.5 = 50% more).
    /// </summary>
    public float MagazineSizeMultiplier => magazineSizeMultiplier;

    /// <summary>
    /// Multiplier for attack range (1 = no change, 1.5 = 50% more range).
    /// </summary>
    public float RangeMultiplier => rangeMultiplier;

    /// <summary>
    /// Flat recoil reduction (0-1, where 0.5 = 50% less recoil).
    /// </summary>
    public float RecoilReduction => recoilReduction;

    /// <summary>
    /// Multiplier for accuracy (1 = no change, 1.5 = 50% more accurate).
    /// </summary>
    public float AccuracyMultiplier => accuracyMultiplier;

    public override void Apply()
    {
        // Augments are applied by PlayerInventory, which modifies Weapon stats
        // This method can be used for one-time effects if needed
    }

    public override void Remove()
    {
        // Augments are removed by PlayerInventory, which restores Weapon stats
        // This method can be used for cleanup if needed
    }
}

