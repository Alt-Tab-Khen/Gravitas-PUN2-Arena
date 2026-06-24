using UnityEngine;
using Photon.Pun;

public class AudioManager : MonoBehaviourPunCallbacks
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource rainSource; // NEW: Dedicated rain source

    [Header("Audio Clips")]
    public AudioClip thudClip;        // Boulder hits ground
    public AudioClip screamClip;      // FAAAHHHHH sound
    public AudioClip rainClip;        // NEW: Rain ambience loop

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Background Music Setup
        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.loop = true;
            musicSource.volume = 0.3f;
            if (!musicSource.isPlaying) musicSource.Play();
        }

        // NEW: Rain Ambience Setup
        if (rainSource != null && rainClip != null)
        {
            rainSource.clip = rainClip;
            rainSource.loop = true;
            rainSource.volume = 0.5f; // Adjust this default volume to whatever feels right
            if (!rainSource.isPlaying) rainSource.Play();
        }
    }

    /// <summary>
    /// Network-synced RPC to play the player impact scream on all clients simultaneously.
    /// </summary>
    [PunRPC]
    public void PlayScreamNetworkRPC(Vector3 position)
    {
        if (screamClip != null)
        {
            AudioSource.PlayClipAtPoint(screamClip, position, 1.0f);
        }
    }
}