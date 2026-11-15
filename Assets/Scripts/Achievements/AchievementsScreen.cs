using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The achievements screen (AchievementsScene): the completion bar under the
// title and every achievement in Resources/Achievements, two columns,
// unlocked ones bright and locked ones dimmed. Hidden achievements show
// "???" until unlocked. Built in code from the database, so adding an
// achievement to the list is all it takes for it to appear here.
//
// Goes on the scene's Canvas. Back / Esc / B return to the main menu
// (BackButton + MenuBackButton, as in Settings).
public class AchievementsScreen : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Sprite barFrame;   // UI_powerBar0
    [SerializeField] private Sprite barFill;    // UI_powerBar1

    [Header("Layout (1920x1080 canvas)")]
    [SerializeField] private float barTop = 230f;        // from the top of the screen
    [SerializeField] private Vector2 barSize = new Vector2(700f, 75f);
    [SerializeField] private float listTop = 330f;       // from the top of the screen
    [SerializeField] private float listBottom = 180f;    // from the bottom (clear of the Back button)
    [SerializeField] private float listWidth = 1760f;    // all columns together
    [SerializeField] private float columnGap = 30f;
    [SerializeField] private float rowGap = 12f;
    [SerializeField] private float maxRowHeight = 100f;
    // Rows any shorter than this and another column is added (up to 3).
    [SerializeField] private float minRowHeight = 70f;

    [Header("Colours")]
    [SerializeField] private Color unlockedBackground = new Color(0.25f, 0.12f, 0.4f, 0.85f);
    [SerializeField] private Color lockedBackground = new Color(0.08f, 0.06f, 0.1f, 0.7f);
    [SerializeField] private Color unlockedTitle = Color.white;
    [SerializeField] private Color lockedTitle = new Color(0.55f, 0.55f, 0.6f);
    [SerializeField] private Color unlockedDescription = new Color(0.85f, 0.8f, 0.95f);
    [SerializeField] private Color lockedDescription = new Color(0.45f, 0.45f, 0.5f);
    [SerializeField] private Color unlockedBorder = new Color(0.75f, 0.55f, 1f, 0.9f);

    private void Start()
    {
        BuildBar();
        BuildList();
    }

    private void BuildBar()
    {
        RectTransform bar = CreateRect("CompletionBar", transform);
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = new Vector2(0f, -barTop);
        bar.sizeDelta = barSize;

        AddImage(bar.gameObject, barFrame);

        RectTransform fillRect = CreateRect("Fill", bar);
        Stretch(fillRect);
        Image fill = AddImage(fillRect.gameObject, barFill);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;

        TextMeshProUGUI percent = CreateText("Percent", bar, "0%", 34f, Color.white, TextAlignmentOptions.Center);
        percent.fontStyle = FontStyles.Bold;
        Stretch(percent.rectTransform);

        // CompletionBar reads its references in OnEnable: add it with the
        // object inactive and wire it first.
        bar.gameObject.SetActive(false);
        CompletionBar completion = bar.gameObject.AddComponent<CompletionBar>();
        completion.Setup(fill, percent);
        bar.gameObject.SetActive(true);

        TextMeshProUGUI count = CreateText("Count", transform,
            $"{Achievements.UnlockedCount} / {Achievements.Total}", 26f, lockedTitle, TextAlignmentOptions.Center);
        RectTransform countRect = count.rectTransform;
        countRect.anchorMin = countRect.anchorMax = new Vector2(0.5f, 1f);
        countRect.pivot = new Vector2(0.5f, 1f);
        countRect.anchoredPosition = new Vector2(0f, -(barTop + barSize.y + 2f));
        countRect.sizeDelta = new Vector2(barSize.x, 34f);
    }

    private void BuildList()
    {
        AchievementDatabase.Entry[] all = Achievements.All;
        if (all.Length == 0) return;

        float available = 1080f - listTop - listBottom;

        int columns = 1;
        int rows = all.Length;
        float rowHeight = RowHeight(rows, available);
        while (rowHeight < minRowHeight && columns < 3)
        {
            columns++;
            rows = Mathf.CeilToInt(all.Length / (float)columns);
            rowHeight = RowHeight(rows, available);
        }
        rowHeight = Mathf.Min(maxRowHeight, rowHeight);

        float columnWidth = (listWidth - (columns - 1) * columnGap) / columns;

        for (int i = 0; i < all.Length; i++)
        {
            int column = i / rows;   // fill the first column top to bottom, then the next
            int row = i % rows;
            float x = -listWidth * 0.5f + column * (columnWidth + columnGap) + columnWidth * 0.5f;
            float y = -(listTop + row * (rowHeight + rowGap));
            BuildRow(all[i], new Vector2(x, y), new Vector2(columnWidth, rowHeight));
        }
    }

    private float RowHeight(int rows, float available)
    {
        return (available - (rows - 1) * rowGap) / Mathf.Max(1, rows);
    }

    private void BuildRow(AchievementDatabase.Entry entry, Vector2 position, Vector2 size)
    {
        float height = size.y;
        bool isUnlocked = Achievements.IsUnlocked(entry.id);
        bool masked = entry.hidden && !isUnlocked;

        RectTransform row = CreateRect("Achievement_" + entry.id, transform);
        row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = position;
        row.sizeDelta = size;

        Image background = row.gameObject.AddComponent<Image>();
        background.color = isUnlocked ? unlockedBackground : lockedBackground;
        background.raycastTarget = false;
        if (isUnlocked)
        {
            Outline border = row.gameObject.AddComponent<Outline>();
            border.effectColor = unlockedBorder;
            border.effectDistance = new Vector2(2f, -2f);
        }

        TextMeshProUGUI title = CreateText("Title", row, masked ? "???" : entry.title,
            Mathf.Min(32f, height * 0.36f), isUnlocked ? unlockedTitle : lockedTitle, TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        // Long titles shrink to fit instead of being cut off.
        title.enableAutoSizing = true;
        title.fontSizeMax = title.fontSize;
        title.fontSizeMin = 14f;
        Place(title.rectTransform, 0.5f, 0.95f);
        title.rectTransform.offsetMax = new Vector2(-Mathf.Min(150f, size.x * 0.25f), 0f); // room for the state tag

        TextMeshProUGUI description = CreateText("Description", row, masked ? "Hidden achievement" : entry.description,
            Mathf.Min(24f, height * 0.27f), isUnlocked ? unlockedDescription : lockedDescription, TextAlignmentOptions.MidlineLeft);
        description.enableAutoSizing = true;
        description.fontSizeMax = description.fontSize;
        description.fontSizeMin = 12f;
        Place(description.rectTransform, 0.05f, 0.5f);

        TextMeshProUGUI state = CreateText("State", row, isUnlocked ? "UNLOCKED" : "LOCKED",
            Mathf.Min(20f, height * 0.22f), isUnlocked ? unlockedBorder : lockedDescription, TextAlignmentOptions.TopRight);
        RectTransform stateRect = state.rectTransform;
        stateRect.anchorMin = new Vector2(0.7f, 0.5f);
        stateRect.anchorMax = new Vector2(1f, 0.95f);
        stateRect.offsetMin = new Vector2(0f, 0f);
        stateRect.offsetMax = new Vector2(-18f, 0f);
    }

    // =========================
    // HELPERS
    // =========================

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image AddImage(GameObject go, Sprite sprite)
    {
        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string content, float size, Color color, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Text band inside a row, between two heights (0 = bottom, 1 = top).
    private static void Place(RectTransform rect, float yMin, float yMax)
    {
        rect.anchorMin = new Vector2(0f, yMin);
        rect.anchorMax = new Vector2(1f, yMax);
        rect.offsetMin = new Vector2(22f, 0f);
        rect.offsetMax = new Vector2(-22f, 0f);
    }
}
