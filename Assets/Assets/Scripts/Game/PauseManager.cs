using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages game pause state. Pauses/unpauses the game when Escape is pressed.
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputActionAsset inputActionsAsset;

    [Header("Pause Settings")]
    [SerializeField] private bool pauseOnStart = false;
    
    [Header("Pause Menu UI")]
    [Tooltip("The Canvas or GameObject containing the pause menu UI. Will be shown when paused and hidden when unpaused.")]
    [SerializeField] private GameObject pauseMenuCanvas;

    private InputActionMap uiActionMap;
    private InputAction cancelAction;
    private bool isPaused = false;

    /// <summary>
    /// Whether the game is currently paused.
    /// </summary>
    public bool IsPaused => isPaused;

    /// <summary>
    /// Event that is called when the pause state changes. Passes true if paused, false if unpaused.
    /// </summary>
    public System.Action<bool> OnPauseStateChanged;

    /// <summary>
    /// Singleton instance for easy access from other scripts.
    /// </summary>
    public static PauseManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple PauseManager instances found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        // Initialize input actions
        if (inputActionsAsset != null)
        {
            uiActionMap = inputActionsAsset.FindActionMap("UI");
            if (uiActionMap != null)
            {
                cancelAction = uiActionMap.FindAction("Cancel");
                if (cancelAction == null)
                {
                    Debug.LogWarning("Cancel action not found in UI action map!");
                }
            }
            else
            {
                Debug.LogWarning("UI action map not found in InputActionAsset!");
            }
        }
        else
        {
            Debug.LogWarning("InputActionAsset is not assigned in PauseManager!");
        }
    }

    private void OnEnable()
    {
        // Enable input actions
        if (inputActionsAsset != null)
        {
            inputActionsAsset.Enable();
        }

        if (uiActionMap != null)
        {
            uiActionMap.Enable();
        }

        if (cancelAction != null)
        {
            cancelAction.performed += OnCancelPressed;
        }

        // Set initial pause state
        if (pauseOnStart)
        {
            Pause();
        }
        else
        {
            Unpause();
        }
        
        // Ensure pause menu starts in correct state
        UpdatePauseMenuVisibility();
    }

    private void OnDisable()
    {
        if (cancelAction != null)
        {
            cancelAction.performed -= OnCancelPressed;
        }

        if (uiActionMap != null)
        {
            uiActionMap.Disable();
        }

        if (inputActionsAsset != null)
        {
            inputActionsAsset.Disable();
        }
    }

    private void OnCancelPressed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Toggles the pause state.
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            Unpause();
        }
        else
        {
            Pause();
        }
    }

    /// <summary>
    /// Pauses the game.
    /// </summary>
    public void Pause()
    {
        if (isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;

        // Unlock and show cursor when paused
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Show pause menu
        UpdatePauseMenuVisibility();

        OnPauseStateChanged?.Invoke(true);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Game Paused");
        #endif
    }

    /// <summary>
    /// Unpauses the game.
    /// </summary>
    public void Unpause()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        // Lock and hide cursor when unpaused
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Hide pause menu
        UpdatePauseMenuVisibility();

        OnPauseStateChanged?.Invoke(false);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Game Unpaused");
        #endif
    }

    /// <summary>
    /// Updates the visibility of the pause menu based on the current pause state.
    /// </summary>
    private void UpdatePauseMenuVisibility()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(isPaused);
        }
    }

    private void OnDestroy()
    {
        // Ensure time scale is reset when destroyed
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }
}

