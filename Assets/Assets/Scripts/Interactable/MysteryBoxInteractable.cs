using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Interactable mystery box component. When the player presses E on this box, something will happen.
/// </summary>
public class MysteryBoxInteractable : Interactable
{
    [Header("Mystery Box Settings")]
    [SerializeField] private bool hasBeenOpened = false;
    [SerializeField] private bool canBeOpenedMultipleTimes = false;

    [Header("Weapon Cycling Settings")]
    [SerializeField] private Weapon[] availableWeapons; // Array of weapon prefabs to cycle through
    [SerializeField] private Transform weaponSpawnPosition; // Empty GameObject that marks where weapons spawn (if null, uses box position + offset)
    [SerializeField] private float weaponSwitchInterval = 0.1f; // How fast weapons switch (seconds between switches)
    [SerializeField] private float levitationSpeed = 0.5f; // How fast weapons levitate upward (units per second)
    [SerializeField] private Vector3 weaponSpawnOffset = new Vector3(0f, 1f, 0f); // Fallback offset if spawn position is not set
    [SerializeField] private float totalCycleDuration = 8f; // Total time for the mystery box cycle (seconds)

    private bool isCycling = false;
    private Coroutine cyclingCoroutine = null;
    private Weapon currentDisplayedWeapon = null;
    private int currentWeaponIndex = 0;
    private int selectedWeaponIndex = -1; // The weapon that will be awarded at the end
    private bool weaponReadyToSwap = false; // Whether the weapon is ready to be swapped

    public override bool CanInteract
    {
        get
        {
            // Can interact if: not cycling AND (box not opened OR can be opened multiple times) OR weapon is ready to swap
            return base.CanInteract && ((canBeOpenedMultipleTimes || !hasBeenOpened) && !isCycling || weaponReadyToSwap);
        }
    }

    public override string InteractionPrompt
    {
        get
        {
            if (weaponReadyToSwap)
            {
                return "Press E to swap weapon";
            }
            return base.InteractionPrompt;
        }
    }

    public override void OnInteract(GameObject interactor)
    {
        Debug.Log("E pressed on Mystery Box!");
        
        // If weapon is ready to swap, give it to the player
        if (weaponReadyToSwap)
        {
            GiveWeaponToPlayer(interactor);
            return;
        }
        
        if (!CanInteract)
        {
            if (isCycling)
            {
                Debug.Log("Mystery box is currently cycling through weapons.");
            }
            else
            {
                Debug.Log("This mystery box cannot be interacted with.");
            }
            return;
        }

        // Start cycling through weapons
        StartCyclingWeapons(interactor);
    }

    /// <summary>
    /// Starts the weapon cycling animation.
    /// </summary>
    private void StartCyclingWeapons(GameObject interactor)
    {
        if (isCycling)
            return;

        if (availableWeapons == null || availableWeapons.Length == 0)
        {
            Debug.LogWarning("MysteryBoxInteractable: No weapons assigned to cycle through!");
            return;
        }

        // Stop any existing cycling
        if (cyclingCoroutine != null)
        {
            StopCoroutine(cyclingCoroutine);
        }

        // Get PlayerController from interactor to check their inventory
        PlayerController playerController = null;
        if (interactor != null)
        {
            playerController = interactor.GetComponent<PlayerController>();
        }

        // Filter out weapons the player already has
        List<Weapon> availableWeaponsList = new List<Weapon>();
        
        // First, add all non-null weapons from availableWeapons
        foreach (Weapon weapon in availableWeapons)
        {
            if (weapon != null)
            {
                availableWeaponsList.Add(weapon);
            }
        }

        // Remove weapons the player already has
        if (playerController != null)
        {
            int originalCount = availableWeaponsList.Count;
            availableWeaponsList.RemoveAll(weapon => playerController.HasWeapon(weapon));
            
            Debug.Log($"Mystery Box: Filtered weapons - Started with {originalCount}, after filtering: {availableWeaponsList.Count}");
            
            // Debug: Log what weapons player has
            List<Weapon> playerWeapons = playerController.GetAllWeapons();
            Debug.Log($"Mystery Box: Player has {playerWeapons.Count} weapons:");
            foreach (Weapon pw in playerWeapons)
            {
                if (pw != null)
                {
                    string pwName = pw.gameObject.name;
                    if (pwName.EndsWith("(Clone)"))
                        pwName = pwName.Substring(0, pwName.Length - 7);
                    Debug.Log($"  - GameObject: {pwName}, WeaponName: {pw.WeaponName}");
                }
            }
            
            // Debug: Log what weapons are in the available list before filtering
            Debug.Log($"Mystery Box: Available weapons before filtering:");
            foreach (Weapon aw in availableWeapons)
            {
                if (aw != null)
                {
                    string awName = aw.gameObject.name;
                    if (awName.EndsWith("(Clone)"))
                        awName = awName.Substring(0, awName.Length - 7);
                    Debug.Log($"  - GameObject: {awName}, WeaponName: {aw.WeaponName}, HasWeapon: {playerController.HasWeapon(aw)}");
                }
            }
            
            // Debug: Log what weapons are available after filtering
            Debug.Log($"Mystery Box: Available weapons after filtering:");
            foreach (Weapon aw in availableWeaponsList)
            {
                if (aw != null)
                {
                    string awName = aw.gameObject.name;
                    if (awName.EndsWith("(Clone)"))
                        awName = awName.Substring(0, awName.Length - 7);
                    Debug.Log($"  - GameObject: {awName}, WeaponName: {aw.WeaponName}");
                }
            }
        }

        // Check if there are any weapons left to give
        if (availableWeaponsList.Count == 0)
        {
            Debug.LogWarning("Mystery Box: Player already has all available weapons! Cannot give a weapon.");
            return;
        }

        // Randomly select which weapon will be awarded from the filtered list
        int randomIndex = Random.Range(0, availableWeaponsList.Count);
        Weapon selectedWeapon = availableWeaponsList[randomIndex];
        
        // Find the index in the original array
        selectedWeaponIndex = System.Array.IndexOf(availableWeapons, selectedWeapon);
        
        if (selectedWeaponIndex < 0)
        {
            Debug.LogWarning("MysteryBoxInteractable: Could not find selected weapon in available weapons array!");
            return;
        }

        Debug.Log($"Mystery Box: Selected weapon {selectedWeapon.WeaponName} (filtered from {availableWeaponsList.Count} available weapons)");

        isCycling = true;
        // Start at a random weapon index for variety
        currentWeaponIndex = Random.Range(0, availableWeapons.Length);
        cyclingCoroutine = StartCoroutine(CycleWeaponsCoroutine());
    }

    /// <summary>
    /// Coroutine that cycles through weapons and makes them levitate.
    /// </summary>
    private IEnumerator CycleWeaponsCoroutine()
    {
        // Use the spawn position GameObject if set, otherwise use box position + offset
        Vector3 spawnPosition;
        if (weaponSpawnPosition != null)
        {
            spawnPosition = weaponSpawnPosition.position;
        }
        else
        {
            spawnPosition = transform.position + weaponSpawnOffset;
        }
        
        float totalElapsedTime = 0f;
        float levitationHeight = 0f;
        bool showingSelectedWeapon = false;

        while (isCycling && totalElapsedTime < totalCycleDuration)
        {
            // Check if we're in the final interval (last weaponSwitchInterval seconds) - show selected weapon
            float timeRemaining = totalCycleDuration - totalElapsedTime;
            bool shouldShowSelected = timeRemaining <= weaponSwitchInterval;

            // Determine which weapon to show
            int weaponToShow;
            if (shouldShowSelected && !showingSelectedWeapon)
            {
                // Time to show the selected weapon
                weaponToShow = selectedWeaponIndex;
                showingSelectedWeapon = true;
                currentWeaponIndex = selectedWeaponIndex;
            }
            else if (!shouldShowSelected)
            {
                // Normal cycling - show random weapons (pick a random weapon each time for more variety)
                weaponToShow = Random.Range(0, availableWeapons.Length);
                currentWeaponIndex = weaponToShow;
            }
            else
            {
                // Already showing selected weapon, keep showing it
                weaponToShow = selectedWeaponIndex;
            }

            // Destroy previous weapon if it exists and we're switching
            if (currentDisplayedWeapon != null && (!shouldShowSelected || !showingSelectedWeapon))
            {
                Destroy(currentDisplayedWeapon.gameObject);
                currentDisplayedWeapon = null;
                levitationHeight = 0f; // Reset height for new weapon
            }

            // Spawn the weapon if we don't have one or we're switching
            if (currentDisplayedWeapon == null && weaponToShow < availableWeapons.Length && availableWeapons[weaponToShow] != null)
            {
                Weapon weaponPrefab = availableWeapons[weaponToShow];
                GameObject weaponObj = Instantiate(weaponPrefab.gameObject, spawnPosition, Quaternion.identity);
                currentDisplayedWeapon = weaponObj.GetComponent<Weapon>();
                
                if (currentDisplayedWeapon == null)
                {
                    currentDisplayedWeapon = weaponObj.GetComponentInChildren<Weapon>();
                }

                if (currentDisplayedWeapon != null)
                {
                    // Disable weapon behavior (don't want it to fire or be picked up during cycling)
                    currentDisplayedWeapon.enabled = false;
                    
                    // Disable colliders so it doesn't interfere
                    Collider[] colliders = weaponObj.GetComponentsInChildren<Collider>();
                    foreach (Collider col in colliders)
                    {
                        col.enabled = false;
                    }
                }
            }

            // Wait for switch interval (or until cycle ends)
            float intervalElapsed = 0f;
            while (intervalElapsed < weaponSwitchInterval && totalElapsedTime < totalCycleDuration)
            {
                float deltaTime = Time.deltaTime;
                intervalElapsed += deltaTime;
                totalElapsedTime += deltaTime;
                
                // Levitate the weapon upward
                if (currentDisplayedWeapon != null)
                {
                    levitationHeight += levitationSpeed * deltaTime;
                    Vector3 currentPosition = spawnPosition + new Vector3(0f, levitationHeight, 0f);
                    currentDisplayedWeapon.transform.position = currentPosition;
                }

                yield return null;
            }

            // Move to next weapon (only if not showing selected weapon yet)
            if (!shouldShowSelected && !showingSelectedWeapon)
            {
                currentWeaponIndex = (currentWeaponIndex + 1) % availableWeapons.Length;
            }
        }

        // Ensure selected weapon is shown at the end
        // Check if we already have the selected weapon displayed
        bool hasCorrectWeapon = false;
        if (currentDisplayedWeapon != null)
        {
            // Check if current weapon is the selected one by comparing prefab
            Weapon currentWeaponPrefab = availableWeapons[selectedWeaponIndex];
            if (currentWeaponPrefab != null)
            {
                string currentName = currentDisplayedWeapon.gameObject.name;
                string selectedName = currentWeaponPrefab.gameObject.name;
                
                // Remove "(Clone)" suffix for comparison
                if (currentName.EndsWith("(Clone)"))
                    currentName = currentName.Substring(0, currentName.Length - 7);
                if (selectedName.EndsWith("(Clone)"))
                    selectedName = selectedName.Substring(0, selectedName.Length - 7);
                
                hasCorrectWeapon = (currentName == selectedName);
            }
        }
        
        // Only destroy and respawn if we don't have the correct weapon
        if (!hasCorrectWeapon)
        {
            if (currentDisplayedWeapon != null)
            {
                Destroy(currentDisplayedWeapon.gameObject);
                currentDisplayedWeapon = null;
            }

            // Spawn the selected weapon
            if (selectedWeaponIndex >= 0 && selectedWeaponIndex < availableWeapons.Length && availableWeapons[selectedWeaponIndex] != null)
            {
                Weapon weaponPrefab = availableWeapons[selectedWeaponIndex];
                GameObject weaponObj = Instantiate(weaponPrefab.gameObject, spawnPosition, Quaternion.identity);
                currentDisplayedWeapon = weaponObj.GetComponent<Weapon>();
                
                if (currentDisplayedWeapon == null)
                {
                    currentDisplayedWeapon = weaponObj.GetComponentInChildren<Weapon>();
                }

                if (currentDisplayedWeapon != null)
                {
                    currentDisplayedWeapon.enabled = false;
                    Collider[] colliders = weaponObj.GetComponentsInChildren<Collider>();
                    foreach (Collider col in colliders)
                    {
                        col.enabled = false;
                    }
                    
                    // Position the weapon at the current levitation height
                    currentDisplayedWeapon.transform.position = spawnPosition + new Vector3(0f, levitationHeight, 0f);
                    
                    Debug.Log($"Mystery Box: Final weapon displayed - {weaponPrefab.gameObject.name} (selected index: {selectedWeaponIndex})");
                }
                else
                {
                    Debug.LogWarning($"Mystery Box: Could not find Weapon component on {weaponPrefab.gameObject.name}!");
                }
            }
            else
            {
                Debug.LogWarning($"Mystery Box: Invalid selectedWeaponIndex {selectedWeaponIndex} or weapon is null!");
            }
        }
        else
        {
            Debug.Log($"Mystery Box: Correct weapon already displayed - {currentDisplayedWeapon.gameObject.name}");
        }

        // Stop cycling
        isCycling = false;
        hasBeenOpened = true;
        weaponReadyToSwap = true;

        Debug.Log($"Mystery Box: Weapon {availableWeapons[selectedWeaponIndex].WeaponName} is ready! Press E to swap.");

        cyclingCoroutine = null;
    }

    /// <summary>
    /// Gives the selected weapon to the player.
    /// </summary>
    private void GiveWeaponToPlayer(GameObject interactor)
    {
        if (selectedWeaponIndex < 0 || selectedWeaponIndex >= availableWeapons.Length)
        {
            Debug.LogWarning("MysteryBoxInteractable: Invalid selected weapon index!");
            return;
        }

        Weapon weaponPrefab = availableWeapons[selectedWeaponIndex];
        if (weaponPrefab == null)
        {
            Debug.LogWarning("MysteryBoxInteractable: Selected weapon prefab is null!");
            return;
        }

        // Get PlayerController from the interactor
        PlayerController playerController = interactor.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("MysteryBoxInteractable: Interactor does not have PlayerController component!");
            return;
        }

        // Remove the currently equipped weapon and get its slot
        int slotToUse = playerController.RemoveCurrentWeapon();
        
        if (slotToUse < 0)
        {
            // No weapon was equipped, find first available slot
            for (int i = 0; i < 2; i++) // Assuming max 2 weapons
            {
                if (playerController.GetWeapon(i) == null)
                {
                    slotToUse = i;
                    break;
                }
            }
        }

        if (slotToUse < 0)
        {
            Debug.LogWarning("MysteryBoxInteractable: Could not find a slot for the weapon!");
            return;
        }

        Debug.Log($"Mystery Box: Removed current weapon, using slot {slotToUse}");
        
        // Add the mystery box weapon to the confirmed slot
        bool success = playerController.AddWeapon(weaponPrefab, slotToUse);
        
        if (!success)
        {
            Debug.LogWarning($"MysteryBoxInteractable: Failed to add weapon to slot {slotToUse}!");
            return;
        }
        
        // Verify the weapon was added
        Weapon addedWeapon = playerController.GetWeapon(slotToUse);
        if (addedWeapon == null)
        {
            Debug.LogWarning($"MysteryBoxInteractable: Weapon was not found in slot {slotToUse} after adding!");
            return;
        }
        
        // AddWeapon should auto-equip if currentWeaponIndex is -1 (which it is after RemoveCurrentWeapon)
        // But let's explicitly switch to it to be sure
        int currentSlotAfterAdd = playerController.GetCurrentWeaponSlot();
        if (currentSlotAfterAdd != slotToUse)
        {
            // Weapon wasn't auto-equipped, switch to it
            playerController.SwitchToWeaponSlot(slotToUse + 1); // SwitchToWeaponSlot uses 1-based indexing
            Debug.Log($"Mystery Box: Switched to weapon at slot {slotToUse}");
        }
        else
        {
            Debug.Log($"Mystery Box: Weapon auto-equipped at slot {slotToUse}");
        }
        
        Debug.Log($"Mystery Box: Successfully added and equipped weapon {weaponPrefab.WeaponName} at slot {slotToUse}");
        
        // Clean up displayed weapon AFTER giving it to player
        if (currentDisplayedWeapon != null)
        {
            Destroy(currentDisplayedWeapon.gameObject);
            currentDisplayedWeapon = null;
        }
        
        // Reset state
        weaponReadyToSwap = false;
        selectedWeaponIndex = -1;
    }

    /// <summary>
    /// Stops the weapon cycling animation.
    /// </summary>
    public void StopCycling()
    {
        isCycling = false;
        if (cyclingCoroutine != null)
        {
            StopCoroutine(cyclingCoroutine);
            cyclingCoroutine = null;
        }
    }

    /// <summary>
    /// Resets the mystery box so it can be opened again.
    /// </summary>
    public void Reset()
    {
        hasBeenOpened = false;
        weaponReadyToSwap = false;
        selectedWeaponIndex = -1;
        StopCycling();
        
        // Clean up displayed weapon if any
        if (currentDisplayedWeapon != null)
        {
            Destroy(currentDisplayedWeapon.gameObject);
            currentDisplayedWeapon = null;
        }
    }

    private void OnDestroy()
    {
        StopCycling();
    }
}

