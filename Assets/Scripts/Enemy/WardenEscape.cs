using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The ending. Lives on the Warden's fallen helmet (WardenFallenHelmet's
// prefab). Once the Warden is dead, WardenAI calls EnableEscape(): standing
// by the helmet shows the "Escape" prompt in the scene's DialogueUI, the same
// way the "Desafiar" spot works at the start.
//
// Pressing Interact:
//   1. The player freezes and the camera starts shaking, harder and faster
//      every moment (Cinemachine impulses, ever closer together).
//   2. At its peak the screen fades completely to black, the ending music
//      plays, and "The End" fades in.
//   3. After a few seconds "The End" and the music fade out and the game
//      goes back to the main menu (clearing the persistent player first).
//
// The trigger zone is a BoxCollider2D on the helmet itself, off until the
// escape is available (a trigger: nothing the player can bump into).
[RequireComponent(typeof(BoxCollider2D))]
public class WardenEscape : MonoBehaviour
{
    [Header("Prompt")]
    [SerializeField] private string promptMessage = "Escape";
    // World size of the zone around the helmet where the prompt shows.
    [SerializeField] private Vector2 zoneSize = new Vector2(5f, 5f);

    [Header("Quake")]
    [SerializeField] private float quakeDuration = 4f;
    // Gap between two shakes: from slow to frantic.
    [SerializeField] private float startInterval = 0.4f;
    [SerializeField] private float endInterval = 0.04f;
    [SerializeField] private float startForce = 0.3f;
    [SerializeField] private float endForce = 2f;

    [Header("Ending")]
    // The fade to black starts this long before the quake ends.
    [SerializeField] private float fadeToBlackTime = 1.5f;
    [SerializeField] private Sprite theEndSprite;
    [SerializeField, Range(0.2f, 1f)] private float theEndWidth = 0.6f;
    [SerializeField] private float theEndDelay = 0.8f;
    [SerializeField] private float theEndFadeTime = 2.5f;
    [SerializeField] private AudioClip endingMusic;

    [Header("Back to the main menu")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    // How long "The End" stays up before going back.
    [SerializeField] private float theEndHoldTime = 6f;
    // "The End" and the music fade out over this, then the menu loads.
    [SerializeField] private float returnFadeTime = 2f;

    private BoxCollider2D zone;
    private DialogueUI dialogueUI;
    private InputSystem_Actions inputActions;
    private bool available;
    private bool playerInRange;
    private bool isPromptShown;
    private bool escaping;

    private void Awake()
    {
        zone = GetComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.enabled = false;
    }

    // Called by WardenAI once the Warden is dead.
    public void EnableEscape(float delay)
    {
        StartCoroutine(EnableAfter(delay));
    }

    private IEnumerator EnableAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        dialogueUI = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);

        // The helmet is scaled and lying rotated: size the box so the zone is
        // zoneSize in world units whatever the helmet's own transform.
        Vector3 s = transform.lossyScale;
        float sx = Mathf.Max(0.01f, Mathf.Abs(s.x)), sy = Mathf.Max(0.01f, Mathf.Abs(s.y));
        zone.offset = Vector2.zero;
        zone.size = new Vector2(zoneSize.x / sx, zoneSize.y / sy);
        zone.enabled = true;
        available = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (other.TryGetComponent(out PlayerMovement playerMovement)) inputActions = playerMovement.InputActions;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
    }

    private void Update()
    {
        if (escaping) return;

        bool canEscape = available && playerInRange;
        SetPrompt(canEscape);

        if (canEscape && inputActions != null && inputActions.Player.Interact.WasPerformedThisFrame())
        {
            StartCoroutine(EscapeSequence());
        }
    }

    private IEnumerator EscapeSequence()
    {
        escaping = true;
        SetPrompt(false);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (player.TryGetComponent(out PlayerMovement movement)) movement.enabled = false;
            if (player.TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        CinemachineImpulseSource shake = CreateShakeSource();
        Image black = BuildOverlay(out Image theEnd);
        bool fading = false;

        // 1. the quake, faster and harder every moment
        float elapsed = 0f;
        float nextShake = 0f;
        while (elapsed < quakeDuration)
        {
            float t = elapsed / quakeDuration;
            if (elapsed >= nextShake)
            {
                float force = Mathf.Lerp(startForce, endForce, t * t);
                shake.GenerateImpulseWithVelocity(Random.insideUnitCircle.normalized * force);
                nextShake = elapsed + Mathf.Lerp(startInterval, endInterval, t);
            }

            // 2. the screen goes black at the peak
            float fadeStart = quakeDuration - fadeToBlackTime;
            if (elapsed >= fadeStart)
            {
                if (!fading)
                {
                    fading = true;
                    WardenIntro intro = FindAnyObjectByType<WardenIntro>();
                    if (intro != null) intro.ChangeMusic(endingMusic, fadeToBlackTime, false);
                }
                SetAlpha(black, Mathf.Clamp01((elapsed - fadeStart) / fadeToBlackTime));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        SetAlpha(black, 1f);

        // 3. The End
        yield return new WaitForSeconds(theEndDelay);
        for (float t = 0f; t < theEndFadeTime; t += Time.deltaTime)
        {
            SetAlpha(theEnd, t / theEndFadeTime);
            yield return null;
        }
        SetAlpha(theEnd, 1f);

        // 4. Back to the main menu
        yield return new WaitForSeconds(theEndHoldTime);

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        float[] startVolumes = new float[sources.Length];
        for (int i = 0; i < sources.Length; i++) startVolumes[i] = sources[i] != null ? sources[i].volume : 0f;

        for (float t = 0f; t < returnFadeTime; t += Time.deltaTime)
        {
            float k = 1f - t / returnFadeTime;
            SetAlpha(theEnd, k);
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null) sources[i].volume = startVolumes[i] * k;
            }
            yield return null;
        }

        ClearPersistentObjects();
        SceneManager.LoadScene(mainMenuScene);
    }

    // The player (and its soul, if one is lying around) survive scene loads
    // (SceneFader / PlayerSoul use DontDestroyOnLoad). Going back to the main
    // menu they must go, so a new game starts from scratch - no leftover
    // player, no jump/dash upgrades carried over.
    private static void ClearPersistentObjects()
    {
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            Destroy(player);
        }

        foreach (PlayerSoul soul in FindObjectsByType<PlayerSoul>(FindObjectsSortMode.None))
        {
            Destroy(soul.gameObject);
        }

        // Collected souls (and their total) belong to this run only.
        SoulScore.ResetAll();
    }

    private CinemachineImpulseSource CreateShakeSource()
    {
        CinemachineImpulseSource source = gameObject.AddComponent<CinemachineImpulseSource>();
        CinemachineImpulseDefinition def = source.ImpulseDefinition;
        def.ImpulseChannel = 1;
        def.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        def.ImpulseDuration = 0.18f;
        def.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform; // same strength wherever the camera is
        return source;
    }

    // Black screen + "The End", on their own overlay canvas above everything
    // (same idea as WardenIntro's title screen).
    private Image BuildOverlay(out Image theEnd)
    {
        GameObject canvasGo = new GameObject("TheEndOverlay", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1001;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image black = CreateImage("Black", canvasGo.transform, Vector2.zero, Vector2.one);
        black.color = new Color(0f, 0f, 0f, 0f);
        black.raycastTarget = true;

        theEnd = CreateImage("TheEnd", canvasGo.transform,
            new Vector2(0.5f - theEndWidth * 0.5f, 0.3f), new Vector2(0.5f + theEndWidth * 0.5f, 0.7f));
        theEnd.sprite = theEndSprite;
        theEnd.preserveAspect = true;
        theEnd.color = new Color(1f, 1f, 1f, 0f);
        return black;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    private void SetPrompt(bool show)
    {
        if (show == isPromptShown || dialogueUI == null) return;

        isPromptShown = show;
        if (show) dialogueUI.Show(promptMessage);
        else dialogueUI.Hide();
    }
}
