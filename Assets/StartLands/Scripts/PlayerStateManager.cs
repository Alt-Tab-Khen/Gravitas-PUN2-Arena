using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PlayerStateManager : MonoBehaviourPun
{
    [Header("References")]
    [SerializeField] private Renderer playerRenderer;
    [SerializeField] private CharacterController characterController;

    [Header("Ghost Settings")]
    [SerializeField] private string ghostLayerName = "Ghost";
    [SerializeField] private string playerLayerName = "Player";

    public bool IsGhost { get; private set; } = false;

    [PunRPC]
    public void OnHitByBoulder(int killerActorNumber, float forceX, float forceY, float forceZ)
    {
        if (IsGhost) return;

        if (PhotonNetwork.LocalPlayer.ActorNumber == killerActorNumber)
        {
            GameManager.Instance.RegisterKill(killerActorNumber);
        }

        SetGhostState(forceX, forceY, forceZ);
    }

    [PunRPC]
    public void SetGhostState(float forceX, float forceY, float forceZ)
    {
        Vector3 deathForce = new Vector3(forceX, forceY, forceZ);
        IsGhost = true;
        characterController.enabled = false;

        Rigidbody pelvisRb = GetComponentInChildren<Rigidbody>();
        if (pelvisRb != null)
        {
            Transform pelvis = pelvisRb.transform;

            Transform cameraHolder = transform.Find("Camera Holder");
            Transform namePlate = transform.Find("NamePlate");

            if (cameraHolder != null) cameraHolder.SetParent(pelvis);
            if (namePlate != null) namePlate.SetParent(pelvis);

            pelvisRb.isKinematic = false;
            pelvisRb.useGravity = true;
            pelvisRb.AddForce(deathForce * 10f, ForceMode.Impulse);

            GetComponentInChildren<Animator>().enabled = false;
        }

        StartCoroutine(GhostDelayRoutine());
    }

    private IEnumerator GhostDelayRoutine()
    {
        yield return new WaitForSeconds(3f);

        Rigidbody pelvisRb = GetComponentInChildren<Rigidbody>();
        Transform pelvis = pelvisRb?.transform;

        Transform cameraHolder = pelvis?.Find("Camera Holder");
        Transform namePlate = pelvis?.Find("NamePlate");

        if (cameraHolder != null)
        {
            cameraHolder.SetParent(transform);
            cameraHolder.localPosition = new Vector3(0, 1.6f, 0);
            cameraHolder.localRotation = Quaternion.identity;
        }

        if (namePlate != null)
        {
            namePlate.SetParent(transform);
            namePlate.localPosition = new Vector3(0, 2.2f, 0);
        }

        if (pelvisRb != null)
            pelvisRb.isKinematic = true;

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
            anim.SetFloat("Speed", 0f);
            anim.SetBool("IsJumping", false);
        }

        playerRenderer.material.color = Color.white;
        SetLayerRecursively(gameObject, LayerMask.NameToLayer(ghostLayerName));
        characterController.excludeLayers = LayerMask.GetMask(playerLayerName, "Boulder");
        characterController.enabled = true;

        GameManager.Instance.CheckWinCondition();

        if (photonView.IsMine)
            GameManager.Instance.ShowNotification("You died! You are now a ghost.");
        else
            GameManager.Instance.ShowNotification($"{photonView.Owner.NickName} has been eliminated!");
    }

    public void ResetPlayerState()
    {
        IsGhost = false;
        playerRenderer.material.color = Color.gray;
        SetLayerRecursively(gameObject, LayerMask.NameToLayer(playerLayerName));
        characterController.excludeLayers = 0;
        characterController.enabled = true;

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
            anim.SetFloat("Speed", 0f);
            anim.SetBool("IsJumping", false);
        }

        Rigidbody pelvisRb = GetComponentInChildren<Rigidbody>();
        if (pelvisRb != null)
        {
            pelvisRb.isKinematic = true;

            Transform cameraHolder = pelvisRb.transform.Find("Camera Holder") ?? transform.Find("Camera Holder");
            Transform namePlate = pelvisRb.transform.Find("NamePlate") ?? transform.Find("NamePlate");

            if (cameraHolder != null && cameraHolder.parent != transform)
            {
                cameraHolder.SetParent(transform);
                cameraHolder.localPosition = new Vector3(0, 1.6f, 0);
                cameraHolder.localRotation = Quaternion.identity;
            }

            if (namePlate != null && namePlate.parent != transform)
            {
                namePlate.SetParent(transform);
                namePlate.localPosition = new Vector3(0, 2.2f, 0);
            }
        }

        GetComponent<PlayerController>()?.ResetForNewRound();
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}