using UnityEngine;

/// <summary>
/// Interface for all objects that can be interacted with by the player.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// The interaction range for this object.
    /// </summary>
    float InteractionRange { get; }

    /// <summary>
    /// The display name shown to the player when looking at this object.
    /// </summary>
    string InteractionPrompt { get; }

    /// <summary>
    /// Whether this object can currently be interacted with.
    /// </summary>
    bool CanInteract { get; }

    /// <summary>
    /// Checks if the given position is within interaction range.
    /// </summary>
    bool IsInRange(Vector3 position);

    /// <summary>
    /// Called when the player interacts with this object.
    /// </summary>
    /// <param name="interactor">The GameObject that interacted with this object (usually the player).</param>
    void OnInteract(GameObject interactor);

    /// <summary>
    /// Called when the player looks at this object (for UI prompts).
    /// </summary>
    void OnLookAt();

    /// <summary>
    /// Called when the player stops looking at this object.
    /// </summary>
    void OnLookAway();
}

