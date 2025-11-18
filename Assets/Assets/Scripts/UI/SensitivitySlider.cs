using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component that controls mouse sensitivity via a slider.
/// Attach this to a GameObject with a Slider component to allow players to adjust sensitivity.
/// </summary>
public class SensitivitySlider : MonoBehaviour
{
    [Header("Slider Settings")]
    [Tooltip("The slider component. Auto-detected if not assigned.")]
    [SerializeField] private Slider sensitivitySlider;
    
    [Header("Sensitivity Range")]
    [Tooltip("Minimum sensitivity value.")]
    [SerializeField] private float minSensitivity = 0.5f;
    
    [Tooltip("Maximum sensitivity value.")]
    [SerializeField] private float maxSensitivity = 5f;

    private PlayerController playerController;

    private void Awake()
    {
        // Auto-find slider if not assigned
        if (sensitivitySlider == null)
        {
            sensitivitySlider = GetComponent<Slider>();
            if (sensitivitySlider == null)
            {
                Debug.LogWarning("SensitivitySlider: No Slider component found. Please add a Slider component or assign one manually.", this);
            }
        }
    }

    private void Start()
    {
        // Find PlayerController
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("SensitivitySlider: PlayerController not found in scene.", this);
            return;
        }

        // Initialize slider
        if (sensitivitySlider != null)
        {
            // Set slider range
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;

            // Load saved sensitivity from PlayerSettings, or use current value
            float currentSensitivity;
            if (PlayerSettings.Instance != null)
            {
                currentSensitivity = PlayerSettings.Instance.GetMouseSensitivity();
            }
            else
            {
                currentSensitivity = playerController.GetMouseSensitivity();
            }
            
            sensitivitySlider.value = Mathf.Clamp(currentSensitivity, minSensitivity, maxSensitivity);

            // Subscribe to slider value changes
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from slider events
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        }
    }

    /// <summary>
    /// Called when the slider value changes.
    /// </summary>
    private void OnSensitivityChanged(float value)
    {
        if (playerController != null)
        {
            playerController.SetMouseSensitivity(value);
        }

        // Save to PlayerSettings if available
        if (PlayerSettings.Instance != null)
        {
            PlayerSettings.Instance.SetMouseSensitivity(value);
        }
    }

    /// <summary>
    /// Manually sets the slider value (useful for external control).
    /// </summary>
    public void SetSliderValue(float value)
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = Mathf.Clamp(value, minSensitivity, maxSensitivity);
        }
    }
}

