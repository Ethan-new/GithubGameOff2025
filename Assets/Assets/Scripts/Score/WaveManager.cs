using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages waves of enemies in a Call of Duty zombies-style system.
/// Spawns enemies progressively, tracks wave completion, and scales difficulty.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private int startingWave = 1;
    [SerializeField] private float delayBetweenWaves = 5f;
    [SerializeField] private bool autoStartWaves = true;

    [Header("Enemy Spawning")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private EnemySpawnGrid spawnGrid;
    [SerializeField] private int baseEnemiesPerWave = 10;
    [SerializeField] private float enemiesPerWaveMultiplier = 1.2f; // Each wave increases by this multiplier
    [SerializeField] private float spawnInterval = 0.5f; // Time between individual enemy spawns
    [SerializeField] private int maxConcurrentSpawns = 3; // Max enemies spawning at once
    [SerializeField] private int maxEnemiesAlive = 10; // Maximum number of enemies that can be alive at once
    [SerializeField] private float minSpawnDistance = 2f; // Minimum distance from other enemies when spawning
    [SerializeField] private float spawnOffsetRadius = 0.5f; // Random offset radius to prevent exact overlaps

    [Header("Difficulty Scaling")]
    [SerializeField] private float healthMultiplierPerWave = 1.1f; // Enemy health increases by this per wave
    [SerializeField] private float speedMultiplierPerWave = 1.05f; // Enemy speed increases by this per wave
    [SerializeField] private bool scaleEnemyHealth = true;
    [SerializeField] private bool scaleEnemySpeed = true;

    [Header("Wave Completion")]
    [SerializeField] private bool waitForAllEnemiesKilled = true;

    private int currentWave = 0;
    private int enemiesToSpawn = 0;
    private int enemiesSpawned = 0;
    private int enemiesAlive = 0;
    private bool isWaveActive = false;
    private bool isSpawning = false;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Dictionary<GameObject, System.Action> enemyDeathHandlers = new Dictionary<GameObject, System.Action>();

    /// <summary>
    /// The current wave number.
    /// </summary>
    public int CurrentWave => currentWave;

    /// <summary>
    /// Whether a wave is currently active.
    /// </summary>
    public bool IsWaveActive => isWaveActive;

    /// <summary>
    /// Number of enemies currently alive.
    /// </summary>
    public int EnemiesAlive => enemiesAlive;

    /// <summary>
    /// Event called when a new wave starts. Passes the wave number.
    /// </summary>
    public System.Action<int> OnWaveStart;

    /// <summary>
    /// Event called when a wave completes. Passes the wave number that just completed.
    /// </summary>
    public System.Action<int> OnWaveComplete;

    /// <summary>
    /// Event called when an enemy is spawned. Passes the spawned enemy GameObject.
    /// </summary>
    public System.Action<GameObject> OnEnemySpawned;

    /// <summary>
    /// Event called when an enemy dies. Passes the dead enemy GameObject.
    /// </summary>
    public System.Action<GameObject> OnEnemyDied;

    /// <summary>
    /// Singleton instance for easy access from other scripts.
    /// </summary>
    public static WaveManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple WaveManager instances found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        currentWave = startingWave - 1;
    }

    private void Start()
    {
        if (spawnGrid == null)
        {
            Debug.LogError("WaveManager: No EnemySpawnGrid assigned! Please assign one in the Inspector.");
        }

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("WaveManager: No enemy prefabs assigned! Please assign at least one enemy prefab in the Inspector.");
        }

        if (autoStartWaves)
        {
            StartNextWave();
        }
    }

    /// <summary>
    /// Starts the next wave.
    /// </summary>
    public void StartNextWave()
    {
        if (isWaveActive)
        {
            Debug.LogWarning("WaveManager: Cannot start next wave while a wave is still active!");
            return;
        }

        currentWave++;
        CalculateWaveEnemies();
        isWaveActive = true;
        enemiesSpawned = 0;
        enemiesAlive = 0;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Wave {currentWave} starting! Spawning {enemiesToSpawn} enemies.");
        #endif

        OnWaveStart?.Invoke(currentWave);
        StartCoroutine(SpawnWaveEnemies());
    }

    /// <summary>
    /// Calculates how many enemies should spawn in the current wave.
    /// </summary>
    private void CalculateWaveEnemies()
    {
        // Formula: baseEnemies * (multiplier ^ (wave - 1))
        // This creates exponential growth like CoD zombies
        enemiesToSpawn = Mathf.RoundToInt(baseEnemiesPerWave * Mathf.Pow(enemiesPerWaveMultiplier, currentWave - 1));
    }

    /// <summary>
    /// Coroutine that spawns all enemies for the current wave.
    /// Spawns enemies slowly over time, respecting the max enemies alive limit.
    /// </summary>
    private IEnumerator SpawnWaveEnemies()
    {
        isSpawning = true;

        while (enemiesSpawned < enemiesToSpawn)
        {
            // Calculate how many enemies we can spawn in this batch
            // Don't spawn more than maxConcurrentSpawns at once
            // Don't spawn more than remaining enemies to spawn
            // Don't spawn if we're at the max alive limit
            int availableSlots = maxEnemiesAlive - enemiesAlive;
            int remainingToSpawn = enemiesToSpawn - enemiesSpawned;
            int spawnsThisBatch = Mathf.Min(maxConcurrentSpawns, remainingToSpawn, availableSlots);

            if (spawnsThisBatch > 0)
            {
                for (int i = 0; i < spawnsThisBatch; i++)
                {
                    SpawnEnemy();
                }
            }

            // Wait before checking again
            yield return new WaitForSeconds(spawnInterval);
        }

        isSpawning = false;
    }

    /// <summary>
    /// Finds a spawn point that doesn't have enemies too close to it.
    /// Tries multiple spawn points to find a safe one.
    /// </summary>
    private EnemySpawnPoint FindSafeSpawnPoint()
    {
        if (spawnGrid == null)
            return null;

        List<EnemySpawnPoint> enabledPoints = spawnGrid.GetEnabledSpawnPoints();
        if (enabledPoints == null || enabledPoints.Count == 0)
            return null;

        // Try up to 10 random spawn points to find a safe one
        int attempts = Mathf.Min(10, enabledPoints.Count);
        for (int i = 0; i < attempts; i++)
        {
            EnemySpawnPoint candidate = enabledPoints[Random.Range(0, enabledPoints.Count)];
            
            // Check if there are any enemies too close to this spawn point
            bool isSafe = true;
            foreach (GameObject enemy in activeEnemies)
            {
                if (enemy == null)
                    continue;

                float distance = Vector3.Distance(candidate.Position, enemy.transform.position);
                if (distance < minSpawnDistance)
                {
                    isSafe = false;
                    break;
                }
            }

            if (isSafe)
            {
                return candidate;
            }
        }

        // If no safe spawn point found, return a random one anyway
        // (better than not spawning at all)
        return enabledPoints[Random.Range(0, enabledPoints.Count)];
    }

    /// <summary>
    /// Spawns a single enemy at a random enabled spawn point.
    /// </summary>
    private void SpawnEnemy()
    {
        // Safety check: Don't spawn more enemies than intended for this wave
        if (enemiesSpawned >= enemiesToSpawn)
        {
            return;
        }

        if (spawnGrid == null)
        {
            Debug.LogError("WaveManager: Cannot spawn enemy - no spawn grid assigned!");
            return;
        }

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("WaveManager: Cannot spawn enemy - no enemy prefabs assigned!");
            return;
        }

        // Find a safe spawn point (one without enemies too close)
        EnemySpawnPoint spawnPoint = FindSafeSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("WaveManager: No safe spawn points available!");
            return;
        }

        // Pick a random enemy prefab
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        if (enemyPrefab == null)
        {
            Debug.LogWarning("WaveManager: Enemy prefab is null!");
            return;
        }

        // Calculate spawn position with small random offset to prevent exact overlaps
        Vector3 spawnPosition = spawnPoint.Position;
        Vector2 randomOffset = Random.insideUnitCircle * spawnOffsetRadius;
        spawnPosition += new Vector3(randomOffset.x, 0f, randomOffset.y);

        // Spawn the enemy
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, spawnPoint.Rotation);
        activeEnemies.Add(enemy);
        enemiesAlive++;
        enemiesSpawned++; // Track total enemies spawned for this wave

        // Scale enemy stats based on wave
        ScaleEnemyForWave(enemy);

        // Subscribe to enemy death
        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            // Store handler reference for proper cleanup
            System.Action deathHandler = () => OnEnemyDeath(enemy);
            enemyHealth.OnDeath += deathHandler;
            enemyDeathHandlers[enemy] = deathHandler;
        }

        OnEnemySpawned?.Invoke(enemy);
    }

    /// <summary>
    /// Scales enemy health and speed based on the current wave.
    /// </summary>
    private void ScaleEnemyForWave(GameObject enemy)
    {
        if (currentWave <= 1)
            return; // First wave uses base stats

        // Scale health
        if (scaleEnemyHealth)
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                float healthMultiplier = Mathf.Pow(healthMultiplierPerWave, currentWave - 1);
                enemyHealth.ScaleMaxHealth(healthMultiplier);
            }
        }

        // Scale speed
        if (scaleEnemySpeed)
        {
            EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
            if (enemyMovement != null)
            {
                float speedMultiplier = Mathf.Pow(speedMultiplierPerWave, currentWave - 1);
                enemyMovement.ScaleMoveSpeed(speedMultiplier);
            }
        }
    }

    /// <summary>
    /// Called when an enemy dies.
    /// </summary>
    private void OnEnemyDeath(GameObject enemy)
    {
        if (!activeEnemies.Contains(enemy))
            return;

        activeEnemies.Remove(enemy);
        enemiesAlive--;

        // Unsubscribe from enemy death event to prevent memory leaks
        if (enemy != null && enemyDeathHandlers.ContainsKey(enemy))
        {
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                System.Action handler = enemyDeathHandlers[enemy];
                enemyHealth.OnDeath -= handler;
            }
            enemyDeathHandlers.Remove(enemy);
        }

        OnEnemyDied?.Invoke(enemy);

        // Spawn a new enemy if there are still enemies left to spawn for this wave
        // and we're below the max alive limit, but only while actively spawning
        // (The coroutine will handle spawning, but we can help fill slots as enemies die)
        if (isWaveActive && isSpawning && enemiesSpawned < enemiesToSpawn && enemiesAlive < maxEnemiesAlive)
        {
            SpawnEnemy();
        }

        // Check if wave is complete
        // Wave completes when all enemies have been spawned AND all are killed
        if (waitForAllEnemiesKilled && enemiesSpawned >= enemiesToSpawn && enemiesAlive <= 0)
        {
            CompleteWave();
        }
    }

    /// <summary>
    /// Completes the current wave and starts the next one after a delay.
    /// </summary>
    private void CompleteWave()
    {
        if (!isWaveActive)
            return;

        isWaveActive = false;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Wave {currentWave} complete!");
        #endif

        OnWaveComplete?.Invoke(currentWave);

        // Start next wave after delay
        StartCoroutine(DelayNextWave());
    }

    /// <summary>
    /// Coroutine that waits before starting the next wave.
    /// </summary>
    private IEnumerator DelayNextWave()
    {
        yield return new WaitForSeconds(delayBetweenWaves);
        StartNextWave();
    }

    /// <summary>
    /// Manually completes the current wave (useful for testing or special conditions).
    /// </summary>
    public void ForceCompleteWave()
    {
        if (isWaveActive)
        {
            CompleteWave();
        }
    }

    /// <summary>
    /// Resets the wave manager to the starting wave.
    /// </summary>
    public void ResetWaves()
    {
        // Kill all active enemies
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.Kill();
                }
                else
                {
                    Destroy(enemy);
                }
            }
        }

        activeEnemies.Clear();
        enemiesAlive = 0;
        enemiesSpawned = 0;
        isWaveActive = false;
        isSpawning = false;
        currentWave = startingWave - 1;
        
        // Clean up all event subscriptions
        foreach (var kvp in enemyDeathHandlers)
        {
            if (kvp.Key != null)
            {
                EnemyHealth enemyHealth = kvp.Key.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.OnDeath -= kvp.Value;
                }
            }
        }
        enemyDeathHandlers.Clear();

        StopAllCoroutines();
    }

    private void Update()
    {
        // Clean up null references from destroyed enemies
        activeEnemies.RemoveAll(enemy => enemy == null);

        // Check for wave completion if not waiting for all enemies killed
        // Wave completes when all enemies have been spawned AND all are killed
        if (!waitForAllEnemiesKilled && isWaveActive && enemiesSpawned >= enemiesToSpawn && enemiesAlive <= 0)
        {
            CompleteWave();
        }
    }
}

