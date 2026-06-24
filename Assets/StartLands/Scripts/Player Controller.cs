using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PhotonView))]
public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable // 1. Added interface back
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Backward Multiplier Settings")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float backwardSpeedMultiplier = 0.5f; 

    [Header("Landing Recover Settings")]
    [SerializeField] private float landingFreezeDuration = 0.25f; 

    [Header("Dynamic Sliding & Input Lag")]
    [Tooltip("Base friction applied during a slide. Lower numbers = longer slides.")]
    [SerializeField] private float baseBrakingFriction = 6f;
    [Tooltip("How much extra slide momentum is carried from a high speed sprint. Lower numbers = slicker slides.")]
    [SerializeField] private float sprintSlideFactor = 0.4f;
    [Tooltip("The speed threshold below which a slide ends and the player can move in a new direction again.")]
    [SerializeField] private float inputReleaseThreshold = 1.5f;

    [Header("Transition / Deceleration Settings")]
    [SerializeField] private float animationShiftSpeed = 3f; 

    [Header("Mouse Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float upDownRange = 80f;

    [Header("Rotation Snapping Speed")]
    [SerializeField] private float rotationSpeed = 20f; 

    [Header("Required Assignments")]
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Animator animator;
    [Tooltip("Drag the child GameObject that contains your character model/mesh here.")]
    [SerializeField] private Transform playerMesh; 

    // Internal State Tracker Variables
    private CharacterController characterController;
    private PlayerInteraction interactionModule; 
    private Vector3 currentVelocity;
    private bool isGrounded;
    private float verticalRotation = 0f;

    // Air & Momentum Tracking States
    private bool wasInAir = false;
    private float landingFreezeTimer = 0f;
    private Vector3 airborneMomentumDirection = Vector3.zero;
    private float airborneMomentumSpeed = 0f;

    // Smooth Animation & Dynamic Slide Buffers
    private float currentAnimSpeedValue = 0f;
    private Vector3 dynamicActiveMovementVector = Vector3.zero;
    private bool isLockedInSlide = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        interactionModule = GetComponent<PlayerInteraction>(); 

        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (!photonView.IsMine)
        {
            if (cameraHolder != null) cameraHolder.gameObject.SetActive(false);
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool canMove = false;

    // NEW: Added method for PlayerStateManager to safely lock inputs on round resets
    public void ResetForNewRound()
    {
        canMove = false;
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        if (!characterController.enabled) return; // add this line
        HandleMouseLook();
        if (canMove) HandleMovementAndRotation();
    }

    private void HandleMouseLook()
    {
        float horizontalRotationInput = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0f, horizontalRotationInput, 0f);

        float verticalRotationInput = Input.GetAxis("Mouse Y") * mouseSensitivity;
        verticalRotation += verticalRotationInput; 
        verticalRotation = Mathf.Clamp(verticalRotation, -upDownRange, upDownRange);

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    private void HandleMovementAndRotation()
    {
        isGrounded = characterController.isGrounded;

        // --- LANDING DETECTION ENGINE WITH SLOPE SAFETY GATE ---
        if (isGrounded)
        {
            if (wasInAir)
            {
                if (currentVelocity.y < -5.0f)
                {
                    landingFreezeTimer = landingFreezeDuration;
                    dynamicActiveMovementVector = Vector3.zero; 
                    isLockedInSlide = false;
                }
                
                wasInAir = false;
                if (animator != null) animator.SetBool("IsJumping", false);
                airborneMomentumDirection = Vector3.zero;
                airborneMomentumSpeed = 0f;
            }

            if (currentVelocity.y < 0)
            {
                currentVelocity.y = -2f; 
            }
        }
        else
        {
            if (currentVelocity.y < -3.0f)
            {
                wasInAir = true;
            }
        }

        if (landingFreezeTimer > 0f)
        {
            landingFreezeTimer -= Time.deltaTime;
        }

        // Gather raw axes WASD inputs
        float moveX = Input.GetAxisRaw("Horizontal"); 
        float moveZ = Input.GetAxisRaw("Vertical"); // FIXED: Re-added missing string parameter call
        
        // --- SPRINT GATE LOGIC ---
        bool isPressingBackward = moveZ < 0f;
        bool isHoldingObject = (interactionModule != null) && interactionModule.IsHoldingObject;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && !isPressingBackward && !isHoldingObject;

        Vector3 forwardDirection = -transform.forward; 
        Vector3 rightDirection = transform.right;      

        Vector3 targetInputDirection = Vector3.zero;
        float targetSpeedParam = 0f; 
        float targetMeshYRotation = 0f;
        float appliedSpeedMultiplier = 1.0f; 
        
        bool hasInput = (moveX != 0 || moveZ != 0);

        // --- DIRECTIONAL LAG CONTROLLER ---
        if (isGrounded && isLockedInSlide)
        {
            if (dynamicActiveMovementVector.magnitude <= inputReleaseThreshold)
            {
                isLockedInSlide = false; 
            }
            else
            {
                hasInput = false; 
            }
        }

        // --- DIRECTIONAL INPUT COMBINATIONS MATRIX ---
        if (hasInput && landingFreezeTimer <= 0f && isGrounded)
        {
            if (moveZ > 0) // W Keys
            {
                if (moveX < 0) // W + A
                {
                    targetInputDirection = forwardDirection + rightDirection;
                    targetMeshYRotation = -45f;
                }
                else if (moveX > 0) // W + D
                {
                    targetInputDirection = forwardDirection - rightDirection;
                    targetMeshYRotation = 45f;
                }
                else // Pure W
                {
                    targetInputDirection = forwardDirection;
                    targetMeshYRotation = 0f;
                }
                
                targetSpeedParam = isRunning ? 1.0f : 0.5f;
            }
            else if (moveZ < 0) // S Keys
            {
                if (moveX < 0) // A + S
                {
                    targetInputDirection = -forwardDirection + rightDirection;
                    targetMeshYRotation = 45f; 
                }
                else if (moveX > 0) // S + D
                {
                    targetInputDirection = -forwardDirection - rightDirection;
                    targetMeshYRotation = -45f; 
                }
                else // Pure S
                {
                    targetInputDirection = -forwardDirection;
                    targetMeshYRotation = 0f; 
                }
            
                appliedSpeedMultiplier = backwardSpeedMultiplier;
                targetSpeedParam = -0.5f; 
            }
            else // Pure Horizontal Movements (A or D Only)
            {
                if (moveX < 0) // Pure A
                {
                    targetInputDirection = rightDirection;
                    targetMeshYRotation = -90f;
                }
                else if (moveX > 0) // Pure D
                {
                    targetInputDirection = -rightDirection;
                    targetMeshYRotation = 90f;
                }
            
                targetSpeedParam = isRunning ? 1.0f : 0.5f; 
            }

            if (targetInputDirection.magnitude > 1f)
            {
                targetInputDirection.Normalize();
            }

            float allocatedSpeed = (isRunning ? runSpeed : walkSpeed) * appliedSpeedMultiplier;
            dynamicActiveMovementVector = targetInputDirection * allocatedSpeed;

            airborneMomentumDirection = targetInputDirection;
            airborneMomentumSpeed = allocatedSpeed;
        }

        // --- DYNAMIC SLIDE FRICTION WITH CORRECTED REVERSE FILTER ---
        if (isGrounded)
        {
            if (!hasInput || landingFreezeTimer > 0f)
            {
                float backwardCheck = Vector3.Dot(dynamicActiveMovementVector.normalized, transform.forward);

                if (backwardCheck > 0.5f) 
                {
                    dynamicActiveMovementVector = Vector3.zero;
                    isLockedInSlide = false;
                }
                else
                {
                    if (dynamicActiveMovementVector.magnitude > inputReleaseThreshold + 1f && !isLockedInSlide)
                    {
                        isLockedInSlide = true;
                    }

                    float speedRatio = Mathf.Clamp01(dynamicActiveMovementVector.magnitude / runSpeed);
                    float dynamicFriction = baseBrakingFriction * Mathf.Lerp(1.0f, sprintSlideFactor, speedRatio);

                    dynamicActiveMovementVector = Vector3.Lerp(dynamicActiveMovementVector, Vector3.zero, dynamicFriction * Time.deltaTime);
                }

                targetSpeedParam = 0f; 
            }
        }
        else
        {
            dynamicActiveMovementVector = airborneMomentumDirection * airborneMomentumSpeed;
        }

        // --- DYNAMIC ANIMATION SHIFT BUFFER ---
        currentAnimSpeedValue = Mathf.MoveTowards(currentAnimSpeedValue, targetSpeedParam, animationShiftSpeed * Time.deltaTime);

        if (isGrounded && hasInput && landingFreezeTimer <= 0f && !isLockedInSlide)
        {
            bool crossingForwardToBack = (targetSpeedParam < 0 && currentAnimSpeedValue > -0.1f);
            bool crossingBackToForward = (targetSpeedParam > 0 && currentAnimSpeedValue < 0.1f);

            if (!crossingForwardToBack && !crossingBackToForward)
            {
                if (playerMesh != null)
                {
                    Quaternion targetRotation = Quaternion.Euler(0f, targetMeshYRotation, 0f);
                    playerMesh.localRotation = Quaternion.Slerp(playerMesh.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }
        }

        if (dynamicActiveMovementVector != Vector3.zero)
        {
            characterController.Move(dynamicActiveMovementVector * Time.deltaTime);
        }

        if (isGrounded && animator != null)
        {
            animator.SetFloat("Speed", currentAnimSpeedValue);
        }

        // --- JUMP ACTION GATE ---
        if (Input.GetButtonDown("Jump") && isGrounded && landingFreezeTimer <= 0f && !isLockedInSlide)
        {
            if (!hasInput) airborneMomentumSpeed = 0f;

            currentVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            
            if (animator != null) animator.SetBool("IsJumping", true);
        }

        currentVelocity.y += gravity * Time.deltaTime;
        characterController.Move(currentVelocity * Time.deltaTime);
    }

    // --- 2. NETWORKING STREAM SERIALIZER OVERRIDE ---
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(playerMesh.localRotation);
        }
        else
        {
            if (playerMesh != null)
            {
                playerMesh.localRotation = (Quaternion)stream.ReceiveNext();
            }
            else
            {
                stream.ReceiveNext(); 
            }
        }
    }
}