using UnityEngine;

/// <summary>
/// Represents a single enemy spawn point in the spawn grid.
/// Can be enabled or disabled to control whether enemies can spawn here.
/// </summary>
public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private bool isEnabled = true;

    /// <summary>
    /// Whether this spawn point is currently enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => isEnabled;
        set => isEnabled = value;
    }

    /// <summary>
    /// The world position of this spawn point.
    /// </summary>
    public Vector3 Position => transform.position;

    /// <summary>
    /// The rotation of this spawn point (useful for spawning enemies facing a specific direction).
    /// </summary>
    public Quaternion Rotation => transform.rotation;

    /// <summary>
    /// Enables this spawn point.
    /// </summary>
    public void Enable()
    {
        isEnabled = true;
    }

    /// <summary>
    /// Disables this spawn point.
    /// </summary>
    public void Disable()
    {
        isEnabled = false;
    }

    /// <summary>
    /// Toggles the enabled state of this spawn point.
    /// </summary>
    public void Toggle()
    {
        isEnabled = !isEnabled;
    }
}

