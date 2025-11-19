using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Basic enemy movement script that moves the enemy towards a target (typically the player).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float angularSpeed = 120f; // Rotation speed in degrees per second
    [SerializeField] private float stoppingDistance = 0.1f; // Very small distance for aggressive chasing
    [SerializeField] private float maxVerticalDistance = 10f; // Maximum Y difference to allow movement (increased for stairs)
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private bool autoBraking = false; // Disable auto-braking for continuous chasing
    [SerializeField] private float baseOffset = 0f; // Vertical offset to prevent sinking into ground

    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool autoFindTarget = true;
    [SerializeField] private float destinationUpdateInterval = 0.1f; // How often to update destination

    private NavMeshAgent navMeshAgent;
    private float destinationUpdateTimer = 0f;
    private EnemyHealth enemyHealth; // Reference to check if enemy is alive

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
        navMeshAgent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        SyncNavMeshAgentSettings();
    }

    /// <summary>
    /// Syncs the serialized movement settings with the NavMeshAgent component.
    /// </summary>
    private void SyncNavMeshAgentSettings()
    {
        if (navMeshAgent == null)
            return;

        navMeshAgent.speed = moveSpeed;
        navMeshAgent.angularSpeed = angularSpeed;
        navMeshAgent.stoppingDistance = stoppingDistance;
        navMeshAgent.acceleration = acceleration;
        navMeshAgent.autoBraking = autoBraking;
        navMeshAgent.baseOffset = baseOffset; // Set base offset to prevent sinking
    }

    /// <summary>
    /// Called when values are changed in the Inspector (Editor only).
    /// </summary>
    private void OnValidate()
    {
        if (navMeshAgent != null)
        {
            SyncNavMeshAgentSettings();
        }
    }

    private void Start()
    {
        // Auto-find target if enabled and target is not set
        if (autoFindTarget && target == null)
        {
            FindTarget();
        }
        
        // If we have a target at start, update destination immediately
        if (target != null)
        {
            destinationUpdateTimer = 0f;
        }
    }

    private float targetSearchCooldown = 0f;
    private const float TARGET_SEARCH_INTERVAL = 1f; // Only search for target once per second

    private void Update()
    {
        // Don't move if enemy is dead
        if (enemyHealth != null && !enemyHealth.IsAlive)
        {
            if (navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
            }
            return;
        }

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

        // Update destination periodically if we have a target - keep chasing until death
        if (target != null)
        {
            destinationUpdateTimer -= Time.deltaTime;
            if (destinationUpdateTimer <= 0f)
            {
                UpdateDestination();
                destinationUpdateTimer = destinationUpdateInterval;
            }
        }
        else
        {
            // Stop moving if no target
            if (navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.isStopped = true;
            }
        }
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
    /// Updates the NavMeshAgent destination to the current target.
    /// </summary>
    private void UpdateDestination()
    {
        if (target == null || !navMeshAgent.isActiveAndEnabled)
            return;
        
        // If agent is not on NavMesh, try to warp it to the nearest valid position
        if (!navMeshAgent.isOnNavMesh)
        {
            UnityEngine.AI.NavMeshHit warpHit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out warpHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                navMeshAgent.Warp(warpHit.position);
            }
            else
            {
                // Can't find valid NavMesh position, skip this update
                return;
            }
        }

        // Don't move if enemy is dead
        if (enemyHealth != null && !enemyHealth.IsAlive)
        {
            navMeshAgent.isStopped = true;
            return;
        }

        // Check if there's a big difference in Y position
        float yDifference = Mathf.Abs(target.position.y - transform.position.y);
        if (yDifference > maxVerticalDistance)
        {
            // Target is too far vertically, stop moving
            navMeshAgent.isStopped = true;
            return;
        }

        // Try to find the nearest valid point on NavMesh near the target
        // Use a larger search radius to account for stairs and elevation changes
        float searchRadius = Mathf.Max(maxVerticalDistance, 5f);
        UnityEngine.AI.NavMeshHit hit;
        bool foundValidPoint = UnityEngine.AI.NavMesh.SamplePosition(target.position, out hit, searchRadius, UnityEngine.AI.NavMesh.AllAreas);
        
        Vector3 destination = target.position;
        if (foundValidPoint)
        {
            // Use the sampled position on NavMesh
            destination = hit.position;
        }
        else
        {
            // If we can't find a valid point, try to find the nearest point on NavMesh
            if (UnityEngine.AI.NavMesh.FindClosestEdge(target.position, out hit, UnityEngine.AI.NavMesh.AllAreas))
            {
                destination = hit.position;
            }
            // If still no valid point, use target position directly and let NavMeshAgent handle it
        }

        // Always set destination - keep chasing until death
        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(destination);
        
        // Don't check path validity here - let the agent keep trying to pathfind
        // This is important for stairs where the path might be temporarily invalid
        // but the agent should keep attempting to find a path
    }

    /// <summary>
    /// Sets a new target for the enemy to move towards.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        // Immediately update destination when target is set
        if (target != null)
        {
            destinationUpdateTimer = 0f;
        }
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
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = moveSpeed;
        }
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
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = moveSpeed;
        }
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

