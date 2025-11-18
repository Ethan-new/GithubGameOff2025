using UnityEngine;

/// <summary>
/// Basic enemy movement script that moves the enemy towards a target (typically the player).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool autoFindTarget = true;

    private CharacterController characterController;
    private Vector3 velocity;
    private float gravity = -9.81f;

    /// <summary>
    /// The current target the enemy is moving towards.
    /// </summary>
    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        // Auto-find target if enabled and target is not set
        if (autoFindTarget && target == null)
        {
            FindTarget();
        }
    }

    private float targetSearchCooldown = 0f;
    private const float TARGET_SEARCH_INTERVAL = 1f; // Only search for target once per second

    private void Update()
    {
        // Try to find target if we don't have one (with cooldown to avoid expensive Find calls every frame)
        if (target == null && autoFindTarget)
        {
            targetSearchCooldown -= Time.deltaTime;
            if (targetSearchCooldown <= 0f)
            {
                FindTarget();
                targetSearchCooldown = TARGET_SEARCH_INTERVAL; // Reset cooldown
            }
        }
        else if (target != null)
        {
            // Reset cooldown if we have a target
            targetSearchCooldown = 0f;
        }

        // Only move if we have a target
        if (target != null)
        {
            MoveTowardsTarget();
        }

        // Apply gravity
        ApplyGravity();
    }

    /// <summary>
    /// Finds the target by searching for a GameObject with the target tag.
    /// </summary>
    private void FindTarget()
    {
        GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
        if (targetObject != null)
        {
            target = targetObject.transform;
        }
    }

    /// <summary>
    /// Moves the enemy towards the current target.
    /// </summary>
    private void MoveTowardsTarget()
    {
        if (target == null)
            return;

        // Calculate direction to target
        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // Keep movement on horizontal plane
        float distance = direction.magnitude;

        // Stop if we're close enough
        if (distance <= stoppingDistance)
        {
            return;
        }

        // Normalize direction
        direction.Normalize();

        // Rotate towards target
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Move towards target
        Vector3 moveDirection = direction * moveSpeed;
        characterController.Move(moveDirection * Time.deltaTime);
    }

    /// <summary>
    /// Applies gravity to the enemy.
    /// </summary>
    private void ApplyGravity()
    {
        // Check if grounded
        bool isGrounded = characterController.isGrounded;

        // Reset vertical velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Sets a new target for the enemy to move towards.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Clears the current target.
    /// </summary>
    public void ClearTarget()
    {
        target = null;
    }

    /// <summary>
    /// Gets the current move speed.
    /// </summary>
    public float GetMoveSpeed()
    {
        return moveSpeed;
    }

    /// <summary>
    /// Sets the move speed. Useful for wave-based difficulty scaling.
    /// </summary>
    /// <param name="newSpeed">The new move speed value.</param>
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0f, newSpeed);
    }

    /// <summary>
    /// Scales the move speed by a multiplier. Useful for wave-based difficulty scaling.
    /// </summary>
    /// <param name="multiplier">The multiplier to apply to move speed.</param>
    public void ScaleMoveSpeed(float multiplier)
    {
        if (multiplier <= 0f)
            return;

        moveSpeed *= multiplier;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw stopping distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        // Draw line to target
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}

