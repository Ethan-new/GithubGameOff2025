using UnityEngine;

/// <summary>
/// ScriptableObject that defines an ammo type. Create instances of this in the project to define different ammo types.
/// </summary>
[CreateAssetMenu(fileName = "New Ammo Type", menuName = "Game/Ammo Type")]
public class AmmoType : ScriptableObject
{
    [Header("Ammo Type Settings")]
    [SerializeField] private string ammoName = "Ammo";
    [SerializeField] private string description = "Standard ammunition";
    [SerializeField] private Sprite icon; // Optional icon for UI
    [SerializeField] private Color color = Color.white; // Optional color for UI

    /// <summary>
    /// The display name of this ammo type.
    /// </summary>
    public string AmmoName => ammoName;

    /// <summary>
    /// The description of this ammo type.
    /// </summary>
    public string Description => description;

    /// <summary>
    /// The icon sprite for this ammo type (for UI).
    /// </summary>
    public Sprite Icon => icon;

    /// <summary>
    /// The color associated with this ammo type (for UI).
    /// </summary>
    public Color Color => color;
}

