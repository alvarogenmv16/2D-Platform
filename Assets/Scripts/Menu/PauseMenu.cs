using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pause menu: Esc (keyboard) or Start (gamepad) freezes the game and shows
// "Continue" / "Quit to Menu". Pressing Esc/Start again also continues.
//
// Nothing to place in the scenes: it creates itself once at startup from
// Resources/PauseMenu.prefab (which only holds the look - font and button
// sprite, the same as the main menu's), builds its own UI in code and
// survives every scene load (DontDestroyOnLoad). It does nothing in the menu
// scenes (notPausableScenes) or while there's no player.
public class PauseMenu : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private TMP_FontAsset font;          // empty = TMP's default
    [SerializeField] private Sprite buttonSprite;         // UI/Buttons.png (Buttons_0), as in the main menu
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color textColor = Color.white;

    [Header("Sound")]
    [SerializeField] private AudioClip moveSound; // moving between options; empty = generated tick
    [SerializeField, Range(0f, 1f)] private float moveVolume = 0.5f;

    [Header("Text")]
    [SerializeField] private string title = "PAUSED";
    [SerializeField] private string continueLabel = "Continue";
    [SerializeField] private string quitLabel = "Quit to Menu";

    [Header("Scenes")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string[] notPausableScenes = { "MainMenu", "SettingsScene" };

    public static bool IsPaused { get; private set; }

    private static PauseMenu instance;

    private InputAction pauseAction;
    private GameObject root;
    private Button continueButton;
    private float timeScaleBeforePause = 1f;
    private InputSystem_Actions pausedPlayerInput;
    private bool playerInputWasEnabled;

    // =========================
    // BOOTSTRAP
    // =========================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateOnStartup()
    {
        if (instance != null) return;

        GameObject prefab = Resources.Load<GameObject>("PauseMenu");
        GameObject menu = prefab != null ? Instantiate(prefab) : new GameObject("PauseMenu", typeof(PauseMenu));
        menu.name = "PauseMenu";
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        IsPaused = false;

        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");
        pauseAction.Enable();

        BuildUI();
        root.SetActive(false);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        pauseAction?.Dispose();
        if (IsPaused) Resume();
        instance = null;
    }

    // A scene change while paused (shouldn't happen, but just in case)
    // must never leave the game frozen.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsPaused) Resume();
    }

    private void Update()
    {
        if (!pauseAction.WasPressedThisFrame()) return;

        if (IsPaused) Resume();
        else if (CanPause()) Pause();
    }

    // =========================
    // PAUSE / RESUME
    // =========================

    private bool CanPause()
    {
        string scene = SceneManager.GetActiveScene().name;
        foreach (string blocked in notPausableScenes)
        {
            if (scene == blocked) return false;
        }
        return GameObject.FindGameObjectWithTag("Player") != null;
    }

    private void Pause()
    {
        IsPaused = true;

        timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // Every player input (move, jump, attack, heal, interact...) is read
        // from PlayerMovement's InputSystem_Actions - switching its Player
        // map off keeps button presses in the menu from reaching the game.
        pausedPlayerInput = null;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.TryGetComponent(out PlayerMovement movement) && movement.InputActions != null)
        {
            pausedPlayerInput = movement.InputActions;
            playerInputWasEnabled = pausedPlayerInput.Player.enabled;
            pausedPlayerInput.Player.Disable();
        }

        // MenuNavigator (on the canvas) selects Continue when it's shown.
        EnsureEventSystem();
        root.SetActive(true);
    }

    private void Resume()
    {
        IsPaused = false;

        root.SetActive(false);
        Time.timeScale = timeScaleBeforePause;
        AudioListener.pause = false;

        if (pausedPlayerInput != null && playerInputWasEnabled)
        {
            pausedPlayerInput.Player.Enable();
        }
        pausedPlayerInput = null;

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void QuitToMenu()
    {
        Resume();
        // Drops the run in progress (the persistent player, souls, flags):
        // the main menu's Continue reloads it all from the save file.
        SaveSystem.QuitToMenu(mainMenuScene);
    }

    // Every scene has its own EventSystem, but the menu must be usable with
    // keyboard/gamepad even in one that doesn't.
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(es);
    }

    // =========================
    // UI
    // =========================

    private void BuildUI()
    {
        root = new GameObject("PauseCanvas", typeof(RectTransform));
        root.transform.SetParent(transform, false);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // above the HUD, dialogue and fades

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // Dark overlay over the whole screen (also blocks clicks behind it).
        Image overlay = CreateChild("Overlay", root.transform).AddComponent<Image>();
        Stretch(overlay.rectTransform);
        overlay.color = overlayColor;

        // Title.
        TextMeshProUGUI titleText = CreateText("Title", root.transform, title, 96f);
        titleText.fontStyle = FontStyles.Bold;
        Place(titleText.rectTransform, new Vector2(0f, 220f), new Vector2(1200f, 150f));

        // Buttons.
        continueButton = CreateButton("Continue", continueLabel, new Vector2(0f, 20f), Resume);
        Button quitButton = CreateButton("Quit", quitLabel, new Vector2(0f, -150f), QuitToMenu);

        // Explicit up/down navigation between the two.
        Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
        nav.selectOnDown = quitButton;
        nav.selectOnUp = quitButton;
        continueButton.navigation = nav;
        nav.selectOnDown = continueButton;
        nav.selectOnUp = continueButton;
        quitButton.navigation = nav;

        root.AddComponent<MenuNavigator>().Setup(new Selectable[] { continueButton }, moveSound, moveVolume);
    }

    private Button CreateButton(string name, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = CreateChild(name, root.transform);
        Image image = go.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.color = buttonSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
        Place(image.rectTransform, position, new Vector2(444f, 150f)); // Buttons_0's own 296x100 proportions

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        // Same colour tint as the main menu's buttons, but with a clearer
        // highlight: with a gamepad the selected one has to stand out.
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText("Label", go.transform, label, 44f);
        Stretch(text.rectTransform);

        return button;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string content, float size)
    {
        TextMeshProUGUI text = CreateChild(name, parent).AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
