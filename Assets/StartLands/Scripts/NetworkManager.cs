using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections; // Needed for the IEnumerator coroutine execution step

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private GameObject createRoomBtn;
    [SerializeField] private GameObject joinRoomBtn;

    [Header("Loading Overlay Assignments")]
    [SerializeField] private LoadingScreenManager loadingScreenManager;

    private void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
        createRoomBtn.SetActive(false);
        joinRoomBtn.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        createRoomBtn.SetActive(true);
        joinRoomBtn.SetActive(true);
    }

    public void OnClickCreateRoom()
    {
        if (string.IsNullOrEmpty(playerNameInput.text) || 
            string.IsNullOrEmpty(roomNameInput.text)) return;

        if (loadingScreenManager != null) 
        {
            loadingScreenManager.ShowLoadingScreen();
        }

        PhotonNetwork.NickName = playerNameInput.text;

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 5;

        PhotonNetwork.CreateRoom(roomNameInput.text, options);
    }

    public void OnClickJoinRoom()
    {
        if (string.IsNullOrEmpty(playerNameInput.text) || 
            string.IsNullOrEmpty(roomNameInput.text)) return;

        if (loadingScreenManager != null) 
        {
            loadingScreenManager.ShowLoadingScreen();
        }

        PhotonNetwork.NickName = playerNameInput.text;
        PhotonNetwork.JoinRoom(roomNameInput.text);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
        PhotonNetwork.IsMessageQueueRunning = false;
        StartCoroutine(DelayedSceneLoad());
    }

    private IEnumerator DelayedSceneLoad()
    {
        yield return new WaitForSeconds(2f);
        PhotonNetwork.LoadLevel("Drought");
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Join Room Failed: " + message);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Create Room Failed: " + message);
    }
}