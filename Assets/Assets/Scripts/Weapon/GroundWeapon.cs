using UnityEngine;

/// <summary>
/// Component for weapons that are placed on the ground and can be picked up.
/// </summary>
[RequireComponent(typeof(Weapon))]
[RequireComponent(typeof(Collider))]
public class GroundWeapon : MonoBehaviour
{
    [Header("Ground Weapon Settings")]
    [SerializeField] private float pickupRadius = 2f;
    [SerializeField] private bool canRotate = true;
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private float bobSpeed = 1f;
    [SerializeField] private float bobAmount = 0.1f;

    private Weapon weapon;
    private Vector3 startPosition;
    private float bobOffset = 0f;

    /// <summary>
    /// The weapon component attached to this object.
    /// </summary>
    public Weapon Weapon => weapon;

    /// <summary>
    /// The radius at which this weapon can be picked up.
    /// </summary>
    public float PickupRadius => pickupRadius;

    private void Awake()
    {
        weapon = GetComponent<Weapon>();
        startPosition = transform.position;
        
        // Ensure weapon is not equipped when on ground
        if (weapon != null)
        {
            weapon.OnUnequip();
        }
    }

    private void Update()
    {
        // Visual effects for ground weapons
        if (canRotate)
        {
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);
        }

        // Bobbing animation
        if (bobAmount > 0)
        {
            bobOffset += bobSpeed * Time.deltaTime;
            Vector3 bobPosition = startPosition;
            bobPosition.y += Mathf.Sin(bobOffset) * bobAmount;
            transform.position = bobPosition;
        }
    }

    /// <summary>
    /// Checks if the given position is within pickup range.
    /// </summary>
    public bool IsInRange(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        bool inRange = distance <= pickupRadius;
        
        // Debug log to help diagnose issues
        if (!inRange && weapon != null)
        {
            Debug.Log($"{weapon.WeaponName}: Distance {distance} > pickup radius {pickupRadius}");
        }
        
        return inRange;
    }

    /// <summary>
    /// Called when this weapon is picked up. Removes the ground weapon component behavior.
    /// </summary>
    public void OnPickedUp()
    {
        // Stop bobbing and rotation
        canRotate = false;
        bobAmount = 0f;
        
        // Optionally destroy this component since the weapon is now in inventory
        // The Weapon component will remain
        Destroy(this);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw pickup radius in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}

