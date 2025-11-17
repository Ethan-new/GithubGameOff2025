using UnityEngine;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

/// <summary>
/// UI component that displays the player's score on screen.
/// Attach this to a GameObject with a TextMeshProUGUI component to display the score.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private string scorePrefix = "Score: ";
    [SerializeField] private bool showPrefix = true;

    [Header("Text Component (Auto-detected if not assigned)")]
    [Tooltip("TextMeshProUGUI component to display the score. Drag your TextMeshPro component here or use 'Auto-Find Text Component' from the context menu.")]
    [SerializeField] 
#if UNITY_TEXTMESHPRO
    private TextMeshProUGUI scoreTextTMP;
#else
    private UnityEngine.Object scoreTextTMP; // Using Object type so it can be assigned even without TextMeshPro package
#endif
    
    private ScoreManager scoreManager;

    private void Awake()
    {
        // Try to find text components - will try again in Start() if not found
#if UNITY_TEXTMESHPRO
        if (scoreTextTMP == null)
        {
            FindTextComponent();
        }
#else
        if (scoreTextTMP == null)
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
        if (scoreTextTMP == null)
        {
            FindTextComponent();
        }
#else
        if (scoreTextTMP == null)
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
        if (scoreTextTMP == null)
        {
            FindTextComponent();
        }

        // Warn if still no text component found
        if (scoreTextTMP == null)
        {
            Debug.LogWarning("ScoreUI: No TextMeshProUGUI component found. Please add a TextMeshProUGUI component to this GameObject or use the context menu 'Auto-Find Text Component'.", this);
        }
#else
        if (scoreTextTMP == null)
        {
            FindTextComponent();
        }

        // Warn if still no text component found
        if (scoreTextTMP == null)
        {
            Debug.LogWarning("ScoreUI: No TextMeshProUGUI component found. Please add a TextMeshProUGUI component to this GameObject or use the context menu 'Auto-Find Text Component'.", this);
        }
#endif

        // Find ScoreManager
        if (ScoreManager.Instance == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
            if (scoreManager == null)
            {
                Debug.LogWarning("ScoreUI: No ScoreManager found in scene. Score will not update.", this);
                return;
            }
        }
        else
        {
            scoreManager = ScoreManager.Instance;
        }

        // Subscribe to score changes
        if (scoreManager != null)
        {
            scoreManager.OnScoreChanged += UpdateScoreDisplay;
            // Initialize display with current score
            UpdateScoreDisplay(scoreManager.CurrentScore);
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
        if (scoreTextTMP == null)
        {
            scoreTextTMP = GetComponent<TextMeshProUGUI>();
        }
        if (scoreTextTMP == null)
        {
            scoreTextTMP = GetComponentInChildren<TextMeshProUGUI>(true); // Include inactive
        }
        if (scoreTextTMP == null)
        {
            scoreTextTMP = GetComponentInParent<TextMeshProUGUI>(true); // Include inactive
        }
        // Last resort: search in the entire scene
        if (scoreTextTMP == null)
        {
            TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            // Prefer one that's on the same GameObject or a child
            foreach (TextMeshProUGUI text in allTexts)
            {
                if (text.transform == transform || text.transform.IsChildOf(transform))
                {
                    scoreTextTMP = text;
                    break;
                }
            }
            // If still not found, just use the first one (user can reassign if wrong)
            if (scoreTextTMP == null && allTexts.Length > 0)
            {
                scoreTextTMP = allTexts[0];
                Debug.LogWarning($"ScoreUI: Auto-assigned TextMeshProUGUI component from '{scoreTextTMP.gameObject.name}'. If this is wrong, please manually assign the correct Text component.", this);
            }
        }
        
        // Log success for TextMeshPro
        if (scoreTextTMP != null)
        {
            Debug.Log($"ScoreUI: Found TextMeshProUGUI component on '{scoreTextTMP.gameObject.name}'", this);
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
                scoreTextTMP = tmpComponent;
                Debug.Log($"ScoreUI: Found TextMeshProUGUI component on '{tmpComponent.gameObject.name}' (using reflection)", this);
                return;
            }
        }
#endif

    }

    private void OnDestroy()
    {
        // Unsubscribe from score changes
        if (scoreManager != null)
        {
            scoreManager.OnScoreChanged -= UpdateScoreDisplay;
        }
    }

    /// <summary>
    /// Updates the score display text.
    /// </summary>
    /// <param name="score">The new score value.</param>
    private void UpdateScoreDisplay(int score)
    {
        string scoreString = showPrefix ? (scorePrefix + score.ToString()) : score.ToString();

#if UNITY_TEXTMESHPRO
        if (scoreTextTMP != null)
        {
            scoreTextTMP.text = scoreString;
        }
#else
        // Try to use TextMeshPro even if define isn't set (using reflection)
        if (scoreTextTMP != null)
        {
            var textProperty = scoreTextTMP.GetType().GetProperty("text");
            if (textProperty != null)
            {
                textProperty.SetValue(scoreTextTMP, scoreString);
            }
        }
#endif
    }
}
