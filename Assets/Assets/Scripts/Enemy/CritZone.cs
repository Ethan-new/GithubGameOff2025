using UnityEngine;

/// <summary>
/// Component that marks a transform as a critical hit zone with a damage multiplier.
/// Attach this to enemy body parts (head, weak spots, etc.) to enable critical damage.
/// </summary>
public class CritZone : MonoBehaviour
{
    [Header("Crit Settings")]
    [SerializeField] private float damageMultiplier = 2f;
    [SerializeField] private string zoneName = "Crit Zone";

    /// <summary>
    /// The damage multiplier for this crit zone.
    /// </summary>
    public float DamageMultiplier => damageMultiplier;

    /// <summary>
    /// The name of this crit zone (for debugging/logging).
    /// </summary>
    public string ZoneName => zoneName;

    private void OnValidate()
    {
        // Ensure multiplier is at least 1 (no negative or zero multipliers)
        damageMultiplier = Mathf.Max(1f, damageMultiplier);
    }
}





