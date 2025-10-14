using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float airControl = 0.3f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private bool enableDoubleJump = true;
    [SerializeField] private float doubleJumpHeight = 2f;
    [SerializeField] private float doubleJumpDirectionalBoost = 5f;
    
    [Header("Wall Run Settings")]
    [SerializeField] private bool enableWallRun = true;
    [SerializeField] private float wallRunMinAngle = 30f;
    [SerializeField] private float wallRunGravity = -8f;
    [SerializeField] private float wallRunSlideGravity = -15f;
    [SerializeField] private float wallJumpUpForce = 12f;
    [SerializeField] private float wallJumpOutForce = 10f;
    [SerializeField] private float wallJumpForwardForce = 8f;
    [SerializeField] private float wallCheckDistance = 0.7f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallRunCameraTilt = 15f;
    [SerializeField] private float wallRunMaxSpeed = 10f;
    [SerializeField] private float wallRunInitialUpwardBoost = 3f;
    
    [Header("Slide Settings")]
    [SerializeField] private bool enableSlide = true;
    [SerializeField] private float slideMinSpeed = 4f;
    [SerializeField] private float slideDeceleration = 5f;
    [SerializeField] private float slideHeightReduction = 0.5f;
    [SerializeField] private float slideCameraLower = 0.5f;
    [SerializeField] private float slideCameraTransitionSpeed = 10f;
    [SerializeField] private float slideJumpBoostTime = 0.5f;
    [SerializeField] private float slideJumpBoostMultiplier = 1.5f;
    [SerializeField] private LayerMask slideHeadCheckLayerMask;
    
    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 90f;
    [SerializeField] private float lookSmoothing = 10f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform cameraParentTransform;
    
    [Header("Head Bob Settings")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float bobFrequency = 10f;
    [SerializeField] private float bobHorizontalAmplitude = 0.05f;
    [SerializeField] private float bobVerticalAmplitude = 0.08f;
    [SerializeField] private float landingBobAmount = 0.15f;
    
    [Header("Gun Bob Settings")]
    [SerializeField] private Transform gunTransform;
    [SerializeField] private bool enableGunBob = true;
    [SerializeField] private float gunBobFrequency = 1f;
    [SerializeField] private float gunBobPositionAmount = 0.02f;
    [SerializeField] private float gunBobRotationAmount = 2f;
    [SerializeField] private float gunLandingBobAmount = 0.3f;
    [SerializeField] private float gunJumpBobAmount = 0.2f;
    [SerializeField] private float gunJumpBobAmountRotation = 0.2f;
    
    [Header("Gun Recoil Settings")]
    [SerializeField] private float recoilPositionAmount = 0.1f;
    [SerializeField] private float recoilRotationAmount = 5f;
    [SerializeField] private float recoilRecoverySpeed = 10f;

    // Components
    private CharacterController characterController;
    private CapsuleCollider capsuleCollider;
    
    // Movement variables
    private Vector2 moveInput;
    private Vector3 currentVelocity;
    private Vector3 targetVelocity;
    private float verticalVelocity;
    
    // Jump variables
    private int jumpsRemaining;
    
    // Look variables
    private Vector2 lookInput;
    private float cameraPitch = 0f;
    private float targetCameraPitch = 0f;
    private float targetYaw = 0f;
    private float currentYaw = 0f;
    
    // Head bob variables
    private float bobTimer = 0f;
    private Vector3 cameraStartPosition;
    private Vector3 cameraParentStartPosition;
    private bool wasGrounded = true;
    
    // Gun bob variables
    private Vector3 gunStartPosition;
    private Quaternion gunStartRotation;
    
    // Gun recoil variables
    private Vector3 gunRecoilOffset = Vector3.zero;
    private Quaternion gunRecoilRotation = Quaternion.identity;
    private bool isRecoiling = false;
    
    // Ground check
    private bool isGrounded;
    
    // Wall run variables
    private bool isWallRunning;
    private bool isWallRight;
    private bool isWallLeft;
    private RaycastHit wallHit;
    private Vector3 wallNormal;
    
    // Slide variables
    private bool isSliding;
    private bool slideInput;
    private Vector3 slideDirection;
    private float slideSpeed;
    private float slideTimer;
    private float originalHeight;
    private float originalRadius;
    private Vector3 originalCenter;
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        
        // Store original character controller dimensions
        originalHeight = characterController.height;
        originalRadius = characterController.radius;
        originalCenter = characterController.center;
        
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
        
        // Store initial camera position for head bob
        if (cameraTransform != null)
        {
            cameraStartPosition = cameraTransform.localPosition;
        }
        
        // Store initial camera parent position for slide
        if (cameraParentTransform != null)
        {
            cameraParentStartPosition = cameraParentTransform.localPosition;
        }
        
        // Store initial gun position and rotation for gun bob
        if (gunTransform != null)
        {
            gunStartPosition = gunTransform.localPosition;
            gunStartRotation = gunTransform.localRotation;
        }
    }
    
    private void Update()
    {
        HandleGroundCheck();
        CheckForWall();
        HandleSlide();
        HandleMovement();
        HandleMouseLook();
        HandleHeadBob();
        HandleGunBob();
    }
    
    private void HandleGroundCheck()
    {
        // CharacterController's built-in ground check
        isGrounded = characterController.isGrounded;

        // Reset vertical velocity when grounded
        if (isGrounded && verticalVelocity <= 0)
        {
            verticalVelocity = -2f; // Small downward force to keep grounded
            jumpsRemaining = enableDoubleJump ? 2 : 1; // Reset jumps when grounded
        }
        
        // Exit wall run when grounded
        if (isGrounded && isWallRunning)
        {
            isWallRunning = false;
        }
    }
    
    private void HandleSlide()
    {
        if (!enableSlide) return;

        // Check if we should start sliding
        if (slideInput && isGrounded && !isSliding && currentVelocity.magnitude >= slideMinSpeed)
        {
            StartSlide();
        }
        
        // Check if we should stop sliding
        if (isSliding && (!slideInput || !isGrounded))
        {
            // Only stop if there's enough space to stand up
            if (CanStandUp())
            {
                StopSlide();
            }
        }
        
        // Update slide if active
        if (isSliding)
        {
            // Decelerate slide speed
            slideSpeed = Mathf.Max(slideSpeed - slideDeceleration * Time.deltaTime, 0f);
            
            // Track slide duration
            slideTimer += Time.deltaTime;
        }
        
        // Smoothly interpolate camera parent position
        if (cameraParentTransform != null)
        {
            Vector3 targetPos = isSliding ? cameraParentStartPosition + Vector3.down * slideCameraLower : cameraParentStartPosition;
            cameraParentTransform.localPosition = Vector3.Lerp(cameraParentTransform.localPosition, targetPos, Time.deltaTime * slideCameraTransitionSpeed);
        }
    }
    
    private void StartSlide()
    {
        isSliding = true;
        slideDirection = currentVelocity.normalized;
        slideSpeed = currentVelocity.magnitude;
        slideTimer = 0f;
        
        // Reduce character controller in all dimensions
        characterController.height = originalHeight * slideHeightReduction;
        characterController.radius = originalRadius * slideHeightReduction;
        characterController.center = new Vector3(originalCenter.x, originalCenter.y * slideHeightReduction, originalCenter.z);
        
        // Reduce capsule collider if present
        if (capsuleCollider != null)
        {
            capsuleCollider.height = originalHeight * slideHeightReduction;
            capsuleCollider.radius = originalRadius * slideHeightReduction;
            capsuleCollider.center = new Vector3(originalCenter.x, originalCenter.y * slideHeightReduction, originalCenter.z);
        }
    }
    
    private void StopSlide()
    {
        if (!isSliding) return;
        
        isSliding = false;
        slideTimer = 0f;
        
        // Restore character controller dimensions
        characterController.height = originalHeight;
        characterController.radius = originalRadius;
        characterController.center = originalCenter;
        
        // Restore capsule collider if present
        if (capsuleCollider != null)
        {
            capsuleCollider.height = originalHeight;
            capsuleCollider.radius = originalRadius;
            capsuleCollider.center = originalCenter;
        }
    }
    
    private bool CanStandUp()
    {
        // Calculate the position where the top of the character would be when standing
        Vector3 checkPosition = transform.position + Vector3.up * originalHeight / 2f;
        
        // Check if there's an obstacle above using a sphere cast
        bool hasObstacle = Physics.CheckSphere(checkPosition, originalRadius * 0.5f, slideHeadCheckLayerMask);
        
        return !hasObstacle;
    }
    
    private void CheckForWall()
    {
        if (!enableWallRun || isGrounded) return;
        
        // Check for walls on both sides
        isWallRight = Physics.Raycast(transform.position, transform.right, out RaycastHit rightHit, wallCheckDistance, wallLayer);
        isWallLeft = Physics.Raycast(transform.position, -transform.right, out RaycastHit leftHit, wallCheckDistance, wallLayer);
        
        // Determine which wall we hit
        if (isWallRight)
        {
            wallHit = rightHit;
            wallNormal = rightHit.normal;
        }
        else if (isWallLeft)
        {
            wallHit = leftHit;
            wallNormal = leftHit.normal;
        }
        
        // Check if we can start wall running
        if ((isWallRight || isWallLeft) && !isGrounded)
        {
            // Calculate angle between player velocity and wall
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            float angle = Vector3.Angle(horizontalVelocity, -wallNormal);
            
            // Only start wall run if angle is shallow enough
            if (angle < wallRunMinAngle && horizontalVelocity.magnitude > 1f)
            {
                if (!isWallRunning)
                {
                    StartWallRun();
                }
            }
            else if (isWallRunning)
            {
                // Stop wall run if angle becomes too steep
                isWallRunning = false;
            }
        }
        else
        {
            isWallRunning = false;
        }
    }
    
    private void StartWallRun()
    {
        isWallRunning = true;
        // Reset double jump when starting wall run
        jumpsRemaining = enableDoubleJump ? 2 : 1;
        
        // Give upward boost if not already moving upward
        if (verticalVelocity <= wallRunInitialUpwardBoost)
        {
            verticalVelocity = wallRunInitialUpwardBoost;
        }
    }
    
    private void HandleMovement()
    {
        if (isSliding)
        {
            HandleSlideMovement();
        }
        else if (isWallRunning)
        {
            HandleWallRunMovement();
        }
        else
        {
            HandleNormalMovement();
        }
    }
    
    private void HandleSlideMovement()
    {
        // Maintain slide direction and speed
        currentVelocity = slideDirection * slideSpeed;
        
        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;
        
        // Combine horizontal and vertical movement
        Vector3 finalMovement = currentVelocity + Vector3.up * verticalVelocity;
        
        // Move the character
        characterController.Move(finalMovement * Time.deltaTime);
    }
    
    private void HandleNormalMovement()
    {
        // Calculate target velocity based on input
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        targetVelocity = moveDirection * moveSpeed;
        
        // Smoothly interpolate to target velocity
        float lerpSpeed = moveInput.magnitude > 0 ? acceleration : deceleration;
        
        // Reduce control when in the air
        if (!isGrounded)
        {
            lerpSpeed *= airControl;
        }
        
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, lerpSpeed * Time.deltaTime);
        
        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;
        
        // Combine horizontal and vertical movement
        Vector3 finalMovement = currentVelocity + Vector3.up * verticalVelocity;
        
        // Move the character
        characterController.Move(finalMovement * Time.deltaTime);
    }
    
    private void HandleWallRunMovement()
    {
        // Calculate wall forward direction (perpendicular to wall normal)
        Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
        
        // Make sure we're running in the correct direction relative to player's forward
        if (Vector3.Dot(wallForward, transform.forward) < 0)
        {
            wallForward = -wallForward;
        }
        
        // Project current velocity onto wall direction to maintain momentum
        Vector3 wallVelocity = Vector3.Project(currentVelocity, wallForward);
        
        // Apply input to modify wall run direction
        if (moveInput.magnitude > 0.1f)
        {
            Vector3 inputDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            Vector3 wallInputDirection = Vector3.Project(inputDirection, wallForward);
            wallVelocity += wallInputDirection * acceleration * Time.deltaTime;
        }
        
        // Clamp horizontal speed to max wall run speed
        if (wallVelocity.magnitude > wallRunMaxSpeed)
        {
            wallVelocity = wallVelocity.normalized * wallRunMaxSpeed;
        }
        
        // Keep player attached to wall by pushing them slightly into it
        Vector3 wallStick = -wallNormal * 2f;
        
        currentVelocity = wallVelocity + wallStick;
        
        // Apply reduced gravity (or slide gravity if not moving)
        float gravityToApply = moveInput.magnitude > 0.1f ? wallRunGravity : wallRunSlideGravity;
        verticalVelocity += gravityToApply * Time.deltaTime;
        
        // Combine horizontal and vertical movement
        Vector3 finalMovement = currentVelocity + Vector3.up * verticalVelocity;
        
        // Move the character
        characterController.Move(finalMovement * Time.deltaTime);
    }
    
    private void HandleMouseLook()
    {
        if (cameraTransform == null) return;
        
        // Calculate target rotations
        targetYaw += lookInput.x * mouseSensitivity;
        targetCameraPitch -= lookInput.y * mouseSensitivity;
        targetCameraPitch = Mathf.Clamp(targetCameraPitch, -maxLookAngle, maxLookAngle);
        
        // Smoothly interpolate to target rotations
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, lookSmoothing * Time.deltaTime);
        cameraPitch = Mathf.Lerp(cameraPitch, targetCameraPitch, lookSmoothing * Time.deltaTime);
        
        // Calculate camera tilt for wall running
        float targetTilt = 0f;
        if (isWallRunning)
        {
            targetTilt = isWallRight ? -wallRunCameraTilt : wallRunCameraTilt;
        }
        
        // Apply rotations
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        
        // Smoothly interpolate camera tilt
        Vector3 currentEuler = cameraTransform.localRotation.eulerAngles;
        float currentTilt = currentEuler.z > 180 ? currentEuler.z - 360 : currentEuler.z;
        float newTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * 10f);
        
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, newTilt);
    }
    
    private void HandleHeadBob()
    {
        if (!enableHeadBob || cameraTransform == null) return;
        
        // Don't bob while sliding - smoothly return to center
        if (isSliding)
        {
            bobTimer = 0f;
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, cameraStartPosition, Time.deltaTime * slideCameraTransitionSpeed);
            wasGrounded = isGrounded;
            return;
        }
        
        // Check for landing
        if (!wasGrounded && isGrounded)
        {
            // Apply landing bob
            StartCoroutine(LandingBob());
        }
        wasGrounded = isGrounded;
        
        // Bob when moving on the ground OR wall running
        if ((isGrounded && currentVelocity.magnitude > 0.1f) || (isWallRunning && moveInput.magnitude > 0.1f))
        {
            bobTimer += Time.deltaTime * bobFrequency;
            
            // Calculate bob offset
            float horizontalBob = Mathf.Sin(bobTimer) * bobHorizontalAmplitude;
            float verticalBob = Mathf.Sin(bobTimer * 2f) * bobVerticalAmplitude;
            
            Vector3 bobOffset = new Vector3(horizontalBob, verticalBob, 0f);
            cameraTransform.localPosition = cameraStartPosition + bobOffset;
        }
        else
        {
            // Smoothly return to original position when not moving
            bobTimer = 0f;
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, cameraStartPosition, Time.deltaTime * 5f);
        }
    }
    
    private System.Collections.IEnumerator LandingBob()
    {
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 startPos = cameraTransform.localPosition;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Quick dip down and back up
            float bobAmount = Mathf.Sin(t * Mathf.PI) * landingBobAmount;
            cameraTransform.localPosition = startPos + Vector3.down * bobAmount;
            
            yield return null;
        }
        
        cameraTransform.localPosition = cameraStartPosition;
    }
    
    private void HandleGunBob()
    {
        if (!enableGunBob || gunTransform == null) return;
        
        // Check for landing and apply gun landing bob
        if (!wasGrounded && isGrounded)
        {
            StartCoroutine(GunLandingBob());
        }
        
        // Handle recoil recovery
        if (isRecoiling)
        {
            gunRecoilOffset = Vector3.Lerp(gunRecoilOffset, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);
            gunRecoilRotation = Quaternion.Lerp(gunRecoilRotation, Quaternion.identity, Time.deltaTime * recoilRecoverySpeed);
            
            if (gunRecoilOffset.magnitude < 0.001f)
            {
                gunRecoilOffset = Vector3.zero;
                gunRecoilRotation = Quaternion.identity;
                isRecoiling = false;
            }
        }
        
        // Bob when moving on the ground OR wall running
        if ((isGrounded && currentVelocity.magnitude > 0.1f) || (isWallRunning && moveInput.magnitude > 0.1f))
        {
            // Calculate gun bob offset (slightly different phase than camera for more natural feel)
            float horizontalBob = Mathf.Sin(bobTimer * 0.5f * gunBobFrequency) * gunBobPositionAmount;
            float verticalBob = Mathf.Sin(bobTimer * gunBobFrequency) * gunBobPositionAmount;
            
            Vector3 bobOffset = new Vector3(horizontalBob, verticalBob, 0f);
            
            // Calculate gun bob rotation (sway)
            float rollBob = Mathf.Sin(bobTimer * 0.5f * gunBobFrequency) * gunBobRotationAmount;
            float pitchBob = Mathf.Sin(bobTimer * gunBobFrequency) * gunBobRotationAmount * 0.5f;
            
            Quaternion bobRotation = Quaternion.Euler(pitchBob, 0f, rollBob);
            
            // Apply bob + recoil
            gunTransform.localPosition = gunStartPosition + bobOffset + gunRecoilOffset;
            gunTransform.localRotation = gunStartRotation * bobRotation * gunRecoilRotation;
        }
        else
        {
            // Smoothly return to original position when not moving (but keep recoil)
            Vector3 targetPos = gunStartPosition + gunRecoilOffset;
            Quaternion targetRot = gunStartRotation * gunRecoilRotation;
            
            gunTransform.localPosition = Vector3.Lerp(gunTransform.localPosition, targetPos, Time.deltaTime * 5f);
            gunTransform.localRotation = Quaternion.Lerp(gunTransform.localRotation, targetRot, Time.deltaTime * 5f);
        }
    }
    
    private System.Collections.IEnumerator GunLandingBob()
    {
        if (gunTransform == null) yield break;
        
        float elapsed = 0f;
        float duration = 0.25f;
        Vector3 startPos = gunTransform.localPosition;
        Quaternion startRot = gunTransform.localRotation;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Gun kicks up and back down on landing
            float bobAmount = Mathf.Sin(t * Mathf.PI) * gunLandingBobAmount;
            float rotAmount = Mathf.Sin(t * Mathf.PI) * 10f; // Rotation kick
            
            gunTransform.localPosition = startPos + new Vector3(0f, -bobAmount, bobAmount * 0.5f);
            gunTransform.localRotation = startRot * Quaternion.Euler(-rotAmount, 0f, 0f);
            
            yield return null;
        }
        
        gunTransform.localPosition = gunStartPosition;
        gunTransform.localRotation = gunStartRotation;
    }
    
    private System.Collections.IEnumerator GunJumpBob()
    {
        if (gunTransform == null) yield break;
        
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 startPos = gunTransform.localPosition;
        Quaternion startRot = gunTransform.localRotation;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Gun kicks down and back up on jump
            float bobAmount = Mathf.Sin(t * Mathf.PI) * gunJumpBobAmount;
            float rotAmount = Mathf.Sin(t * Mathf.PI) * gunJumpBobAmountRotation; // Rotation kick
            
            gunTransform.localPosition = startPos + new Vector3(0f, bobAmount, -bobAmount * 0.3f);
            gunTransform.localRotation = startRot * Quaternion.Euler(rotAmount, 0f, 0f);
            
            yield return null;
        }
        
        gunTransform.localPosition = gunStartPosition;
        gunTransform.localRotation = gunStartRotation;
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
        if (context.performed && jumpsRemaining > 0)
        {
            // Check for slide jump boost
            bool slideJumpBoost = isSliding && slideTimer >= slideJumpBoostTime;
            
            // Exit slide if jumping
            if (isSliding)
            {
                StopSlide();
            }
            
            // Wall jump
            if (isWallRunning)
            {
                // Jump up and away from wall
                verticalVelocity = wallJumpUpForce;
                
                // Calculate wall forward direction for momentum
                Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
                if (Vector3.Dot(wallForward, transform.forward) < 0)
                {
                    wallForward = -wallForward;
                }
                
                // Combine outward force from wall and forward momentum
                currentVelocity = wallNormal * wallJumpOutForce + wallForward * wallJumpForwardForce;
                
                // Exit wall run
                isWallRunning = false;
                jumpsRemaining--;
            }
            else
            {
                // Normal jump or double jump
                float jumpHeightToUse = (jumpsRemaining == 2) ? jumpHeight : doubleJumpHeight;
                
                // Apply slide jump boost if applicable
                if (slideJumpBoost)
                {
                    jumpHeightToUse *= slideJumpBoostMultiplier;
                }
                
                // Calculate jump velocity using physics formula: v = sqrt(2 * h * g)
                verticalVelocity = Mathf.Sqrt(jumpHeightToUse * 2f * -gravity);
                
                // On double jump, add directional boost based on input
                if (jumpsRemaining == 1)
                {
                    // Apply directional boost if there's movement input
                    if (moveInput.magnitude > 0.1f)
                    {
                        Vector3 boostDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
                        currentVelocity = boostDirection * doubleJumpDirectionalBoost;
                    }
                    else
                    {
                        currentVelocity = Vector3.zero;
                    }
                }
                
                jumpsRemaining--;
            }
            
            // Trigger gun jump bob
            if (enableGunBob && gunTransform != null)
            {
                StartCoroutine(GunJumpBob());
            }
        }
    }
    
    public void OnSlide(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            slideInput = true;
        }
        else if (context.canceled)
        {
            slideInput = false;
        }
    }
    
    public void TriggerGunRecoil()
    {
        if (gunTransform == null) return;
        
        // Add recoil offset (kick back and up slightly)
        gunRecoilOffset = new Vector3(0f, -recoilPositionAmount * 0.3f, -recoilPositionAmount);
        
        // Add recoil rotation (kick up)
        gunRecoilRotation = Quaternion.Euler(-recoilRotationAmount, 0f, 0f);
        
        isRecoiling = true;
    }
    
    #endregion
}
