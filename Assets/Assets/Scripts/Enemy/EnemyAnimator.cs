using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls the animator for enemies based on their movement and health state.
/// Syncs with EnemyMovement and EnemyHealth to update animation parameters.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAnimator : MonoBehaviour
{
    [Header("Animation Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string isDeadParameter = "IsDead";
    [SerializeField] private string takeDamageParameter = "TakeDamage";
    
    [Header("Settings")]
    [SerializeField] private float speedSmoothing = 5f; // How fast speed parameter transitions
    
    private Animator animator;
    private EnemyMovement enemyMovement;
    private EnemyHealth enemyHealth;
    private NavMeshAgent navMeshAgent;
    
    private float currentSpeed = 0f;
    private bool wasAlive = true;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemyMovement = GetComponent<EnemyMovement>();
        enemyHealth = GetComponent<EnemyHealth>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        
        // Subscribe to health events
        if (enemyHealth != null)
        {
            enemyHealth.OnDamageTaken += OnDamageTaken;
            enemyHealth.OnDeath += OnDeath;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (enemyHealth != null)
        {
            enemyHealth.OnDamageTaken -= OnDamageTaken;
            enemyHealth.OnDeath -= OnDeath;
        }
    }
    
    private void Update()
    {
        if (animator == null || enemyHealth == null)
            return;
        
        // Update death state
        bool isAlive = enemyHealth.IsAlive;
        if (isAlive != wasAlive)
        {
            animator.SetBool(isDeadParameter, !isAlive);
            wasAlive = isAlive;
        }
        
        // Don't update movement animations if dead
        if (!isAlive)
        {
            currentSpeed = 0f;
            animator.SetFloat(speedParameter, 0f);
            return;
        }
        
        // Update speed based on NavMeshAgent velocity
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            // Get the actual speed from NavMeshAgent
            float targetSpeed = navMeshAgent.velocity.magnitude;
            
            // Smooth the speed transition
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedSmoothing * Time.deltaTime);
            
            // Set the speed parameter in the animator
            animator.SetFloat(speedParameter, currentSpeed);
        }
        else
        {
            // If NavMeshAgent is not available, use movement component
            if (enemyMovement != null && enemyMovement.Target != null)
            {
                float targetSpeed = enemyMovement.GetMoveSpeed();
                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, speedSmoothing * Time.deltaTime);
                animator.SetFloat(speedParameter, currentSpeed);
            }
            else
            {
                // No target, idle
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, speedSmoothing * Time.deltaTime);
                animator.SetFloat(speedParameter, currentSpeed);
            }
        }
    }
    
    /// <summary>
    /// Called when the enemy takes damage.
    /// </summary>
    private void OnDamageTaken(float damage)
    {
        if (animator != null && !string.IsNullOrEmpty(takeDamageParameter))
        {
            animator.SetTrigger(takeDamageParameter);
        }
    }
    
    /// <summary>
    /// Called when the enemy dies.
    /// </summary>
    private void OnDeath()
    {
        if (animator != null)
        {
            animator.SetBool(isDeadParameter, true);
            animator.SetFloat(speedParameter, 0f);
        }
    }
    
    /// <summary>
    /// Manually trigger a damage animation (useful for external calls).
    /// </summary>
    public void TriggerDamageAnimation()
    {
        if (animator != null && !string.IsNullOrEmpty(takeDamageParameter))
        {
            animator.SetTrigger(takeDamageParameter);
        }
    }
}

