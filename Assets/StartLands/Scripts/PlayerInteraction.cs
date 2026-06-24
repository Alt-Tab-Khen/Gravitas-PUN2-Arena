using UnityEngine;
using System.Collections;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class PlayerInteraction : MonoBehaviourPun
{
    [Header("Tactile Interaction Settings")]
    [SerializeField] private float maxGrabDistance = 6f;
    [Tooltip("How tightly the object snaps to your hold point. Higher = snappier, Lower = more physics lag/weight.")]
    [SerializeField] private float grabLerpSpeed = 15f;
    [Tooltip("Multiplier for the swing velocity when you release the object.")]
    [SerializeField] private float throwVelocityMultiplier = 3f;
    [SerializeField] private LayerMask boulderLayer;

    [Header("Required Assignments")]
    [SerializeField] private Transform grabPoint;
    [SerializeField] private Transform cameraTransform;

    // PUBLIC ACCESSOR: Tells other scripts (like PlayerController) if we are carrying weight
    public bool IsHoldingObject => heldRigidbody != null;

    // Internal State Trackers
    private Rigidbody heldRigidbody;
    private GameObject lastThrownBoulder;
    private bool isCooldownActive = false;
    private Vector3 lastObjectPosition;

    void Update()
    {
        if (!photonView.IsMine) return;

        // --- MOUSE BUTTON 0 STATE MACHINE ---
        if (Input.GetMouseButtonDown(0)) // Press and Hold started
        {
            if (heldRigidbody == null)
            {
                TryGrabBoulder();
            }
        }

        if (Input.GetMouseButtonUp(0)) // Released the click
        {
            if (heldRigidbody != null)
            {
                ReleaseAndThrowBoulder();
            }
        }
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        if (heldRigidbody != null)
        {
            MaintainHoldPhysicsPosition();
        }
    }

    private void TryGrabBoulder()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxGrabDistance, boulderLayer))
        {
            Rigidbody targetRb = hit.collider.GetComponent<Rigidbody>();
            
            if (targetRb != null)
            {
                if (isCooldownActive && hit.collider.gameObject == lastThrownBoulder) return;
                if (targetRb.linearVelocity.magnitude > 0.5f) return;

                PhotonView targetPV = targetRb.GetComponent<PhotonView>();
                if (targetPV != null)
                {
                    targetPV.RequestOwnership();
                }

                heldRigidbody = targetRb;
                heldRigidbody.isKinematic = false;
                heldRigidbody.useGravity = false; 
                
                lastObjectPosition = heldRigidbody.transform.position;
            }
        }
    }

    private void MaintainHoldPhysicsPosition()
    {
        Vector3 targetPosition = grabPoint.position;
        heldRigidbody.linearVelocity = (targetPosition - heldRigidbody.transform.position) * grabLerpSpeed;
        heldRigidbody.angularVelocity = Vector3.zero;
    }

    private void ReleaseAndThrowBoulder()
    {
        lastThrownBoulder = heldRigidbody.gameObject;
        heldRigidbody.useGravity = true;
        heldRigidbody.linearVelocity = heldRigidbody.linearVelocity * throwVelocityMultiplier;
        heldRigidbody = null;

        StartCoroutine(ExecuteBoulderCooldownRoutine());
    }

    private IEnumerator ExecuteBoulderCooldownRoutine()
    {
        isCooldownActive = true;
        yield return new WaitForSeconds(1f);
        isCooldownActive = false;
        lastThrownBoulder = null;
    }
}