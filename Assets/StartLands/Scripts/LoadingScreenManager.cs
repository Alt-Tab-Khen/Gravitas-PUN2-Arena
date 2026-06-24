using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("UI Element Assignments")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private TMP_Text loadingText;

    [Header("Pulse Settings")]
    [SerializeField] private float pulseSpeed = 2f;

    private bool isFirstLoad = true;
    private int lastTipIndex = -1; // Track the last tip so we don't accidentally roll the same one twice in a row

    // Hardcoded master array of loading screen wisdom
    private readonly string[] tips = new string[]
    {
        "Tip: Can't reach someone? Swing your camera hard before releasing. The boulder goes where your momentum goes.",
        "Tip: A fast moving boulder can't be grabbed. Use this to your advantage.",
        "Tip: Ghosts can't be killed but they can still walk around. Touch grass. Literally.",
        "Tip: Sprint before grabbing a boulder. Your momentum carries into the throw.",
        "Tip: Boulders don't care about your feelings. Neither does gravity.",
        "Tip: The arena is big for a reason. Use it. Don't just stand there.",
        "Tip: Can't slap someone with your hand? Slap them with 300kg of rock. Always works.",
        "Tip: Aim isn't everything. Panic throws have claimed more lives than calculated ones.",
        "Tip: Two boulders, five players. Do the math.",
        "Tip: WAAASHING. Check your boulders. — Vergil, probably",
        "Tip: It just works. — Todd Howard, on the physics engine",
        "Tip: Do you get to the Cloud District very often? Oh, what am I saying — of course you don't.",
        "Tip: A boulder a day keeps the Dreads away. Medical professionals hate this trick.",
        "Tip: The Dread who grabs the boulder first has the upper hand. The other four have a problem.",
        "Tip: Dying is just becoming a very slow spectator. Embrace it.",
        "Tip: If you're reading this, the other players are already fighting over the boulder.",
        "Tip: Rock beats everything. There is no scissors.",
        "Tip: Ghosts can't throw boulders. This is not a bug. This is karma.",
        "Tip: Your boulder. Their face. Simple math."
    };

    void Update()
    {
        // NEW: If the loading screen is visible and the player clicks/taps, skip to the next tip
        if (loadingScreen.activeInHierarchy && Input.GetMouseButtonDown(0))
        {
            CycleTip();
        }
    }

    /// <summary>
    /// Triggers the full screen loading graphic pipeline overlay and populates textual buffers
    /// </summary>
    public void ShowLoadingScreen()
    {
        loadingScreen.SetActive(true);

       
        if (isFirstLoad)
        {
            tipText.text = "Tip: Hire me. — The Developer";
            isFirstLoad = false;
        }
        else
        {
            CycleTip();
        }

        StopAllCoroutines();
        StartCoroutine(PulseLoadingText());
    }

    /// <summary>
    /// NEW: Selects a completely random tip from the pool, ensuring it doesn't repeat the previous one
    /// </summary>
    private void CycleTip()
    {
        if (tips.Length <= 1) return;

        int randomIdx = lastTipIndex;
        
        // Loop until we get a completely different index than the one currently showing
        while (randomIdx == lastTipIndex)
        {
            randomIdx = Random.Range(0, tips.Length);
        }

        lastTipIndex = randomIdx;
        tipText.text = tips[randomIdx];
    }

    /// <summary>
    /// Smoothly interpolates TMPro vertex color alpha properties back and forth using a cosine loop
    /// </summary>
    private IEnumerator PulseLoadingText()
    {
        Color baseColor = loadingText.color;
        float elapsedTime = 0f;

        while (loadingScreen.activeInHierarchy)
        {
            elapsedTime += Time.deltaTime * pulseSpeed;
            
            // Map raw math over a clean 0 to 1 absolute bounds wave
            float calculatedAlpha = (Mathf.Cos(elapsedTime) + 1f) / 2f;
            
            loadingText.color = new Color(baseColor.r, baseColor.g, baseColor.b, calculatedAlpha);
            yield return null;
        }
    }
}