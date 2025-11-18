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
    /// </summary>
    private IEnumerator SpawnWaveEnemies()
    {
        isSpawning = true;

        while (enemiesSpawned < enemiesToSpawn)
        {
            int spawnsThisBatch = Mathf.Min(maxConcurrentSpawns, enemiesToSpawn - enemiesSpawned);

            for (int i = 0; i < spawnsThisBatch; i++)
            {
                SpawnEnemy();
            }

            enemiesSpawned += spawnsThisBatch;

            // Wait before spawning next batch
            if (enemiesSpawned < enemiesToSpawn)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        isSpawning = false;
    }

    /// <summary>
    /// Spawns a single enemy at a random enabled spawn point.
    /// </summary>
    private void SpawnEnemy()
    {
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

        // Get a random enabled spawn point
        EnemySpawnPoint spawnPoint = spawnGrid.GetRandomEnabledSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("WaveManager: No enabled spawn points available!");
            return;
        }

        // Pick a random enemy prefab
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        if (enemyPrefab == null)
        {
            Debug.LogWarning("WaveManager: Enemy prefab is null!");
            return;
        }

        // Spawn the enemy
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.Position, spawnPoint.Rotation);
        activeEnemies.Add(enemy);
        enemiesAlive++;

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

        // Check if wave is complete
        if (waitForAllEnemiesKilled && !isSpawning && enemiesAlive <= 0)
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
        if (!waitForAllEnemiesKilled && isWaveActive && !isSpawning && enemiesAlive <= 0)
        {
            CompleteWave();
        }
    }
}

