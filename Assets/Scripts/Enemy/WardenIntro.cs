using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// TempleScene: what happens once the player accepts the Warden's challenge
// (WardenChallenge.OnChallengeAccepted).
//   1. The screen fades completely to black (player frozen).
//   2. The Warden's title (WardenUI) fades in over the black, and its music
//      (Warden1) starts.
//   3. After titleHoldTime, the title fades out and the scene fades back in,
//      just as it was - but now with the Warden standing there in Idle.
//   4. Player control comes back.
//
// The black screen + title are built at runtime on their own overlay canvas
// (drawn above everything, SceneFader included), so the scene needs no UI
// wiring. The Warden is a scene object left inactive until step 3.
public class WardenIntro : MonoBehaviour
{
    [SerializeField] private WardenChallenge challenge; // found in the scene if empty
    [SerializeField] private GameObject warden;         // inactive in the scene until the intro
    // The Warden's own bar (not Morthos'): shown and bound to its
    // EnemyHealth when it appears.
    [SerializeField] private BossHealthUI healthBar;

    [Header("Title")]
    [SerializeField] private Sprite titleSprite;
    // Fraction of the screen width the title takes up.
    [SerializeField, Range(0.2f, 1f)] private float titleWidth = 0.75f;

    [Header("Timing")]
    [SerializeField] private float fadeToBlackTime = 1.2f;
    [SerializeField] private float titleFadeTime = 0.8f;
    [SerializeField] private float titleHoldTime = 3f;
    [SerializeField] private float fadeBackTime = 1f;

    [Header("Music")]
    [SerializeField] private AudioClip music;
    [SerializeField] private float musicVolume = 0.7f;

    public bool IsPlaying { get; private set; }

    private AudioSource musicSource;
    private Image blackImage;
    private Image titleImage;

    private void Start()
    {
        if (challenge == null) challenge = FindFirstObjectByType<WardenChallenge>(FindObjectsInactive.Include);
        if (challenge != null) challenge.OnChallengeAccepted.AddListener(Play);

        if (warden != null) warden.SetActive(false);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;
    }

    private void OnDestroy()
    {
        if (challenge != null) challenge.OnChallengeAccepted.RemoveListener(Play);
    }

    // Fades the current track out and starts another one (looping) - the
    // phase 2 music (WardenAI).
    // loop = false for a one-off track (the victory music when it dies).
    public void ChangeMusic(AudioClip clip, float fadeTime = 0.8f, bool loop = true)
    {
        if (clip == null || musicSource == null) return;
        if (musicSwap != null) StopCoroutine(musicSwap);
        musicSwap = StartCoroutine(SwapMusic(clip, fadeTime, loop));
    }

    private Coroutine musicSwap;

    private IEnumerator SwapMusic(AudioClip clip, float fadeTime, bool loop)
    {
        float startVolume = musicSource.volume;
        if (musicSource.isPlaying)
        {
            for (float t = 0f; t < fadeTime; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeTime);
                yield return null;
            }
        }

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void Play()
    {
        if (IsPlaying) return;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        IsPlaying = true;
        BuildOverlay();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerMovement movement = null;
        Rigidbody2D playerRb = null;
        if (player != null)
        {
            movement = player.GetComponent<PlayerMovement>();
            playerRb = player.GetComponent<Rigidbody2D>();
        }
        if (movement != null) movement.enabled = false;
        if (playerRb != null) playerRb.linearVelocity = new Vector2(0f, playerRb.linearVelocity.y);

        // Morthos' victory track may still be ringing out.
        BeastPhaseMusic beastMusic = FindFirstObjectByType<BeastPhaseMusic>();
        if (beastMusic != null) beastMusic.Stop();

        // 1. fade to black
        yield return FadeImage(blackImage, 0f, 1f, fadeToBlackTime);

        // At full black: the challenge spot is gone and the Warden takes its place.
        if (challenge != null)
        {
            challenge.OnChallengeAccepted.RemoveListener(Play);
            Destroy(challenge.gameObject);
            challenge = null;
        }

        if (warden != null)
        {
            warden.SetActive(true);

            if (healthBar != null)
            {
                // Show first (OnEnable subscribes to nothing yet), then Bind
                // subscribes once and fills the bar with the current health.
                healthBar.Show();
                EnemyHealth wardenHealth = warden.GetComponent<EnemyHealth>();
                if (wardenHealth != null) healthBar.Bind(wardenHealth);
            }
        }

        // 2. title + music
        if (music != null)
        {
            musicSource.clip = music;
            musicSource.Play();
        }
        yield return FadeImage(titleImage, 0f, 1f, titleFadeTime);
        yield return new WaitForSeconds(titleHoldTime);
        yield return FadeImage(titleImage, 1f, 0f, titleFadeTime);

        // 3. back to the scene
        yield return FadeImage(blackImage, 1f, 0f, fadeBackTime);

        if (movement != null) movement.enabled = true;

        // The fight starts.
        if (warden != null && warden.TryGetComponent(out WardenAI wardenAI)) wardenAI.Activate();

        Destroy(blackImage.canvas.gameObject);
        IsPlaying = false;
    }

    private void BuildOverlay()
    {
        GameObject canvasGo = new GameObject("WardenIntroOverlay", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // above every other UI, the SceneFader included

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        blackImage = CreateImage("Black", canvasGo.transform);
        blackImage.color = new Color(0f, 0f, 0f, 0f);
        RectTransform blackRect = blackImage.rectTransform;
        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        blackRect.offsetMin = Vector2.zero;
        blackRect.offsetMax = Vector2.zero;
        blackImage.raycastTarget = true;

        titleImage = CreateImage("Title", canvasGo.transform);
        titleImage.sprite = titleSprite;
        titleImage.preserveAspect = true;
        titleImage.color = new Color(1f, 1f, 1f, 0f);
        RectTransform titleRect = titleImage.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f - titleWidth * 0.5f, 0.2f);
        titleRect.anchorMax = new Vector2(0.5f + titleWidth * 0.5f, 0.8f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
    }

    private static Image CreateImage(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static IEnumerator FadeImage(Image image, float from, float to, float duration)
    {
        Color c = image.color;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(from, to, t / duration);
            image.color = c;
            yield return null;
        }
        c.a = to;
        image.color = c;
    }
}
