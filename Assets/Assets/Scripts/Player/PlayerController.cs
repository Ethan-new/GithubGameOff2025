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

    private CharacterController characterController;
    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction jumpAction;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isSprinting;
    private bool jumpPressed;

    private float horizontalRotation = 0f;
    private float verticalRotation = 0f;
    private Vector3 velocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Initialize input actions
        if (inputActionsAsset != null)
        {
            playerActionMap = inputActionsAsset.FindActionMap("Player");
            if (playerActionMap != null)
            {
                moveAction = playerActionMap.FindAction("Move");
                lookAction = playerActionMap.FindAction("Look");
                sprintAction = playerActionMap.FindAction("Sprint");
                jumpAction = playerActionMap.FindAction("Jump");
            }
        }

        // Setup camera - ensure it's a child of the player
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

        if (playerActionMap != null)
        {
            playerActionMap.Disable();
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

    private void OnDestroy()
    {
        // InputActionAsset cleanup is handled by Unity
    }
}

