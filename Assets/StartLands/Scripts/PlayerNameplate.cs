using UnityEngine;
using TMPro;
using Photon.Pun;

public class PlayerNameplate : MonoBehaviour
{
    [Header("Assignments")]
    [SerializeField] private TMP_Text nameText;
    
    private PhotonView targetPhotonView;
    private Transform mainCameraTransform;

    void Start()
    {
        targetPhotonView = GetComponentInParent<PhotonView>();

        if (targetPhotonView == null)
        {
            Debug.LogError("PlayerNameplate: Could not find a PhotonView component on the player root!", this);
            return;
        }

        // CULLING GATE: Hide your own nameplate on your screen
        if (targetPhotonView.IsMine)
        {
            gameObject.SetActive(false);
            return;
        }

        // INITIALIZATION: Push the nickname to the TMPro component
        if (nameText != null)
        {
            string playerNickName = targetPhotonView.Owner != null ? targetPhotonView.Owner.NickName : "";
            nameText.text = string.IsNullOrEmpty(playerNickName) ? $"Player {targetPhotonView.ViewID}" : playerNickName;
            
            // Apply the tank-proof, state-isolated deterministic network color
            nameText.color = GenerateColorFromPlayer();
        }

        // FIX: Specifically look for the active local camera via the global engine tag registry
        GameObject mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCamObj != null)
        {
            mainCameraTransform = mainCamObj.transform;
        }
    }

    void LateUpdate()
    {
        // BILLBOARD CALCULATOR: Keep the text facing the validated local camera transform
        if (mainCameraTransform != null && mainCameraTransform.gameObject.activeInHierarchy)
        {
            transform.LookAt(transform.position + mainCameraTransform.forward);
        }
        else
        {
            // Emergency runtime fallback check if cameras toggle during state swaps
            GameObject mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCamObj != null)
            {
                mainCameraTransform = mainCamObj.transform;
            }
        }
    }

    /// <summary>
    /// Generates a distinct, high-visibility color seeded by the player's network ID
    /// Isolated from Unity's global random state generator pipeline
    /// </summary>
    private Color GenerateColorFromPlayer()
    {
        int seed = targetPhotonView.Owner != null ? targetPhotonView.Owner.ActorNumber : targetPhotonView.ViewID;
        
        // Using System.Random to completely isolate state calculations
        System.Random rng = new System.Random(seed);
        float randomHue = (float)rng.NextDouble();
        
        return Color.HSVToRGB(randomHue, 0.85f, 0.95f);
    }
}