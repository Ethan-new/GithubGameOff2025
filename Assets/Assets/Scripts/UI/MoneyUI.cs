using UnityEngine;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

/// <summary>
/// UI component that displays the player's money on screen.
/// Attach this to a GameObject with a TextMeshProUGUI component to display the money.
/// </summary>
public class MoneyUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private string moneyPrefix = "$";
    [SerializeField] private bool showPrefix = true;

    [Header("Text Component (Auto-detected if not assigned)")]
    [Tooltip("TextMeshProUGUI component to display the money. Drag your TextMeshPro component here or use 'Auto-Find Text Component' from the context menu.")]
    [SerializeField] 
#if UNITY_TEXTMESHPRO
    private TextMeshProUGUI moneyTextTMP;
#else
    private UnityEngine.Object moneyTextTMP; // Using Object type so it can be assigned even without TextMeshPro package
#endif
    
    private MoneyManager moneyManager;

    private void Awake()
    {
        // Try to find text components - will try again in Start() if not found
#if UNITY_TEXTMESHPRO
        if (moneyTextTMP == null)
        {
            FindTextComponent();
        }
#else
        if (moneyTextTMP == null)
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
        if (moneyTextTMP == null)
        {
            FindTextComponent();
        }
#else
        if (moneyTextTMP == null)
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
        if (moneyTextTMP == null)
        {
            FindTextComponent();
        }

        // Warn if still no text component found
        if (moneyTextTMP == null)
        {
            Debug.LogWarning("MoneyUI: No TextMeshProUGUI component found. Please add a TextMeshProUGUI component to this GameObject or use the context menu 'Auto-Find Text Component'.", this);
        }
#else
        if (moneyTextTMP == null)
        {
            FindTextComponent();
        }

        // Warn if still no text component found
        if (moneyTextTMP == null)
        {
            Debug.LogWarning("MoneyUI: No TextMeshProUGUI component found. Please add a TextMeshProUGUI component to this GameObject or use the context menu 'Auto-Find Text Component'.", this);
        }
#endif

        // Find MoneyManager - prefer singleton instance
        if (MoneyManager.Instance != null)
        {
            moneyManager = MoneyManager.Instance;
        }
        else
        {
            // Fallback: find in scene if singleton not initialized yet
            moneyManager = FindFirstObjectByType<MoneyManager>();
            if (moneyManager == null)
            {
                Debug.LogWarning("MoneyUI: No MoneyManager found in scene. Money will not update.", this);
                return;
            }
        }

        // Subscribe to money changes
        if (moneyManager != null)
        {
            moneyManager.OnMoneyChanged += UpdateMoneyDisplay;
            // Initialize display with current money
            UpdateMoneyDisplay(moneyManager.CurrentMoney);
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
        if (moneyTextTMP == null)
        {
            moneyTextTMP = GetComponent<TextMeshProUGUI>();
        }
        if (moneyTextTMP == null)
        {
            moneyTextTMP = GetComponentInChildren<TextMeshProUGUI>(true); // Include inactive
        }
        if (moneyTextTMP == null)
        {
            moneyTextTMP = GetComponentInParent<TextMeshProUGUI>(true); // Include inactive
        }
        // Last resort: search in the entire scene
        if (moneyTextTMP == null)
        {
            TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            // Prefer one that's on the same GameObject or a child
            foreach (TextMeshProUGUI text in allTexts)
            {
                if (text.transform == transform || text.transform.IsChildOf(transform))
                {
                    moneyTextTMP = text;
                    break;
                }
            }
            // If still not found, just use the first one (user can reassign if wrong)
            if (moneyTextTMP == null && allTexts.Length > 0)
            {
                moneyTextTMP = allTexts[0];
                Debug.LogWarning($"MoneyUI: Auto-assigned TextMeshProUGUI component from '{moneyTextTMP.gameObject.name}'. If this is wrong, please manually assign the correct Text component.", this);
            }
        }
        
        // Log success for TextMeshPro
        if (moneyTextTMP != null)
        {
            Debug.Log($"MoneyUI: Found TextMeshProUGUI component on '{moneyTextTMP.gameObject.name}'", this);
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
                moneyTextTMP = tmpComponent;
                Debug.Log($"MoneyUI: Found TextMeshProUGUI component on '{tmpComponent.gameObject.name}' (using reflection)", this);
                return;
            }
        }
#endif

    }

    private void OnDestroy()
    {
        // Unsubscribe from money changes
        if (moneyManager != null)
        {
            moneyManager.OnMoneyChanged -= UpdateMoneyDisplay;
        }
    }

    /// <summary>
    /// Updates the money display text.
    /// </summary>
    /// <param name="money">The new money value.</param>
    private void UpdateMoneyDisplay(int money)
    {
        // Cache money string to avoid allocations - only update if money changed
        string moneyString = showPrefix ? string.Concat(moneyPrefix, money) : money.ToString();

#if UNITY_TEXTMESHPRO
        if (moneyTextTMP != null)
        {
            moneyTextTMP.text = moneyString;
        }
#else
        // Try to use TextMeshPro even if define isn't set (using reflection)
        if (moneyTextTMP != null)
        {
            var textProperty = moneyTextTMP.GetType().GetProperty("text");
            if (textProperty != null)
            {
                textProperty.SetValue(moneyTextTMP, moneyString);
            }
        }
#endif
    }
}




