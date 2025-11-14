using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 80f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private InputActionAsset inputActionsAsset;
    [SerializeField] private Transform weaponHolder; // Transform where weapons are held/positioned

    [Header("Inventory Settings")]
    [SerializeField] private int maxWeapons = 2;
    [SerializeField] private Weapon[] startingWeapons; // Optional starting weapons

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask weaponLayerMask = -1; // All layers by default

    [Header("Weapon Swap Animation")]
    [SerializeField] private float swapAnimationDuration = 0.5f;
    [SerializeField] private AnimationCurve swapAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Vector3 swapHideOffset = new Vector3(0f, -0.5f, 0.5f); // Where to move weapon when hiding
    [SerializeField] private Vector3 swapShowOffset = new Vector3(0f, -0.5f, 0.5f); // Where to start new weapon from

    private CharacterController characterController;
    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction previousWeaponAction;
    private InputAction nextWeaponAction;
    private InputAction interactAction;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isSprinting;
    private bool jumpPressed;
    private bool attackPressed;
    private bool attackPressedThisFrame; // For single fire weapons
    private bool interactPressed;

    private float horizontalRotation = 0f;
    private float verticalRotation = 0f;
    private Vector3 velocity;

    // Inventory system
    private Weapon[] weaponInventory;
    private int currentWeaponIndex = -1; // -1 means no weapon equipped
    private bool isSwappingWeapon = false; // Track if swap animation is in progress
    private Coroutine swapAnimationCoroutine = null;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Initialize input actions (find action map, but don't enable yet - that happens in OnEnable)
        if (inputActionsAsset != null)
        {
            playerActionMap = inputActionsAsset.FindActionMap("Player");
            if (playerActionMap != null)
            {
                moveAction = playerActionMap.FindAction("Move");
                lookAction = playerActionMap.FindAction("Look");
                sprintAction = playerActionMap.FindAction("Sprint");
                jumpAction = playerActionMap.FindAction("Jump");
                attackAction = playerActionMap.FindAction("Attack");
                previousWeaponAction = playerActionMap.FindAction("Previous");
                nextWeaponAction = playerActionMap.FindAction("Next");
                interactAction = playerActionMap.FindAction("Interact");
                
                // Debug log to verify interact action is found
                if (interactAction == null)
                {
                    Debug.LogWarning("Interact action not found in input actions!");
                }
                else
                {
                    Debug.Log("Interact action found successfully");
                }
            }
            else
            {
                Debug.LogError("Player action map not found in InputActionAsset!");
            }
        }
        else
        {
            Debug.LogWarning("InputActionAsset is not assigned in PlayerController!");
        }

        // Initialize weapon inventory
        weaponInventory = new Weapon[maxWeapons];
        
        // Setup camera - ensure it's a child of the player (do this first so weapon holder can be parented to it)
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                // Create a new camera as a child of the player
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(transform);
                cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0); // Standard eye height
                cameraObj.transform.localRotation = Quaternion.identity;
                playerCamera = cameraObj.AddComponent<Camera>();
            }
        }

        // Ensure camera is a child of the player
        if (playerCamera != null && playerCamera.transform.parent != transform)
        {
            playerCamera.transform.SetParent(transform);
            if (playerCamera.transform.localPosition.y < 0.1f)
            {
                playerCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
            }
            playerCamera.transform.localRotation = Quaternion.identity;
        }
        
        // Setup weapon holder if not assigned (after camera is set up)
        if (weaponHolder == null)
        {
            GameObject holderObj = new GameObject("WeaponHolder");
            // Parent to camera if available, otherwise to player transform
            holderObj.transform.SetParent(playerCamera != null ? playerCamera.transform : transform);
            holderObj.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f); // Position in front of camera
            holderObj.transform.localRotation = Quaternion.identity;
            weaponHolder = holderObj.transform;
        }
        else
        {
            // Ensure weapon holder is parented to camera if camera exists
            if (playerCamera != null && weaponHolder.parent != playerCamera.transform)
            {
                weaponHolder.SetParent(playerCamera.transform);
                weaponHolder.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                weaponHolder.localRotation = Quaternion.identity;
            }
        }
        
        // Ensure weapon holder is active
        if (weaponHolder != null)
        {
            weaponHolder.gameObject.SetActive(true);
            Debug.Log($"Weapon holder created at: {weaponHolder.position}, local: {weaponHolder.localPosition}, parent: {(weaponHolder.parent != null ? weaponHolder.parent.name : "null")}");
        }

        // Initialize starting weapons
        if (startingWeapons != null && startingWeapons.Length > 0)
        {
            for (int i = 0; i < Mathf.Min(startingWeapons.Length, maxWeapons); i++)
            {
                if (startingWeapons[i] != null)
                {
                    AddWeapon(startingWeapons[i], i);
                }
            }
        }

        // Initialize vertical rotation from camera
        if (playerCamera != null)
        {
            verticalRotation = playerCamera.transform.localEulerAngles.x;
            // Normalize to -180 to 180 range
            if (verticalRotation > 180f)
                verticalRotation -= 360f;
        }
    }

    private void OnEnable()
    {
        // Ensure InputActionAsset is enabled first
        if (inputActionsAsset != null)
        {
            inputActionsAsset.Enable();
        }

        // Enable the player action map if it exists
        if (playerActionMap != null)
        {
            playerActionMap.Enable();

            if (moveAction != null)
            {
                moveAction.performed += OnMove;
                moveAction.canceled += OnMove;
            }

            if (lookAction != null)
            {
                lookAction.performed += OnLook;
                lookAction.canceled += OnLook;
            }

            if (sprintAction != null)
            {
                sprintAction.performed += OnSprint;
                sprintAction.canceled += OnSprint;
            }

            if (jumpAction != null)
            {
                jumpAction.performed += OnJump;
                jumpAction.canceled += OnJump;
            }

            if (attackAction != null)
            {
                attackAction.performed += OnAttack;
                attackAction.canceled += OnAttack;
            }

            if (previousWeaponAction != null)
            {
                previousWeaponAction.performed += OnPreviousWeapon;
            }

            if (nextWeaponAction != null)
            {
                nextWeaponAction.performed += OnNextWeapon;
            }

            if (interactAction != null)
            {
                interactAction.performed += OnInteract;
                interactAction.canceled += OnInteract;
            }
        }

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
        }

        if (lookAction != null)
        {
            lookAction.performed -= OnLook;
            lookAction.canceled -= OnLook;
        }

        if (sprintAction != null)
        {
            sprintAction.performed -= OnSprint;
            sprintAction.canceled -= OnSprint;
        }

        if (jumpAction != null)
        {
            jumpAction.performed -= OnJump;
            jumpAction.canceled -= OnJump;
        }

        if (attackAction != null)
        {
            attackAction.performed -= OnAttack;
            attackAction.canceled -= OnAttack;
        }

        if (previousWeaponAction != null)
        {
            previousWeaponAction.performed -= OnPreviousWeapon;
        }

        if (nextWeaponAction != null)
        {
            nextWeaponAction.performed -= OnNextWeapon;
        }

        if (interactAction != null)
        {
            interactAction.performed -= OnInteract;
            interactAction.canceled -= OnInteract;
        }

        if (playerActionMap != null)
        {
            playerActionMap.Disable();
        }

        // Disable the InputActionAsset
        if (inputActionsAsset != null)
        {
            inputActionsAsset.Disable();
        }

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.performed;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        jumpPressed = context.performed;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        attackPressed = context.performed;
        // Track if attack was just pressed this frame (for single fire)
        attackPressedThisFrame = context.performed;
    }

    private void OnPreviousWeapon(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SwitchToPreviousWeapon();
        }
    }

    private void OnNextWeapon(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SwitchToNextWeapon();
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        interactPressed = context.performed;
    }

    private void Update()
    {
        // Ensure cursor stays locked during gameplay
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        HandleMouseLook();
        HandleMovement();
        HandleAttack();
        HandleInteract();
    }

    private void HandleMouseLook()
    {
        // Read mouse delta directly - this is more reliable for mouse look
        Vector2 currentLookInput = Vector2.zero;

        // Priority: Read directly from mouse device (most reliable for mouse look)
        if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
        {
            currentLookInput = Mouse.current.delta.ReadValue();
        }

        // Fallback: Try Input Action if mouse isn't available
        if (currentLookInput.sqrMagnitude < 0.01f && lookAction != null && lookAction.enabled)
        {
            currentLookInput = lookAction.ReadValue<Vector2>();
        }

        if (currentLookInput.sqrMagnitude < 0.01f)
            return;

        // Apply mouse sensitivity scaling (mouse delta is in pixels, scale appropriately)
        // Typical mouse sensitivity: 0.1 to 0.5 works well for pixel-to-degree conversion
        float sensitivityScale = 0.1f;
        Vector2 scaledInput = currentLookInput * sensitivityScale * mouseSensitivity;

        // Horizontal rotation (Y-axis) - rotate the player body
        horizontalRotation += scaledInput.x;

        // Apply horizontal rotation to the player transform (Y-axis only for player body)
        transform.rotation = Quaternion.Euler(0, horizontalRotation, 0);

        // Vertical rotation (X-axis) - rotate only the camera
        verticalRotation -= scaledInput.y;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);

        // Apply vertical rotation directly to the camera
        if (playerCamera != null)
        {
            if (playerCamera.transform.parent == transform)
            {
                // Camera is direct child of player - use local rotation
                playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
            }
            else
            {
                // Camera is somewhere else - use world space relative to player
                playerCamera.transform.rotation = transform.rotation * Quaternion.Euler(verticalRotation, 0, 0);
            }
        }
    }

    private void HandleMovement()
    {
        // Check if grounded
        bool isGrounded = characterController.isGrounded;

        // Handle jump
        if (jumpPressed && isGrounded)
        {
            velocity.y = jumpForce;
            jumpPressed = false; // Consume the jump input
        }

        // Reset vertical velocity when grounded (unless jumping)
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        // Calculate movement direction relative to player's rotation
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        // Apply movement speed (sprint if holding sprint button)
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleAttack()
    {
        if (currentWeaponIndex < 0 || currentWeaponIndex >= weaponInventory.Length)
            return;

        Weapon currentWeapon = weaponInventory[currentWeaponIndex];
        if (currentWeapon == null)
            return;

        // Check fire mode and handle accordingly
        bool shouldAttack = false;
        
        if (currentWeapon.FireMode == FireMode.SingleFire)
        {
            // Single fire: only attack on button press (not while held)
            shouldAttack = attackPressedThisFrame;
            attackPressedThisFrame = false; // Reset after checking
        }
        else // FullAuto
        {
            // Full auto: attack while button is held
            shouldAttack = attackAction != null && attackAction.IsPressed();
        }

        if (shouldAttack)
        {
            // Try to attack - the weapon's cooldown will handle rate limiting
            currentWeapon.Attack();
        }
    }

    private void HandleInteract()
    {
        if (interactPressed)
        {
            Debug.Log("Interact pressed - attempting to pick up weapon");
            TryPickupWeapon();
            interactPressed = false; // Reset interact input
        }
    }

    /// <summary>
    /// Attempts to find and pick up a weapon from the ground.
    /// </summary>
    private void TryPickupWeapon()
    {
        // Get the player's position (use camera position for more accurate detection)
        Vector3 checkPosition = playerCamera != null ? playerCamera.transform.position : transform.position;
        
        Debug.Log($"Checking for weapons at position: {checkPosition}, range: {pickupRange}, layer mask: {weaponLayerMask.value}");
        
        // Use overlap sphere to find nearby weapons - try without layer mask first if nothing found
        Collider[] colliders = Physics.OverlapSphere(checkPosition, pickupRange, weaponLayerMask);
        
        // If no colliders found with layer mask, try without layer mask
        if (colliders.Length == 0)
        {
            Debug.Log("No colliders found with layer mask, trying without layer mask");
            colliders = Physics.OverlapSphere(checkPosition, pickupRange);
        }
        
        Debug.Log($"Found {colliders.Length} colliders in range");
        
        GroundWeapon nearestGroundWeapon = null;
        float nearestDistance = float.MaxValue;

        // Find the nearest ground weapon
        foreach (Collider col in colliders)
        {
            if (col == null) continue;
            
            // Check for GroundWeapon component on this object or parent
            GroundWeapon groundWeapon = col.GetComponent<GroundWeapon>();
            if (groundWeapon == null)
            {
                groundWeapon = col.GetComponentInParent<GroundWeapon>();
            }
            
            if (groundWeapon != null)
            {
                float distance = Vector3.Distance(checkPosition, groundWeapon.transform.position);
                
                // Use the larger of pickupRange or the weapon's pickup radius
                float effectiveRange = Mathf.Max(pickupRange, groundWeapon.PickupRadius);
                
                Debug.Log($"Found GroundWeapon: {groundWeapon.name}, distance: {distance}, effective range: {effectiveRange}");
                
                if (distance <= effectiveRange && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestGroundWeapon = groundWeapon;
                }
            }
        }

        if (nearestGroundWeapon == null)
        {
            Debug.LogWarning("No ground weapon found in range");
            return;
        }
        
        Debug.Log($"Found nearest weapon: {nearestGroundWeapon.name}");

        Weapon weaponToPickup = nearestGroundWeapon.Weapon;
        if (weaponToPickup == null)
            return;

        // Check if there's an empty slot
        int emptySlot = FindEmptySlot();
        
        if (emptySlot >= 0)
        {
            // Pick up weapon into empty slot
            PickupWeapon(weaponToPickup, emptySlot);
            nearestGroundWeapon.OnPickedUp();
            Debug.Log($"Picked up {weaponToPickup.WeaponName}");
        }
        else
        {
            // Inventory is full - swap with currently equipped weapon
            if (currentWeaponIndex >= 0 && currentWeaponIndex < weaponInventory.Length)
            {
                Weapon currentWeapon = weaponInventory[currentWeaponIndex];
                if (currentWeapon != null)
                {
                    SwapWeapon(weaponToPickup, currentWeapon, currentWeaponIndex);
                    nearestGroundWeapon.OnPickedUp();
                    Debug.Log($"Swapped {currentWeapon.WeaponName} for {weaponToPickup.WeaponName}");
                }
            }
            else
            {
                // No weapon equipped, find first slot to swap
                for (int i = 0; i < weaponInventory.Length; i++)
                {
                    if (weaponInventory[i] != null)
                    {
                        Weapon weaponToDrop = weaponInventory[i];
                        SwapWeapon(weaponToPickup, weaponToDrop, i);
                        nearestGroundWeapon.OnPickedUp();
                        Debug.Log($"Swapped {weaponToDrop.WeaponName} for {weaponToPickup.WeaponName}");
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds the first empty slot in the inventory. Returns -1 if inventory is full.
    /// </summary>
    private int FindEmptySlot()
    {
        for (int i = 0; i < weaponInventory.Length; i++)
        {
            if (weaponInventory[i] == null)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Picks up a weapon and adds it to the inventory.
    /// </summary>
    private void PickupWeapon(Weapon weapon, int slot)
    {
        AddWeapon(weapon, slot);
    }

    /// <summary>
    /// Swaps a weapon in the inventory with a weapon from the ground.
    /// </summary>
    private void SwapWeapon(Weapon weaponToPickup, Weapon weaponToDrop, int slot)
    {
        // Remove the weapon from inventory
        RemoveWeapon(slot);

        // Drop the old weapon on the ground
        DropWeapon(weaponToDrop);

        // Add the new weapon
        AddWeapon(weaponToPickup, slot);
    }

    /// <summary>
    /// Drops a weapon on the ground at the player's position.
    /// </summary>
    private void DropWeapon(Weapon weapon)
    {
        if (weapon == null)
            return;

        // Remove from weapon holder
        weapon.transform.SetParent(null);

        // Position weapon in front of player
        Vector3 dropPosition = transform.position + transform.forward * 1f;
        dropPosition.y = transform.position.y; // Keep at ground level
        
        weapon.transform.position = dropPosition;
        weapon.transform.rotation = Quaternion.identity;

        // Ensure weapon has a collider for pickup detection
        Collider weaponCollider = weapon.GetComponent<Collider>();
        if (weaponCollider == null)
        {
            // Add a box collider if none exists
            BoxCollider boxCollider = weapon.gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = false; // Not a trigger so it can be detected
        }
        else
        {
            weaponCollider.enabled = true;
        }

        // Add GroundWeapon component so it can be picked up again
        GroundWeapon groundWeapon = weapon.gameObject.GetComponent<GroundWeapon>();
        if (groundWeapon == null)
        {
            groundWeapon = weapon.gameObject.AddComponent<GroundWeapon>();
        }

        // Unequip the weapon
        weapon.OnUnequip();
    }

    /// <summary>
    /// Switches to the previous weapon in the inventory.
    /// </summary>
    public void SwitchToPreviousWeapon()
    {
        // Don't allow swapping during animation
        if (isSwappingWeapon)
            return;

        if (weaponInventory == null || weaponInventory.Length == 0)
            return;

        int nextIndex = currentWeaponIndex;
        int attempts = 0;

        do
        {
            nextIndex = (nextIndex - 1 + weaponInventory.Length) % weaponInventory.Length;
            attempts++;
            
            if (attempts >= weaponInventory.Length)
                return; // No weapons available
        }
        while (weaponInventory[nextIndex] == null && attempts < weaponInventory.Length);

        if (weaponInventory[nextIndex] != null)
        {
            SwitchWeapon(nextIndex);
        }
    }

    /// <summary>
    /// Switches to the next weapon in the inventory.
    /// </summary>
    public void SwitchToNextWeapon()
    {
        // Don't allow swapping during animation
        if (isSwappingWeapon)
            return;

        if (weaponInventory == null || weaponInventory.Length == 0)
            return;

        int nextIndex = currentWeaponIndex;
        int attempts = 0;

        do
        {
            nextIndex = (nextIndex + 1) % weaponInventory.Length;
            attempts++;
            
            if (attempts >= weaponInventory.Length)
                return; // No weapons available
        }
        while (weaponInventory[nextIndex] == null && attempts < weaponInventory.Length);

        if (weaponInventory[nextIndex] != null)
        {
            SwitchWeapon(nextIndex);
        }
    }

    /// <summary>
    /// Equips a weapon immediately without animation (used for first weapon).
    /// </summary>
    private void EquipWeaponImmediate(int index)
    {
        if (index < 0 || index >= weaponInventory.Length)
            return;

        if (weaponInventory[index] == null)
            return;

        currentWeaponIndex = index;
        Weapon newWeapon = weaponInventory[currentWeaponIndex];
        if (newWeapon != null)
        {
            // Ensure weapon is parented to weapon holder
            if (newWeapon.transform.parent != weaponHolder)
            {
                newWeapon.transform.SetParent(weaponHolder);
                newWeapon.transform.localPosition = Vector3.zero;
                newWeapon.transform.localRotation = Quaternion.identity;
            }
            
            // Ensure weapon is active and visible
            newWeapon.gameObject.SetActive(true);
            
            // Apply position offset
            newWeapon.OnEquip();
            
            Debug.Log($"Equipped {newWeapon.WeaponName} immediately");
        }
    }

    /// <summary>
    /// Switches to the weapon at the specified index with animation.
    /// </summary>
    private void SwitchWeapon(int index)
    {
        if (index < 0 || index >= weaponInventory.Length)
            return;

        if (weaponInventory[index] == null)
            return;

        // Don't allow swapping to the same weapon
        if (currentWeaponIndex == index)
            return;

        // Stop any existing swap animation
        if (swapAnimationCoroutine != null)
        {
            StopCoroutine(swapAnimationCoroutine);
            swapAnimationCoroutine = null;
        }

        // Start swap animation
        swapAnimationCoroutine = StartCoroutine(AnimateWeaponSwap(index));
    }

    /// <summary>
    /// Coroutine that animates the weapon swap.
    /// </summary>
    private System.Collections.IEnumerator AnimateWeaponSwap(int newWeaponIndex)
    {
        isSwappingWeapon = true;

        Weapon currentWeapon = null;
        Weapon newWeapon = weaponInventory[newWeaponIndex];

        // Get current weapon if one is equipped
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weaponInventory.Length)
        {
            currentWeapon = weaponInventory[currentWeaponIndex];
        }

        // Ensure new weapon is parented and active (but hidden initially)
        if (newWeapon != null)
        {
            if (newWeapon.transform.parent != weaponHolder)
            {
                newWeapon.transform.SetParent(weaponHolder);
            }
            newWeapon.gameObject.SetActive(true);
            
            // Get target position/rotation for new weapon
            Vector3 targetPosition = newWeapon.GetPositionOffset();
            Quaternion targetRotation = Quaternion.Euler(newWeapon.GetRotationOffset());
            
            // Start new weapon from hidden position
            newWeapon.transform.localPosition = targetPosition + swapShowOffset;
            newWeapon.transform.localRotation = targetRotation;
        }

        // Store current weapon's position/rotation if it exists
        Vector3 currentWeaponStartPos = Vector3.zero;
        Quaternion currentWeaponStartRot = Quaternion.identity;
        bool hasCurrentWeapon = false;
        if (currentWeapon != null && currentWeapon.gameObject.activeSelf)
        {
            currentWeaponStartPos = currentWeapon.transform.localPosition;
            currentWeaponStartRot = currentWeapon.transform.localRotation;
            hasCurrentWeapon = true;
        }

        // Animate the swap
        float elapsedTime = 0f;
        while (elapsedTime < swapAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / swapAnimationDuration);
            float curveValue = swapAnimationCurve.Evaluate(normalizedTime);

            // Animate current weapon out (if exists)
            if (hasCurrentWeapon && currentWeapon != null && currentWeapon.gameObject.activeSelf)
            {
                Vector3 hidePosition = currentWeaponStartPos + swapHideOffset;
                currentWeapon.transform.localPosition = Vector3.Lerp(currentWeaponStartPos, hidePosition, curveValue);
                currentWeapon.transform.localRotation = Quaternion.Lerp(currentWeaponStartRot, Quaternion.identity, curveValue);
            }

            // Animate new weapon in
            if (newWeapon != null)
            {
                Vector3 targetPosition = newWeapon.GetPositionOffset();
                Quaternion targetRotation = Quaternion.Euler(newWeapon.GetRotationOffset());
                Vector3 startPosition = targetPosition + swapShowOffset;
                
                newWeapon.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
                newWeapon.transform.localRotation = Quaternion.Lerp(Quaternion.Euler(newWeapon.GetRotationOffset()), targetRotation, curveValue);
            }

            yield return null;
        }

        // Finalize the swap
        // Stop recoil on old weapon and hide it
        if (currentWeapon != null)
        {
            currentWeapon.StopRecoil();
            currentWeapon.OnUnequip();
        }

        // Equip new weapon
        currentWeaponIndex = newWeaponIndex;
        if (newWeapon != null)
        {
            newWeapon.OnEquip();
            Debug.Log($"Switched to {newWeapon.WeaponName}");
        }

        isSwappingWeapon = false;
        swapAnimationCoroutine = null;
    }

    /// <summary>
    /// Adds a weapon to the inventory at the specified slot. Returns true if successful.
    /// </summary>
    public bool AddWeapon(Weapon weapon, int slot = -1)
    {
        if (weapon == null)
            return false;

        // If slot is -1, find the first available slot
        if (slot < 0)
        {
            for (int i = 0; i < weaponInventory.Length; i++)
            {
                if (weaponInventory[i] == null)
                {
                    slot = i;
                    break;
                }
            }
        }

        // Check if slot is valid
        if (slot < 0 || slot >= weaponInventory.Length)
        {
            Debug.LogWarning("Cannot add weapon: Inventory is full or invalid slot specified.");
            return false;
        }

        // If slot is occupied, replace it
        if (weaponInventory[slot] != null)
        {
            if (currentWeaponIndex == slot)
            {
                weaponInventory[slot].OnUnequip();
            }
            // Optionally drop or destroy the old weapon
        }

        // Instantiate weapon if it's a prefab (not in scene)
        Weapon weaponInstance = weapon;
        if (weapon.gameObject.scene.name == null)
        {
            // This is a prefab, instantiate it
            weaponInstance = Instantiate(weapon);
            Debug.Log($"Instantiated weapon prefab: {weaponInstance.WeaponName}");
        }

        // Add the new weapon
        weaponInventory[slot] = weaponInstance;
        
        // Ensure weapon GameObject is active
        weaponInstance.gameObject.SetActive(true);
        
        // Disable collider when in inventory (no longer needs physics)
        Collider weaponCollider = weaponInstance.GetComponent<Collider>();
        if (weaponCollider != null)
        {
            weaponCollider.enabled = false;
        }
        
        // Remove GroundWeapon component if it exists (no longer on ground)
        GroundWeapon groundWeapon = weaponInstance.GetComponent<GroundWeapon>();
        if (groundWeapon != null)
        {
            Destroy(groundWeapon);
        }
        
        // Parent to weapon holder first
        weaponInstance.transform.SetParent(weaponHolder);
        
        // Reset transform before applying offset
        weaponInstance.transform.localPosition = Vector3.zero;
        weaponInstance.transform.localRotation = Quaternion.identity;

        Debug.Log($"Added weapon {weaponInstance.WeaponName} to slot {slot}, weapon holder: {weaponHolder != null}, local pos: {weaponInstance.transform.localPosition}");

        // If no weapon is currently equipped, equip this one immediately (no animation for first weapon)
        if (currentWeaponIndex < 0)
        {
            EquipWeaponImmediate(slot);
        }
        else
        {
            // Otherwise, keep it unequipped
            weaponInstance.OnUnequip();
        }

        return true;
    }

    /// <summary>
    /// Removes a weapon from the inventory at the specified slot.
    /// </summary>
    public bool RemoveWeapon(int slot)
    {
        if (slot < 0 || slot >= weaponInventory.Length)
            return false;

        if (weaponInventory[slot] == null)
            return false;

        // If this is the currently equipped weapon, unequip it first
        if (currentWeaponIndex == slot)
        {
            weaponInventory[slot].OnUnequip();
            currentWeaponIndex = -1;

            // Try to switch to another weapon
            for (int i = 0; i < weaponInventory.Length; i++)
            {
                if (i != slot && weaponInventory[i] != null)
                {
                    SwitchWeapon(i);
                    break;
                }
            }
        }

        weaponInventory[slot] = null;
        return true;
    }

    /// <summary>
    /// Gets the currently equipped weapon.
    /// </summary>
    public Weapon GetCurrentWeapon()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weaponInventory.Length)
        {
            return weaponInventory[currentWeaponIndex];
        }
        return null;
    }

    /// <summary>
    /// Gets the weapon at the specified slot.
    /// </summary>
    public Weapon GetWeapon(int slot)
    {
        if (slot >= 0 && slot < weaponInventory.Length)
        {
            return weaponInventory[slot];
        }
        return null;
    }

    private void OnDestroy()
    {
        // InputActionAsset cleanup is handled by Unity
    }

    private void OnDrawGizmosSelected()
    {
        // Draw pickup range in editor
        Gizmos.color = Color.green;
        Vector3 checkPosition = playerCamera != null && playerCamera.transform != null 
            ? playerCamera.transform.position 
            : transform.position;
        Gizmos.DrawWireSphere(checkPosition, pickupRange);
    }
}

