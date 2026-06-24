using UnityEngine;

public class CameraBob : MonoBehaviour
{
    [Header("Walk Bob Settings")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;

    [Header("Sprint Bob Settings")]
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.1f;

    [Header("Jump Landing Impact Settings")]
    [SerializeField] private float landingImpactAmount = 0.2f;    
    [SerializeField] private float landingImpactSmoothing = 10f; 

    [Header("Position Smoothing")]
    [SerializeField] private float bobSmoothing = 10f;

    // Component dependencies
    private CharacterController characterController;

    // Internal Math Tracking Variables
    private Vector3 originalLocalPosition;
    private float timer = 0f;
    private bool wasInAir = false;
    private float currentLandingOffset = 0f;

    void Start()
    {
        originalLocalPosition = transform.localPosition;
        characterController = GetComponentInParent<CharacterController>();
    }

    void Update()
    {
        HandleLandingImpact();
        HandleHeadBob();
    }

    private void HandleHeadBob()
    {
        // Read movement input keys
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        
        bool isGrounded = (characterController != null) ? characterController.isGrounded : true;
        bool isMoving = (moveX != 0f || moveZ != 0f) && isGrounded;

        float targetSpeed = walkBobSpeed;
        float targetAmount = walkBobAmount;

        // --- CAMERA SPRINT BOB GATE BLOCK ---
        // Only allow sprint bob intensities if holding Shift AND NOT pressing the S key (moveZ <= 0 block)
        bool isPressingBackward = moveZ < 0f;
        if (Input.GetKey(KeyCode.LeftShift) && isMoving && !isPressingBackward)
        {
            targetSpeed = sprintBobSpeed;
            targetAmount = sprintBobAmount;
        }

        Vector3 targetLocalPos;

        if (isMoving)
        {
            timer += targetSpeed * Time.deltaTime;
            
            float newY = originalLocalPosition.y + Mathf.Sin(timer) * targetAmount;
            float newX = originalLocalPosition.x + Mathf.Cos(timer / 2f) * targetAmount * 0.5f; 

            targetLocalPos = new Vector3(newX, newY, originalLocalPosition.z);
        }
        else
        {
            timer = 0f;
            targetLocalPos = originalLocalPosition;
        }

        targetLocalPos.y -= currentLandingOffset;

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, bobSmoothing * Time.deltaTime);
    }

    private void HandleLandingImpact()
    {
        if (characterController == null) return;

        if (characterController.isGrounded)
        {
            if (wasInAir)
            {
                currentLandingOffset = landingImpactAmount;
                wasInAir = false;
            }
            currentLandingOffset = Mathf.Lerp(currentLandingOffset, 0f, landingImpactSmoothing * Time.deltaTime);
        }
        else
        {
            wasInAir = true;
        }
    }
}