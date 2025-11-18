using UnityEngine;

/// <summary>
/// Base class for all augments (player and weapon modifications).
/// </summary>
public abstract class Augment : ScriptableObject
{
    [Header("Augment Settings")]
    [SerializeField] protected string augmentName = "Augment";
    [SerializeField] protected string description = "An augment that modifies something";
    [SerializeField] protected Sprite icon; // Optional icon for UI
    [SerializeField] protected int maxStack = 1; // Maximum number of times this augment can be stacked

    /// <summary>
    /// The display name of this augment.
    /// </summary>
    public string AugmentName => augmentName;

    /// <summary>
    /// The description of this augment.
    /// </summary>
    public string Description => description;

    /// <summary>
    /// The icon sprite for this augment (for UI).
    /// </summary>
    public Sprite Icon => icon;

    /// <summary>
    /// The maximum number of times this augment can be stacked.
    /// </summary>
    public int MaxStack => maxStack;

    /// <summary>
    /// Applies this augment's effects. Override in derived classes.
    /// </summary>
    public abstract void Apply();

    /// <summary>
    /// Removes this augment's effects. Override in derived classes.
    /// </summary>
    public abstract void Remove();
}

