using UnityEngine;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

/// <summary>
/// UI component that displays the current wave on screen.
/// Attach this to a GameObject with a TextMeshProUGUI component to display the wave.
/// </summary>
public class WaveUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private string wavePrefix = "Wave: ";
    [SerializeField] private bool showPrefix = true;

    [Header("Text Component (Auto-detected if not assigned)")]
    [Tooltip("TextMeshProUGUI component to display the wave. Drag your TextMeshPro component here or use 'Auto-Find Text Component' from the context menu.")]
    [SerializeField] 
#if UNITY_TEXTMESHPRO
    private TextMeshProUGUI waveTextTMP;
#else
    private UnityEngine.Object waveTextTMP; // Using Object type so it can be assigned even without TextMeshPro package
#endif
    
    private WaveManager waveManager;

    private void Awake()
    {
        // Try to find text components - will try again in Start() if not found
#if UNITY_TEXTMESHPRO
        if (waveTextTMP == null)
        {
            FindTextComponent();
        }
#else
        if (waveTextTMP == null)
        {
            FindTextComponent();
        }
#endif
    }

    private void OnValidate()
    {
        // In editor, try to auto-find if not assigned
        #if UNITY_EDITOR
#if UNITY_TEXTMESHPRO
        if (waveTextTMP == null)
        {
            FindTextComponent();
        }
#else
        if (waveTextTMP == null)
        {
            FindTextComponent();
        }
#endif
        #endif
    }

    private void Start()
    {
        // Try to find text component again in case it was added after Awake()
#if UNITY_TEXTMESHPRO
        if (waveTextTMP == null)
        {
            FindTextComponent();
        }

        // Warn if still no text component found
        if (waveTextTMP == null)
        {
            Debug.LogWarning("WaveUI: No TextMeshProUGUI component found. Please add a TextMeshProUGUI component to this GameObject or use the context menu 'Auto-Find Text Component'.", this);
        }
#else
        if (waveTextTMP == null)
        {
            FindTextComponent();
        }

        // Warn if still no text component found
        if (waveTextTMP == null)
        {
            Debug.LogWarning("WaveUI: No TextMeshProUGUI component found. Please add a TextMeshProUGUI component to this GameObject or use the context menu 'Auto-Find Text Component'.", this);
        }
#endif

        // Find WaveManager - prefer singleton instance
        if (WaveManager.Instance != null)
        {
            waveManager = WaveManager.Instance;
        }
        else
        {
            // Fallback: find in scene if singleton not initialized yet
            waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager == null)
            {
                Debug.LogWarning("WaveUI: No WaveManager found in scene. Wave will not update.", this);
                return;
            }
        }

        // Subscribe to wave changes
        if (waveManager != null)
        {
            waveManager.OnWaveStart += UpdateWaveDisplay;
            // Initialize display with current wave
            UpdateWaveDisplay(waveManager.CurrentWave);
        }
    }

    /// <summary>
    /// Finds and caches the Text or TextMeshProUGUI component.
    /// Can be called from inspector context menu.
    /// </summary>
    [ContextMenu("Auto-Find Text Component")]
    private void FindTextComponent()
    {
#if UNITY_TEXTMESHPRO
        // Try the standard way if define is set
        if (waveTextTMP == null)
        {
            waveTextTMP = GetComponent<TextMeshProUGUI>();
        }
        if (waveTextTMP == null)
        {
            waveTextTMP = GetComponentInChildren<TextMeshProUGUI>(true); // Include inactive
        }
        if (waveTextTMP == null)
        {
            waveTextTMP = GetComponentInParent<TextMeshProUGUI>(true); // Include inactive
        }
        // Last resort: search in the entire scene
        if (waveTextTMP == null)
        {
            TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            // Prefer one that's on the same GameObject or a child
            foreach (TextMeshProUGUI text in allTexts)
            {
                if (text.transform == transform || text.transform.IsChildOf(transform))
                {
                    waveTextTMP = text;
                    break;
                }
            }
            // If still not found, just use the first one (user can reassign if wrong)
            if (waveTextTMP == null && allTexts.Length > 0)
            {
                waveTextTMP = allTexts[0];
                Debug.LogWarning($"WaveUI: Auto-assigned TextMeshProUGUI component from '{waveTextTMP.gameObject.name}'. If this is wrong, please manually assign the correct Text component.", this);
            }
        }
        
        // Log success for TextMeshPro
        if (waveTextTMP != null)
        {
            Debug.Log($"WaveUI: Found TextMeshProUGUI component on '{waveTextTMP.gameObject.name}'", this);
            return; // Found TextMeshPro, don't look for standard Text
        }
#else
        // Try to find TextMeshProUGUI using reflection (even if package isn't imported)
        System.Type tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType != null)
        {
            Component tmpComponent = GetComponent(tmpType);
            if (tmpComponent == null)
            {
                tmpComponent = GetComponentInChildren(tmpType, true);
            }
            if (tmpComponent == null)
            {
                tmpComponent = GetComponentInParent(tmpType, true);
            }
            if (tmpComponent != null)
            {
                waveTextTMP = tmpComponent;
                Debug.Log($"WaveUI: Found TextMeshProUGUI component on '{tmpComponent.gameObject.name}' (using reflection)", this);
                return;
            }
        }
#endif

    }

    private void OnDestroy()
    {
        // Unsubscribe from wave changes
        if (waveManager != null)
        {
            waveManager.OnWaveStart -= UpdateWaveDisplay;
        }
    }

    /// <summary>
    /// Updates the wave display text.
    /// </summary>
    /// <param name="wave">The new wave number.</param>
    private void UpdateWaveDisplay(int wave)
    {
        // Cache wave string to avoid allocations - only update if wave changed
        string waveString = showPrefix ? string.Concat(wavePrefix, wave) : wave.ToString();

#if UNITY_TEXTMESHPRO
        if (waveTextTMP != null)
        {
            waveTextTMP.text = waveString;
        }
#else
        // Try to use TextMeshPro even if define isn't set (using reflection)
        if (waveTextTMP != null)
        {
            var textProperty = waveTextTMP.GetType().GetProperty("text");
            if (textProperty != null)
            {
                textProperty.SetValue(waveTextTMP, waveString);
            }
        }
#endif
    }
}

