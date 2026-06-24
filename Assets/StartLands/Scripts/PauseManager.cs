using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

// FIXED: Inheriting from MonoBehaviourPunCallbacks so network event triggers actually fire
public class PauseManager : MonoBehaviourPunCallbacks
{
    [Header("UI Panel Assignment")]
    [SerializeField] private GameObject pausePanel;

    [Header("Interactive Button Elements")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    [Header("Loading Overlay Assignment")] 
    [SerializeField] private GameObject quitFadePanel; // FIXED: Replaced manager class with a direct GameObject reference

    private bool isPaused = false;

    void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Ensure the panel starts disabled at runtime layout assembly
        if (quitFadePanel != null)
        {
            quitFadePanel.SetActive(false);
        }

        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (quitButton != null) quitButton.onClick.AddListener(QuitToLobby);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pausePanel != null) pausePanel.SetActive(true);

        // FIXED: Explicitly shutting down local input streams on execution
        PlayerController localPlayer = FindLocalPlayer();
        if (localPlayer != null)
        {
            localPlayer.canMove = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);

        PlayerController localPlayer = FindLocalPlayer();
        if (localPlayer != null && GameManager.Instance.GameStarted)
            localPlayer.canMove = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitToLobby()
    {
        // Safety step: Always clear mouse states back out
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // FIXED: Fast panel activation layout flip
        if (quitFadePanel != null) 
        {
            quitFadePanel.SetActive(true);
        }

        // Drop out of the network context cleanly
        PhotonNetwork.LeaveRoom();
    }

    // FIXED: This callback will now execute flawlessly because of the class type header update
    public override void OnLeftRoom()
    {
        Debug.Log("Network cleanup confirmed. Re-entering Lobby scene context.");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }

    private PlayerController FindLocalPlayer()
    {
        // OPTIMIZED FOR UNITY 6: Matching our optimized non-alloc structure patterns
        PlayerController[] controllers = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PlayerController pc in controllers)
        {
            if (pc.GetComponent<PhotonView>().IsMine)
                return pc;
        }
        return null;
    }
}