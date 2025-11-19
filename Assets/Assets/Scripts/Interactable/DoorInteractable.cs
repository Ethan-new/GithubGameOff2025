using UnityEngine;

/// <summary>
/// Interactable door component. When the player presses E on this door, it will open/close.
/// Implementation not yet added - this is a placeholder.
/// </summary>
public class DoorInteractable : Interactable
{
    [Header("Door Settings")]
    [SerializeField] private bool isOpen = false;
    [SerializeField] private bool isLocked = false;
    [SerializeField] private string lockedMessage = "This door is locked.";

    public override bool CanInteract
    {
        get
        {
            // TODO: Add logic to determine if door can be interacted with
            // (e.g., check if locked, check if player has key, etc.)
            return base.CanInteract && !isLocked;
        }
    }

    public override void OnInteract(GameObject interactor)
    {
        if (!CanInteract)
        {
            // TODO: Show locked message to player
            Debug.Log(lockedMessage);
            return;
        }

        // TODO: Implement door opening/closing logic
        // - Animate door opening/closing
        // - Play sound effects
        // - Update isOpen state
        // - Handle door collision/physics
        
        isOpen = !isOpen;
        Debug.Log($"Door interaction: {(isOpen ? "Opened" : "Closed")}");
    }

    /// <summary>
    /// Unlocks the door.
    /// </summary>
    public void Unlock()
    {
        isLocked = false;
    }

    /// <summary>
    /// Locks the door.
    /// </summary>
    public void Lock()
    {
        isLocked = true;
        isOpen = false; // Close door when locking
    }
}

