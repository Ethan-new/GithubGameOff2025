using UnityEngine;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

/// <summary>
/// UI component that displays the interaction prompt when the player looks at an interactable object.
/// Attach this to a GameObject with a TextMeshProUGUI component to display the prompt.
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private string defaultText = "";
    [SerializeField] private bool hideWhenNoInteractable = true;
    [SerializeField] private bool disableGameObjectWhenHidden = false;

    [Header("Text Component (Auto-detected if not assigned)")]
    [Tooltip("TextMeshProUGUI component to display the interaction prompt. Drag your TextMeshPro component here or use 'Auto-Find Text Component' from the context menu.")]
    [SerializeField] 
#if UNITY_TEXTMESHPRO
    private TextMeshProUGUI promptTextTMP;
#else
    private UnityEngine.Object promptTextTMP; // Using Object type so it can be assigned even without TextMeshPro package
#endif
    
    private PlayerController playerController;
    private IInteractable currentInteractable;

    private void Awake()
    {
        // Try to find text components - will try again in Start() if not found
#if UNITY_TEXTMESHPRO
        if (promptTextTMP == null)
        {
            FindTextComponent();
        }
        // Hide text immediately in Awake
        if (promptTextTMP != null)
        {
            promptTextTMP.text = "";
        }
#else
        if (promptTextTMP == null)
        {
            FindTextComponent();
        }
        // Hide text immediately in Awake (using reflection)
        if (promptTextTMP != null)
        {
            var textProperty = promptTextTMP.GetType().GetProperty("text");
            if (textProperty != null)
            {
                textProperty.SetValue(promptTextTMP, "");
            }
        }
#endif
    }

    private void OnValidate()
    {
        // In editor, try to auto-find if not assigned
        #if UNITY_EDITOR
#if UNITY_TEXTMESHPRO
        if (promptTextTMP == null)
        {
            FindTextComponent();
        }
#else
        if (promptTextTMP == null)
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
        if (promptTextTMP == null)
        {
            FindTextComponent();
        }

        // Warn if still no text component found
        if (promptTextTMP == null)
        {
            Debug.LogWarning("InteractionPromptUI: No TextMeshProUGUI component found. Please add a TextMeshProUGUI component to this GameObject or use the context menu 'Auto-Find Text Component'.", this);
        }
#else
        if (promptTextTMP == null)
        {
            FindTextComponent();
        }

        // Warn if still no text component found
        if (promptTextTMP == null)
        {
            Debug.LogWarning("InteractionPromptUI: No TextMeshProUGUI component found. Please add a TextMeshProUGUI component to this GameObject or use the context menu 'Auto-Find Text Component'.", this);
        }
#endif

        // Find PlayerController
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("InteractionPromptUI: No PlayerController found in scene. Prompt will not update.", this);
            return;
        }

        // Hide the text initially
        SetPromptText("");
    }

    private void Update()
    {
        if (playerController == null)
            return;

        // Get the current interactable from PlayerController
        IInteractable newInteractable = playerController.GetCurrentInteractable();

        // Update if interactable changed
        if (newInteractable != currentInteractable)
        {
            currentInteractable = newInteractable;
            UpdatePromptDisplay();
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
        if (promptTextTMP == null)
        {
            promptTextTMP = GetComponent<TextMeshProUGUI>();
        }
        if (promptTextTMP == null)
        {
            promptTextTMP = GetComponentInChildren<TextMeshProUGUI>(true); // Include inactive
        }
        if (promptTextTMP == null)
        {
            promptTextTMP = GetComponentInParent<TextMeshProUGUI>(true); // Include inactive
        }

        // Last resort: find any TextMeshPro in the scene (but warn user)
        if (promptTextTMP == null)
        {
            TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (TextMeshProUGUI text in allTexts)
            {
                if (text.transform == transform || text.transform.IsChildOf(transform))
                {
                    promptTextTMP = text;
                    break;
                }
            }
            // If still not found, use first one found (with warning)
            if (promptTextTMP == null && allTexts.Length > 0)
            {
                promptTextTMP = allTexts[0];
                Debug.LogWarning($"InteractionPromptUI: Auto-assigned TextMeshProUGUI component from '{promptTextTMP.gameObject.name}'. If this is wrong, please manually assign the correct Text component.", this);
            }
        }

        // Log success for TextMeshPro
        if (promptTextTMP != null)
        {
            Debug.Log($"InteractionPromptUI: Found TextMeshProUGUI component on '{promptTextTMP.gameObject.name}'", this);
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
                promptTextTMP = tmpComponent;
                Debug.Log($"InteractionPromptUI: Found TextMeshProUGUI component on '{tmpComponent.gameObject.name}' (using reflection)", this);
                return;
            }
        }
#endif

    }

    /// <summary>
    /// Updates the prompt display text.
    /// </summary>
    private void UpdatePromptDisplay()
    {
        string promptString = defaultText;
        bool shouldShow = false;

        if (currentInteractable != null && currentInteractable.CanInteract)
        {
            promptString = currentInteractable.InteractionPrompt;
            shouldShow = true;
        }
        else if (hideWhenNoInteractable)
        {
            promptString = "";
            shouldShow = false;
        }
        else
        {
            shouldShow = true;
        }

#if UNITY_TEXTMESHPRO
        if (promptTextTMP != null)
        {
            promptTextTMP.text = promptString;
            // Disable/enable GameObject if option is set
            if (disableGameObjectWhenHidden)
            {
                promptTextTMP.gameObject.SetActive(shouldShow);
            }
        }
#else
        // Try to use TextMeshPro even if define isn't set (using reflection)
        if (promptTextTMP != null)
        {
            var textProperty = promptTextTMP.GetType().GetProperty("text");
            if (textProperty != null)
            {
                textProperty.SetValue(promptTextTMP, promptString);
            }
            // Disable/enable GameObject if option is set
            if (disableGameObjectWhenHidden)
            {
                var gameObjectProperty = promptTextTMP.GetType().GetProperty("gameObject");
                if (gameObjectProperty != null)
                {
                    var go = gameObjectProperty.GetValue(promptTextTMP) as GameObject;
                    if (go != null)
                    {
                        go.SetActive(shouldShow);
                    }
                }
            }
        }
#endif
    }

    /// <summary>
    /// Manually sets the prompt text. Useful if you want to control it from other scripts.
    /// </summary>
    public void SetPromptText(string text)
    {
#if UNITY_TEXTMESHPRO
        if (promptTextTMP != null)
        {
            promptTextTMP.text = text;
        }
#else
        if (promptTextTMP != null)
        {
            var textProperty = promptTextTMP.GetType().GetProperty("text");
            if (textProperty != null)
            {
                textProperty.SetValue(promptTextTMP, text);
            }
        }
#endif
    }
}

