using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
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
//   3. "The End" rises and the run's statistics (RunStats) fade in below it
//      while the music keeps playing. Any button (or statsMaxTime) moves on.
//   4. Everything and the music fade out and the game goes back to the
//      main menu (clearing the persistent player first).
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

    [Header("Statistics")]
    // "The End" alone for this long, then it rises and the stats appear.
    [SerializeField] private float statsDelay = 1.5f;
    [SerializeField] private float statsFadeTime = 1.2f;
    // Any button continues once this much time has passed...
    [SerializeField] private float statsMinTime = 3f;
    // ...and it continues by itself after this long.
    [SerializeField] private float statsMaxTime = 40f;
    [SerializeField] private string statsTitle = "GAME STATS";
    [SerializeField] private string continueHint = "Press any button to continue";

    [Header("Back to the main menu")]
    [SerializeField] private string mainMenuScene = "MainMenu";
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
        AchievementTracker.GameFinished();

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

        // 4. The statistics of the run, under "The End"
        yield return new WaitForSeconds(statsDelay);
        RunStats.Save();
        CanvasGroup stats = BuildStats(theEnd.canvas.transform, out TextMeshProUGUI hint);
        RectTransform endRect = theEnd.rectTransform;
        Vector2 endMinFrom = endRect.anchorMin, endMaxFrom = endRect.anchorMax;
        Vector2 endMinTo = new Vector2(endMinFrom.x, 0.7f), endMaxTo = new Vector2(endMaxFrom.x, 0.95f);
        for (float t = 0f; t < statsFadeTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / statsFadeTime);
            endRect.anchorMin = Vector2.Lerp(endMinFrom, endMinTo, k);
            endRect.anchorMax = Vector2.Lerp(endMaxFrom, endMaxTo, k);
            stats.alpha = k;
            yield return null;
        }
        endRect.anchorMin = endMinTo;
        endRect.anchorMax = endMaxTo;
        stats.alpha = 1f;

        for (float t = 0f; t < statsMaxTime; t += Time.deltaTime)
        {
            bool canContinue = t >= statsMinTime;
            hint.alpha = canContinue ? 0.45f + 0.35f * Mathf.Sin(t * 3f) : 0f;
            if (canContinue && AnyButtonPressed()) break;
            yield return null;
        }

        // 5. Back to the main menu

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        float[] startVolumes = new float[sources.Length];
        for (int i = 0; i < sources.Length; i++) startVolumes[i] = sources[i] != null ? sources[i].volume : 0f;

        for (float t = 0f; t < returnFadeTime; t += Time.deltaTime)
        {
            float k = 1f - t / returnFadeTime;
            SetAlpha(theEnd, k);
            stats.alpha = k;
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
        GameProgress.ResetAll();
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

    // =========================
    // STATISTICS
    // =========================

    private CanvasGroup BuildStats(Transform canvas, out TextMeshProUGUI hint)
    {
        GameObject panelGo = new GameObject("Stats", typeof(RectTransform), typeof(CanvasGroup));
        panelGo.transform.SetParent(canvas, false);
        RectTransform panel = (RectTransform)panelGo.transform;
        panel.anchorMin = new Vector2(0.5f, 0.06f);
        panel.anchorMax = new Vector2(0.5f, 0.68f);
        panel.sizeDelta = new Vector2(980f, 0f);
        CanvasGroup group = panelGo.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        Color label = new Color(0.82f, 0.78f, 0.9f);
        Color value = Color.white;
        Color minor = new Color(0.6f, 0.56f, 0.68f);

        int total = Achievements.Total;
        int earned = RunStats.AchievementsEarned;
        int percent = total > 0 ? Mathf.RoundToInt(100f * earned / total) : 0;
        string killer = RunStats.TopKiller(out int kills);

        List<(string name, string number, bool small)> rows = new List<(string, string, bool)>
        {
            ("Play time", RunStats.FormatTime(RunStats.PlayTime), false),
            ("Achievements this run", $"{earned} / {total}  ({percent}%)", false),
            ("Deaths", RunStats.Deaths.ToString(), false),
        };
        foreach (string level in RunStats.Levels)
        {
            rows.Add(("      " + level, RunStats.DeathsIn(level).ToString(), true));
        }
        rows.Add(("Deadliest enemy", killer != null ? $"{killer}  ({kills})" : "None", false));
        rows.Add(("Deaths by hazards", RunStats.HazardDeaths.ToString(), false));

        float top = 0f;
        TextMeshProUGUI title = CreateText("Title", panel, statsTitle, 46f, value, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        PlaceRow(title.rectTransform, top, 64f);
        top += 84f;

        foreach ((string name, string number, bool small) in rows)
        {
            float size = small ? 28f : 36f;
            float height = small ? 38f : 50f;
            TextMeshProUGUI left = CreateText(name.Trim(), panel, name, size, small ? minor : label, TextAlignmentOptions.MidlineLeft);
            PlaceRow(left.rectTransform, top, height);
            TextMeshProUGUI right = CreateText(name.Trim() + " Value", panel, number, size, small ? minor : value, TextAlignmentOptions.MidlineRight);
            PlaceRow(right.rectTransform, top, height);
            if (!small) right.fontStyle = FontStyles.Bold;
            top += height;
        }

        hint = CreateText("Hint", panel, continueHint, 26f, minor, TextAlignmentOptions.Center);
        PlaceRow(hint.rectTransform, top + 30f, 40f);
        hint.alpha = 0f;

        return group;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string content, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    // A full-width row, top pixels below the top of the stats panel.
    private static void PlaceRow(RectTransform rect, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(0f, height);
    }

    private static bool AnyButtonPressed()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;

        Gamepad pad = Gamepad.current;
        return pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame
            || pad.startButton.wasPressedThisFrame);
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
