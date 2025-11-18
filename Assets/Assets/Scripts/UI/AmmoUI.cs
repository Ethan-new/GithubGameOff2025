using UnityEngine;

#if UNITY_TEXTMESHPRO
using TMPro;
#endif

/// <summary>
/// UI component that displays the player's current ammo in the bottom right of the screen.
/// Attach this to a GameObject with a TextMeshProUGUI component to display the ammo.
/// </summary>
public class AmmoUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private string ammoFormat = "{0}/{1}";
    [SerializeField] private string emptyAmmoText = "0/0";

    [Header("Text Component (Auto-detected if not assigned)")]
    [Tooltip("TextMeshProUGUI component to display the ammo. Drag your TextMeshPro component here or use 'Auto-Find Text Component' from the context menu.")]
    [SerializeField] 
#if UNITY_TEXTMESHPRO
    private TextMeshProUGUI ammoTextTMP;
#else
    private UnityEngine.Object ammoTextTMP; // Using Object type so it can be assigned even without TextMeshPro package
#endif
    
    private PlayerController playerController;
    private PlayerInventory playerInventory;
    private Weapon currentWeapon;
    private int lastCurrentAmmo = -1;
    private int lastInventoryAmmo = -1;

    private void Awake()
    {
        // Try to find text components - will try again in Start() if not found
#if UNITY_TEXTMESHPRO
        if (ammoTextTMP == null)
        {
            FindTextComponent();
        }
#else
        if (ammoTextTMP == null)
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
        if (ammoTextTMP == null)
        {
            FindTextComponent();
        }
#else
        if (ammoTextTMP == null)
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
        if (ammoTextTMP == null)
        {
            FindTextComponent();
        }

        // Warn if still no text component found
        if (ammoTextTMP == null)
        {
            Debug.LogWarning("AmmoUI: No TextMeshProUGUI component found. Please add a TextMeshProUGUI component to this GameObject or use the context menu 'Auto-Find Text Component'.", this);
        }
#else
        if (ammoTextTMP == null)
        {
            FindTextComponent();
        }

        // Warn if still no text component found
        if (ammoTextTMP == null)
        {
            Debug.LogWarning("AmmoUI: No TextMeshProUGUI component found. Please add a TextMeshProUGUI component to this GameObject or use the context menu 'Auto-Find Text Component'.", this);
        }
#endif

        // Find PlayerController - cache reference to avoid expensive Find calls
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogWarning("AmmoUI: No PlayerController found in scene. Ammo will not update.", this);
            }
        }

        // Find PlayerInventory - cache reference to avoid expensive Find calls
        if (playerInventory == null)
        {
            if (playerController != null)
            {
                playerInventory = playerController.GetComponent<PlayerInventory>();
                if (playerInventory == null)
                {
                    playerInventory = playerController.GetComponentInChildren<PlayerInventory>();
                }
            }
            if (playerInventory == null)
            {
                playerInventory = FindFirstObjectByType<PlayerInventory>();
            }
        }

        // Initialize display
        UpdateAmmoDisplay();
    }

    private void Update()
    {
        // Re-find PlayerController if lost (e.g., after scene reload)
        // Use cooldown to avoid expensive Find calls every frame
        if (playerController == null)
        {
            // Only search once per second if reference is lost
            if (Time.frameCount % 60 == 0) // Check every 60 frames (~1 second at 60fps)
            {
                playerController = FindFirstObjectByType<PlayerController>();
                if (playerController == null)
                    return;
            }
            else
            {
                return; // Skip update if we don't have a controller reference
            }
        }

        // Re-find PlayerInventory if lost (only if we have a controller)
        if (playerInventory == null && playerController != null)
        {
            playerInventory = playerController.GetComponent<PlayerInventory>();
            if (playerInventory == null)
            {
                playerInventory = playerController.GetComponentInChildren<PlayerInventory>();
            }
            // Don't use FindFirstObjectByType here - if it's not on the controller, skip
        }

        // Update ammo display if weapon or ammo has changed
        Weapon newWeapon = playerController.GetCurrentWeapon();
        
        // Check if weapon changed
        if (newWeapon != currentWeapon)
        {
            currentWeapon = newWeapon;
            lastCurrentAmmo = -1; // Reset to force update
            lastInventoryAmmo = -1; // Reset to force update
            UpdateAmmoDisplay();
        }
        // Check if ammo changed
        else if (currentWeapon != null)
        {
            int currentAmmo = currentWeapon.CurrentAmmo;
            int inventoryAmmo = 0;
            
            // Get ammo count from inventory if available
            if (playerInventory != null && currentWeapon.AmmoType != null)
            {
                inventoryAmmo = playerInventory.GetAmmoCount(currentWeapon.AmmoType);
            }
            
            if (currentAmmo != lastCurrentAmmo || inventoryAmmo != lastInventoryAmmo)
            {
                lastCurrentAmmo = currentAmmo;
                lastInventoryAmmo = inventoryAmmo;
                UpdateAmmoDisplay();
            }
        }
        // If no weapon, make sure display is updated
        else if (currentWeapon == null && (lastCurrentAmmo != -1 || lastInventoryAmmo != -1))
        {
            lastCurrentAmmo = -1;
            lastInventoryAmmo = -1;
            UpdateAmmoDisplay();
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
        if (ammoTextTMP == null)
        {
            ammoTextTMP = GetComponent<TextMeshProUGUI>();
        }
        if (ammoTextTMP == null)
        {
            ammoTextTMP = GetComponentInChildren<TextMeshProUGUI>(true); // Include inactive
        }
        if (ammoTextTMP == null)
        {
            ammoTextTMP = GetComponentInParent<TextMeshProUGUI>(true); // Include inactive
        }
        // Last resort: search in the entire scene
        if (ammoTextTMP == null)
        {
            TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            // Prefer one that's on the same GameObject or a child
            foreach (TextMeshProUGUI text in allTexts)
            {
                if (text.transform == transform || text.transform.IsChildOf(transform))
                {
                    ammoTextTMP = text;
                    break;
                }
            }
            // If still not found, just use the first one (user can reassign if wrong)
            if (ammoTextTMP == null && allTexts.Length > 0)
            {
                ammoTextTMP = allTexts[0];
                Debug.LogWarning($"AmmoUI: Auto-assigned TextMeshProUGUI component from '{ammoTextTMP.gameObject.name}'. If this is wrong, please manually assign the correct Text component.", this);
            }
        }
        
        // Log success for TextMeshPro
        if (ammoTextTMP != null)
        {
            Debug.Log($"AmmoUI: Found TextMeshProUGUI component on '{ammoTextTMP.gameObject.name}'", this);
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
                ammoTextTMP = tmpComponent;
                Debug.Log($"AmmoUI: Found TextMeshProUGUI component on '{tmpComponent.gameObject.name}' (using reflection)", this);
                return;
            }
        }
#endif
    }

    /// <summary>
    /// Updates the ammo display text.
    /// </summary>
    private void UpdateAmmoDisplay()
    {
        string ammoString;

        if (currentWeapon != null && currentWeapon.IsEquipped)
        {
            int currentAmmo = currentWeapon.CurrentAmmo;
            int inventoryAmmo = 0;
            
            // Get ammo count from inventory if available
            if (playerInventory != null && currentWeapon.AmmoType != null)
            {
                inventoryAmmo = playerInventory.GetAmmoCount(currentWeapon.AmmoType);
            }
            
            // Display format: ammoInClip / ammoInInventory
            // Use string.Format - it's optimized in Unity and handles format strings properly
            ammoString = string.Format(ammoFormat, currentAmmo, inventoryAmmo);
        }
        else
        {
            ammoString = emptyAmmoText;
        }

#if UNITY_TEXTMESHPRO
        if (ammoTextTMP != null)
        {
            ammoTextTMP.text = ammoString;
        }
#else
        // Try to use TextMeshPro even if define isn't set (using reflection)
        if (ammoTextTMP != null)
        {
            var textProperty = ammoTextTMP.GetType().GetProperty("text");
            if (textProperty != null)
            {
                textProperty.SetValue(ammoTextTMP, ammoString);
            }
        }
#endif
    }
}

