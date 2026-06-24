using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Boulder Spawn Points")]
    [SerializeField] private Transform[] boulderSpawnPoints;

    public static Transform[] SpawnPoints;
    public static Transform[] BoulderSpawnPoints;

    private void Start()
    {
        SpawnPoints = spawnPoints;
        BoulderSpawnPoints = boulderSpawnPoints;
        SpawnPlayer();
        if (PhotonNetwork.IsMasterClient)
            SpawnBoulders();
    }

    private void SpawnPlayer()
    {
        int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) 
                         % spawnPoints.Length;

        Transform spawnPoint = spawnPoints[spawnIndex];

        PhotonNetwork.Instantiate("PlayerPrefab",
            spawnPoint.position,
            spawnPoint.rotation);
    }

    private void SpawnBoulders()
    {
        foreach (Transform boulderSpawn in boulderSpawnPoints)
        {
            PhotonNetwork.Instantiate("Boulder",
                boulderSpawn.position,
                boulderSpawn.rotation);
        }
    }
}