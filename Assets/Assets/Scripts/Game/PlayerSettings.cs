using UnityEngine;

/// <summary>
/// Manages player settings and preferences. Saves and loads settings using PlayerPrefs.
/// </summary>
public class PlayerSettings : MonoBehaviour
{
    [Header("Default Settings")]
    [SerializeField] private float defaultMouseSensitivity = 2f;

    // PlayerPrefs keys
    private const string KEY_MOUSE_SENSITIVITY = "MouseSensitivity";

    /// <summary>
    /// Singleton instance for easy access from other scripts.
    /// </summary>
    public static PlayerSettings Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // Load settings when created
            LoadSettings();
        }
        else
        {
            Debug.LogWarning("Multiple PlayerSettings instances found. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Gets the saved mouse sensitivity, or returns the default if not set.
    /// </summary>
    public float GetMouseSensitivity()
    {
        if (PlayerPrefs.HasKey(KEY_MOUSE_SENSITIVITY))
        {
            return PlayerPrefs.GetFloat(KEY_MOUSE_SENSITIVITY);
        }
        return defaultMouseSensitivity;
    }

    /// <summary>
    /// Sets and saves the mouse sensitivity.
    /// </summary>
    public void SetMouseSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat(KEY_MOUSE_SENSITIVITY, sensitivity);
        PlayerPrefs.Save(); // Save immediately
    }

    /// <summary>
    /// Loads all player settings and applies them to the game.
    /// </summary>
    public void LoadSettings()
    {
        // Load sensitivity and apply to PlayerController
        float sensitivity = GetMouseSensitivity();
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.SetMouseSensitivity(sensitivity);
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Loaded player settings - Sensitivity: {sensitivity}");
        #endif
    }

    /// <summary>
    /// Saves all current player settings.
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.Save();
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Player settings saved");
        #endif
    }

    /// <summary>
    /// Resets all settings to default values.
    /// </summary>
    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(KEY_MOUSE_SENSITIVITY);
        // Add more DeleteKey calls for other settings as needed
        
        LoadSettings(); // Reload with defaults
        SaveSettings();
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Player settings reset to defaults");
        #endif
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Save settings when application is paused (e.g., mobile)
        if (pauseStatus)
        {
            SaveSettings();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // Save settings when application loses focus
        if (!hasFocus)
        {
            SaveSettings();
        }
    }

    private void OnDestroy()
    {
        // Save settings when destroyed
        SaveSettings();
    }
}

