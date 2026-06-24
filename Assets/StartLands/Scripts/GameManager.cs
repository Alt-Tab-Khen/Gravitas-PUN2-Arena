using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("HUD References")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private GameObject[] scoreRows;

    [Header("Game Settings")]
    [SerializeField] private int countdownDuration = 15;
    [SerializeField] private int minPlayersToStart = 2;

    [Header("Round Settings")]
    [SerializeField] private int totalRounds = 3;
    [SerializeField] private LoadingScreenManager loadingScreenManager;

    // Internal State
    private int currentRound = 1;
    private bool gameStarted = false;
    private bool countdownRunning = false;
    private int currentCountdownValue;
    private Dictionary<int, int> playerKills = new Dictionary<int, int>();
    private List<PlayerController> allPlayers = new List<PlayerController>();
    public bool GameStarted => gameStarted;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        notificationText.text = "";
        UpdatePlayerCountUI();
        TryStartCountdown();
        PopulateScoreboard();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerCountUI();
        TryStartCountdown();
        ShowNotification($"{newPlayer.NickName} joined!");
        PopulateScoreboard();

        if (PhotonNetwork.IsMasterClient && countdownRunning)
        {
            photonView.RPC("SyncCountdownState", newPlayer, currentCountdownValue);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerCountUI();
        ShowNotification($"{otherPlayer.NickName} left!");
    }

    private void TryStartCountdown()
    {
        if (gameStarted || countdownRunning) return;
        if (PhotonNetwork.CurrentRoom.PlayerCount >= minPlayersToStart)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("StartCountdownRPC", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    private void StartCountdownRPC()
    {
        if (countdownRunning) return;
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        yield return StartCoroutine(CountdownRoutine(countdownDuration));
    }

    private IEnumerator CountdownRoutine(int startFrom)
    {
        countdownRunning = true;
        int timer = startFrom;

        if (!countdownText.gameObject.activeSelf)
            countdownText.gameObject.SetActive(true);

        while (timer > 0)
        {
            currentCountdownValue = timer;
            countdownText.text = timer.ToString();
            yield return new WaitForSeconds(1f);
            timer--;
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(1f);
        countdownText.gameObject.SetActive(false);

        gameStarted = true;
        countdownRunning = false;

        PlayerController localPlayer = FindLocalPlayer();
        if (localPlayer != null)
            localPlayer.canMove = true;
    }

    [PunRPC]
    private void SyncCountdownState(int currentTimer)
    {
        // Force stop and resync to master's current timer
        StopAllCoroutines();
        countdownRunning = false;
        notificationText.text = "";
        countdownText.gameObject.SetActive(true);
        countdownText.text = currentTimer.ToString();
        StartCoroutine(CountdownRoutine(Mathf.Max(1, currentTimer - 1)));
    }

    private PlayerController FindLocalPlayer()
    {
        PlayerController[] controllers = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PlayerController pc in controllers)
        {
            if (pc.GetComponent<PhotonView>().IsMine)
                return pc;
        }
        return null;
    }

    public void RegisterKill(int killerActorNumber)
    {
        photonView.RPC("SyncKillRPC", RpcTarget.All, killerActorNumber);
    }

    [PunRPC]
    private void SyncKillRPC(int killerActorNumber)
    {
        if (!playerKills.ContainsKey(killerActorNumber))
            playerKills[killerActorNumber] = 0;

        playerKills[killerActorNumber]++;
        PopulateScoreboard();
    }

    public void ShowNotification(string message)
    {
        StopCoroutine("NotificationRoutine");
        StartCoroutine(NotificationRoutine(message));
    }

    private IEnumerator NotificationRoutine(string message)
    {
        notificationText.text = message;
        yield return new WaitForSeconds(3f);
        notificationText.text = "";
    }

    private void UpdatePlayerCountUI()
    {
        playerCountText.text = $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/5";
    }

    private Player GetPlayerByActorNumber(int actorNumber)
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == actorNumber) return p;
        }
        return null;
    }

    private void PopulateScoreboard()
    {
        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < scoreRows.Length; i++)
        {
            TMP_Text nameText = scoreRows[i].transform.Find("PlayerName").GetComponent<TMP_Text>();
            TMP_Text killText = scoreRows[i].transform.Find("KillCount").GetComponent<TMP_Text>();

            if (i < players.Length)
            {
                nameText.text = players[i].NickName;
                killText.text = playerKills.ContainsKey(players[i].ActorNumber)
                    ? playerKills[players[i].ActorNumber].ToString()
                    : "0";
            }
            else
            {
                nameText.text = "-";
                killText.text = "-";
            }
        }
    }

    public void CheckWinCondition()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PlayerStateManager[] allStateManagers = FindObjectsByType<PlayerStateManager>(UnityEngine.FindObjectsSortMode.None);
        int alivePlayers = 0;
        PlayerStateManager lastAlive = null;

        foreach (PlayerStateManager p in allStateManagers)
        {
            if (!p.IsGhost)
            {
                alivePlayers++;
                lastAlive = p;
            }
        }

        if (alivePlayers <= 1)
        {
            string winnerName = lastAlive != null ? lastAlive.photonView.Owner.NickName : "Nobody";
            photonView.RPC("OnRoundEnd", RpcTarget.All, winnerName);
        }
    }

    [PunRPC]
    private void OnRoundEnd(string winnerName)
    {
        ShowNotification($"{winnerName} wins Round {currentRound}!");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (currentRound >= totalRounds)
        {
            StartCoroutine(EndGameAndReturnToLobby());
        }
        else
        {
            currentRound++;
            StartCoroutine(StartNextRound());
        }
    }

 private IEnumerator StartNextRound()
{
    yield return new WaitForSeconds(5f);

    gameStarted = false;
    countdownRunning = false;

    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;

    // Reset boulders to spawn positions
    if (PhotonNetwork.IsMasterClient)
    {
        GameObject[] boulders = GameObject.FindGameObjectsWithTag("Boulder");
        for (int i = 0; i < boulders.Length; i++)
        {
            PhotonView bv = boulders[i].GetComponent<PhotonView>();
            if (bv != null && bv.IsMine && i < PlayerSpawner.BoulderSpawnPoints.Length)
            {
                boulders[i].transform.position = PlayerSpawner.BoulderSpawnPoints[i].position;
                boulders[i].GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                boulders[i].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            }
        }
    }

    // Reset players to spawn points
    PlayerStateManager[] allStateManagers = FindObjectsByType<PlayerStateManager>(UnityEngine.FindObjectsSortMode.None);
    foreach (PlayerStateManager p in allStateManagers)
    {
        p.ResetPlayerState();

        if (p.GetComponent<PhotonView>().IsMine)
        {
            int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % PlayerSpawner.SpawnPoints.Length;
            p.transform.position = PlayerSpawner.SpawnPoints[spawnIndex].position;
            p.transform.rotation = PlayerSpawner.SpawnPoints[spawnIndex].rotation;
        }
    }

    TryStartCountdown();
}

    private IEnumerator EndGameAndReturnToLobby()
    {
        int highestKills = 0;
        int winnerActorNumber = -1;

        foreach (var kvp in playerKills)
        {
            if (kvp.Value > highestKills)
            {
                highestKills = kvp.Value;
                winnerActorNumber = kvp.Key;
            }
        }

        Player winner = GetPlayerByActorNumber(winnerActorNumber);
        string winnerName = winner != null ? winner.NickName : "Nobody";

        ShowNotification($"Game Over! {winnerName} wins with {highestKills} kills!");

        yield return new WaitForSeconds(8f);

        if (loadingScreenManager != null)
            loadingScreenManager.ShowLoadingScreen();

        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }
}