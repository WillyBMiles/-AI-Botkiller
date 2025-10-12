using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -20f;
    
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 90f;
    [SerializeField] private Transform cameraTransform;
    
    // Components
    private CharacterController characterController;
    
    // Movement variables
    private Vector2 moveInput;
    private Vector3 currentVelocity;
    private Vector3 targetVelocity;
    private float verticalVelocity;
    
    // Look variables
    private Vector2 lookInput;
    private float cameraPitch = 0f;
    
    // Ground check
    private bool isGrounded;
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = GetComponentInChildren<Camera>()?.transform;
            if (cameraTransform == null)
            {
                Debug.LogError("PlayerController: No camera found! Please assign a camera transform.");
            }
        }
    }
    
    private void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleMouseLook();
    }
    
    private void HandleGroundCheck()
    {
        // CharacterController's built-in ground check
        isGrounded = characterController.isGrounded;

        Debug.Log(isGrounded);
        // Reset vertical velocity when grounded
        if (isGrounded && verticalVelocity <= 0)
        {
            verticalVelocity = -2f; // Small downward force to keep grounded
        }
    }
    
    private void HandleMovement()
    {
        // Calculate target velocity based on input
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        targetVelocity = moveDirection * moveSpeed;
        
        // Smoothly interpolate to target velocity
        float lerpSpeed = moveInput.magnitude > 0 ? acceleration : deceleration;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, lerpSpeed * Time.deltaTime);
        
        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;
        
        // Combine horizontal and vertical movement
        Vector3 finalMovement = currentVelocity + Vector3.up * verticalVelocity;
        
        // Move the character
        characterController.Move(finalMovement * Time.deltaTime);
    }
    
    private void HandleMouseLook()
    {
        if (cameraTransform == null) return;
        
        // Horizontal rotation (rotate player body)
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);
        
        // Vertical rotation (rotate camera)
        cameraPitch -= lookInput.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }
    
    #region Input Callbacks
    
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
    
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            // Calculate jump velocity using physics formula: v = sqrt(2 * h * g)
            verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * -gravity);
        }
    }
    
    #endregion
}
