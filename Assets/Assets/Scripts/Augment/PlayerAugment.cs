using UnityEngine;

/// <summary>
/// Augment that modifies player stats and abilities.
/// </summary>
[CreateAssetMenu(fileName = "New Player Augment", menuName = "Game/Player Augment")]
public class PlayerAugment : Augment
{
    [Header("Player Stat Modifications")]
    [SerializeField] private float moveSpeedMultiplier = 1f; // Multiplier for movement speed (1 = no change)
    [SerializeField] private float sprintSpeedMultiplier = 1f; // Multiplier for sprint speed
    [SerializeField] private float jumpForceMultiplier = 1f; // Multiplier for jump force
    [SerializeField] private float healthMultiplier = 1f; // Multiplier for max health
    [SerializeField] private float damageResistance = 0f; // Flat damage reduction (0-1, where 1 = 100% resistance)
    [SerializeField] private float pickupRangeMultiplier = 1f; // Multiplier for pickup range

    /// <summary>
    /// Multiplier for movement speed (1 = no change, 1.5 = 50% faster).
    /// </summary>
    public float MoveSpeedMultiplier => moveSpeedMultiplier;

    /// <summary>
    /// Multiplier for sprint speed (1 = no change, 1.5 = 50% faster).
    /// </summary>
    public float SprintSpeedMultiplier => sprintSpeedMultiplier;

    /// <summary>
    /// Multiplier for jump force (1 = no change, 1.5 = 50% stronger).
    /// </summary>
    public float JumpForceMultiplier => jumpForceMultiplier;

    /// <summary>
    /// Multiplier for max health (1 = no change, 1.5 = 50% more health).
    /// </summary>
    public float HealthMultiplier => healthMultiplier;

    /// <summary>
    /// Flat damage reduction (0-1, where 0.5 = 50% damage reduction).
    /// </summary>
    public float DamageResistance => damageResistance;

    /// <summary>
    /// Multiplier for pickup range (1 = no change, 1.5 = 50% more range).
    /// </summary>
    public float PickupRangeMultiplier => pickupRangeMultiplier;

    public override void Apply()
    {
        // Augments are applied by PlayerInventory, which modifies PlayerController stats
        // This method can be used for one-time effects if needed
    }

    public override void Remove()
    {
        // Augments are removed by PlayerInventory, which restores PlayerController stats
        // This method can be used for cleanup if needed
    }
}

