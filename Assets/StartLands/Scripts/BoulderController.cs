using UnityEngine;
using Photon.Pun;

public class BoulderController : MonoBehaviourPun
{
    [Header("Kill Settings")]
    [SerializeField] private float killVelocityThreshold = 5f;

    private void OnCollisionEnter(Collision collision)
    {
        // Safety check: Only the network owner of this specific boulder calculates collisions
        if (!photonView.IsMine) return;

        float currentVelocity = GetComponent<Rigidbody>().linearVelocity.magnitude;

        // 1. ENVIRONMENT IMPACT (Local Thud)
        // If the boulder hits anything that isn't a Player, play the thud sound locally
        if (!collision.gameObject.CompareTag("Player"))
        {
            if (AudioManager.Instance != null && AudioManager.Instance.thudClip != null)
            {
                // Play positional 3D sound at the boulder's impact location
                AudioSource.PlayClipAtPoint(AudioManager.Instance.thudClip, transform.position, 1.0f);
            }
        }

        // Speed check: If the boulder is moving too slowly, it won't trigger kills or screams
        if (currentVelocity < killVelocityThreshold) return;

        // 2. PLAYER IMPACT (Networked Scream)
        if (collision.gameObject.CompareTag("Player"))
        {
            PhotonView targetPV = collision.gameObject.GetComponent<PhotonView>();
            if (targetPV == null) return;

            PlayerStateManager stateManager = collision.gameObject.GetComponent<PlayerStateManager>();
            if (stateManager != null && stateManager.IsGhost) return;

            // NEW: Fire the network RPC through AudioManager so all connected clients hear the scream at this position
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.photonView.RPC("PlayScreamNetworkRPC", RpcTarget.All, transform.position);
            }

            // Calculate death force from boulder velocity
            Vector3 deathForce = GetComponent<Rigidbody>().linearVelocity.normalized * 5f + Vector3.up * 3f;

            targetPV.RPC("OnHitByBoulder", RpcTarget.All, 
                photonView.OwnerActorNr,
                deathForce.x, 
                deathForce.y, 
                deathForce.z);
        }
    }
}