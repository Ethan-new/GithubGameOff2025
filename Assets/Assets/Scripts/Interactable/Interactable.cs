using UnityEngine;

/// <summary>
/// Base class for interactable objects. Provides common functionality for interaction detection.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] protected float interactionRange = 3f;
    [SerializeField] protected string interactionPrompt = "Press E to Interact";
    [SerializeField] protected bool canInteract = true;

    [Header("Detection Settings")]
    [SerializeField] protected LayerMask playerLayer = -1; // All layers by default
    [SerializeField] protected bool requireLineOfSight = true;
    [SerializeField] protected bool showGizmos = true;

    protected bool isPlayerLookingAt = false;

    /// <summary>
    /// The interaction range for this object.
    /// </summary>
    public float InteractionRange => interactionRange;

    /// <summary>
    /// The display name shown to the player when looking at this object.
    /// </summary>
    public virtual string InteractionPrompt => interactionPrompt;

    /// <summary>
    /// Whether this object can currently be interacted with.
    /// </summary>
    public virtual bool CanInteract => canInteract;

    /// <summary>
    /// Called when the player interacts with this object.
    /// Override this method to implement specific interaction behavior.
    /// </summary>
    public abstract void OnInteract(GameObject interactor);

    /// <summary>
    /// Called when the player looks at this object (for UI prompts).
    /// </summary>
    public virtual void OnLookAt()
    {
        isPlayerLookingAt = true;
    }

    /// <summary>
    /// Called when the player stops looking at this object.
    /// </summary>
    public virtual void OnLookAway()
    {
        isPlayerLookingAt = false;
    }

    /// <summary>
    /// Checks if the given position is within interaction range.
    /// </summary>
    public bool IsInRange(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        return distance <= interactionRange;
    }

    /// <summary>
    /// Checks if there's a clear line of sight from the given position to this object.
    /// </summary>
    public bool HasLineOfSight(Vector3 fromPosition, LayerMask obstacleLayers)
    {
        if (!requireLineOfSight)
            return true;

        Vector3 direction = transform.position - fromPosition;
        float distance = direction.magnitude;

        // Use the center of the collider as the target
        Collider col = GetComponent<Collider>();
        Vector3 targetPoint = col != null ? col.bounds.center : transform.position;

        RaycastHit hit;
        if (Physics.Raycast(fromPosition, (targetPoint - fromPosition).normalized, out hit, distance, obstacleLayers))
        {
            // Check if we hit this object or its children
            return hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform);
        }

        return true;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        // Draw interaction range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        // Draw line of sight indicator
        if (requireLineOfSight)
        {
            Gizmos.color = Color.yellow;
            Collider col = GetComponent<Collider>();
            Vector3 center = col != null ? col.bounds.center : transform.position;
            Gizmos.DrawLine(transform.position, center);
        }
    }
}

