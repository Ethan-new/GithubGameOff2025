using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages enemy spawn points organized by rooms. Each room is a GameObject that contains
/// multiple spawn points. Allows enabling/disabling spawn points and provides methods
/// to get random enabled spawn points for wave-based spawning.
/// </summary>
public class EnemySpawnGrid : MonoBehaviour
{
    [Header("Rooms")]
    [SerializeField] private GameObject[] rooms;

    private List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();
    private bool isInitialized = false;

    /// <summary>
    /// All spawn points from all rooms.
    /// </summary>
    public List<EnemySpawnPoint> SpawnPoints => spawnPoints;

    /// <summary>
    /// The number of enabled spawn points.
    /// </summary>
    public int EnabledSpawnPointCount
    {
        get
        {
            int count = 0;
            foreach (var point in spawnPoints)
            {
                if (point != null && point.IsEnabled)
                    count++;
            }
            return count;
        }
    }

    /// <summary>
    /// The total number of spawn points in all rooms.
    /// </summary>
    public int TotalSpawnPointCount => spawnPoints.Count;

    private void Awake()
    {
        CollectSpawnPoints();
    }

    /// <summary>
    /// Collects all EnemySpawnPoint components from all rooms.
    /// </summary>
    public void CollectSpawnPoints()
    {
        spawnPoints.Clear();

        if (rooms == null || rooms.Length == 0)
        {
            isInitialized = true;
            return;
        }

        foreach (var room in rooms)
        {
            if (room != null)
            {
                EnemySpawnPoint[] points = room.GetComponentsInChildren<EnemySpawnPoint>();
                spawnPoints.AddRange(points);
            }
        }

        isInitialized = true;
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Collected {spawnPoints.Count} spawn points from {rooms.Length} rooms");
        #endif
    }

    /// <summary>
    /// Gets a random enabled spawn point. Returns null if no enabled spawn points exist.
    /// </summary>
    /// <returns>A random enabled spawn point, or null if none are available.</returns>
    public EnemySpawnPoint GetRandomEnabledSpawnPoint()
    {
        if (!isInitialized)
            CollectSpawnPoints();

        List<EnemySpawnPoint> enabledPoints = GetEnabledSpawnPoints();
        if (enabledPoints.Count == 0)
            return null;

        return enabledPoints[Random.Range(0, enabledPoints.Count)];
    }

    /// <summary>
    /// Gets all enabled spawn points.
    /// </summary>
    /// <returns>A list of all enabled spawn points.</returns>
    public List<EnemySpawnPoint> GetEnabledSpawnPoints()
    {
        if (!isInitialized)
            CollectSpawnPoints();

        List<EnemySpawnPoint> enabledPoints = new List<EnemySpawnPoint>();
        foreach (var point in spawnPoints)
        {
            if (point != null && point.IsEnabled)
            {
                enabledPoints.Add(point);
            }
        }
        return enabledPoints;
    }

    /// <summary>
    /// Gets all disabled spawn points.
    /// </summary>
    /// <returns>A list of all disabled spawn points.</returns>
    public List<EnemySpawnPoint> GetDisabledSpawnPoints()
    {
        if (!isInitialized)
            CollectSpawnPoints();

        List<EnemySpawnPoint> disabledPoints = new List<EnemySpawnPoint>();
        foreach (var point in spawnPoints)
        {
            if (point != null && !point.IsEnabled)
            {
                disabledPoints.Add(point);
            }
        }
        return disabledPoints;
    }

    /// <summary>
    /// Enables all spawn points in all rooms.
    /// </summary>
    public void EnableAllSpawnPoints()
    {
        foreach (var point in spawnPoints)
        {
            if (point != null)
                point.Enable();
        }
    }

    /// <summary>
    /// Disables all spawn points in all rooms.
    /// </summary>
    public void DisableAllSpawnPoints()
    {
        foreach (var point in spawnPoints)
        {
            if (point != null)
                point.Disable();
        }
    }

    /// <summary>
    /// Enables a specific spawn point by index.
    /// </summary>
    /// <param name="index">The index of the spawn point to enable.</param>
    public void EnableSpawnPoint(int index)
    {
        if (index >= 0 && index < spawnPoints.Count && spawnPoints[index] != null)
        {
            spawnPoints[index].Enable();
        }
    }

    /// <summary>
    /// Disables a specific spawn point by index.
    /// </summary>
    /// <param name="index">The index of the spawn point to disable.</param>
    public void DisableSpawnPoint(int index)
    {
        if (index >= 0 && index < spawnPoints.Count && spawnPoints[index] != null)
        {
            spawnPoints[index].Disable();
        }
    }

    /// <summary>
    /// Toggles a specific spawn point by index.
    /// </summary>
    /// <param name="index">The index of the spawn point to toggle.</param>
    public void ToggleSpawnPoint(int index)
    {
        if (index >= 0 && index < spawnPoints.Count && spawnPoints[index] != null)
        {
            spawnPoints[index].Toggle();
        }
    }
}
