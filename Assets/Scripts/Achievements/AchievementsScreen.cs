using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The achievements screen (AchievementsScene): the completion bar under the
// title and a grid with one card per achievement in Resources/Achievements
// (7 per row, the same order as the sprite sheet).
//   - Unlocked: its card (the entry's icon) with its description below.
//   - Locked: a blank card with a "?" and nothing else - no picture, name
//     or description, so it gives no clue about how to get it.
// Built in code from the database, so adding an achievement to the list is
// all it takes for it to appear here.
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
    [SerializeField] private float barTop = 160f;        // from the top of the screen
    [SerializeField] private Vector2 barSize = new Vector2(700f, 75f);
    [SerializeField] private float gridTop = 330f;       // from the top of the screen
    [SerializeField] private float gridBottom = 160f;    // from the bottom (clear of the Back button)
    [SerializeField] private float gridWidth = 1760f;
    [SerializeField] private int columns = 7;
    [SerializeField] private float columnGap = 20f;
    [SerializeField] private float rowGap = 14f;
    [SerializeField] private float descriptionHeight = 40f;
    [SerializeField] private float cardAspect = 0.83f;  // width / height of the cards

    [Header("Colours")]
    [SerializeField] private Color countColor = new Color(0.55f, 0.55f, 0.6f);
    [SerializeField] private Color descriptionColor = new Color(0.85f, 0.8f, 0.95f);
    [SerializeField] private Color lockedCard = new Color(0.08f, 0.06f, 0.12f, 0.9f);
    [SerializeField] private Color lockedBorder = new Color(0.35f, 0.3f, 0.45f, 0.9f);
    [SerializeField] private Color lockedMark = new Color(0.4f, 0.36f, 0.5f);

    private void Start()
    {
        BuildBar();
        BuildGrid();
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

        TextMeshProUGUI percent = CreateText("Percent", bar, "0%", 34f, Color.white);
        percent.fontStyle = FontStyles.Bold;
        Stretch(percent.rectTransform);

        // CompletionBar reads its references in OnEnable: add it with the
        // object inactive and wire it first.
        bar.gameObject.SetActive(false);
        CompletionBar completion = bar.gameObject.AddComponent<CompletionBar>();
        completion.Setup(fill, percent);
        bar.gameObject.SetActive(true);

        TextMeshProUGUI count = CreateText("Count", transform,
            $"{Achievements.UnlockedCount} / {Achievements.Total}", 26f, countColor);
        RectTransform countRect = count.rectTransform;
        countRect.anchorMin = countRect.anchorMax = new Vector2(0.5f, 1f);
        countRect.pivot = new Vector2(0.5f, 1f);
        countRect.anchoredPosition = new Vector2(0f, -(barTop + barSize.y + 2f));
        countRect.sizeDelta = new Vector2(barSize.x, 34f);
    }

    private void BuildGrid()
    {
        AchievementDatabase.Entry[] all = Achievements.All;
        if (all.Length == 0) return;

        int cols = Mathf.Max(1, columns);
        int rows = Mathf.CeilToInt(all.Length / (float)cols);

        float available = 1080f - gridTop - gridBottom;
        float cellWidth = (gridWidth - (cols - 1) * columnGap) / cols;
        float cellHeight = (available - (rows - 1) * rowGap) / rows;

        // The biggest card that fits the cell, above its description.
        float cardHeight = Mathf.Min(cellHeight - descriptionHeight, cellWidth / cardAspect);
        Vector2 cardSize = new Vector2(cardHeight * cardAspect, cardHeight);

        for (int i = 0; i < all.Length; i++)
        {
            int row = i / cols;
            int column = i % cols;
            float x = -gridWidth * 0.5f + column * (cellWidth + columnGap) + cellWidth * 0.5f;
            float y = -(gridTop + row * (cellHeight + rowGap));
            BuildCell(all[i], new Vector2(x, y), new Vector2(cellWidth, cellHeight), cardSize);
        }
    }

    private void BuildCell(AchievementDatabase.Entry entry, Vector2 position, Vector2 cellSize, Vector2 cardSize)
    {
        bool isUnlocked = Achievements.IsUnlocked(entry.id);

        RectTransform cell = CreateRect("Achievement_" + (isUnlocked ? entry.id : "Locked"), transform);
        cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 1f);
        cell.pivot = new Vector2(0.5f, 1f);
        cell.anchoredPosition = position;
        cell.sizeDelta = cellSize;

        RectTransform card = CreateRect("Card", cell);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 1f);
        card.pivot = new Vector2(0.5f, 1f);
        card.anchoredPosition = Vector2.zero;
        card.sizeDelta = cardSize;

        if (isUnlocked && entry.icon != null)
        {
            Image icon = AddImage(card.gameObject, entry.icon);
            icon.preserveAspect = true;
        }
        else if (isUnlocked)
        {
            // Unlocked but without a card sprite yet: show its name instead.
            Image blank = AddImage(card.gameObject, null);
            blank.color = lockedCard;
            TextMeshProUGUI name = CreateText("Title", card, entry.title, 20f, Color.white);
            name.textWrappingMode = TextWrappingModes.Normal;
            Stretch(name.rectTransform);
        }
        else
        {
            Image blank = AddImage(card.gameObject, null);
            blank.color = lockedCard;
            Outline border = card.gameObject.AddComponent<Outline>();
            border.effectColor = lockedBorder;
            border.effectDistance = new Vector2(2f, -2f);

            TextMeshProUGUI mark = CreateText("Mark", card, "?", cardSize.y * 0.45f, lockedMark);
            mark.fontStyle = FontStyles.Bold;
            Stretch(mark.rectTransform);
        }

        // Description only once unlocked - a locked card gives no hints.
        if (!isUnlocked) return;

        TextMeshProUGUI description = CreateText("Description", cell, entry.description, 16f, descriptionColor);
        description.textWrappingMode = TextWrappingModes.Normal;
        description.enableAutoSizing = true;
        description.fontSizeMax = 16f;
        description.fontSizeMin = 11f;
        RectTransform descRect = description.rectTransform;
        descRect.anchorMin = new Vector2(0f, 1f);
        descRect.anchorMax = new Vector2(1f, 1f);
        descRect.pivot = new Vector2(0.5f, 1f);
        descRect.anchoredPosition = new Vector2(0f, -(cardSize.y + 4f));
        descRect.sizeDelta = new Vector2(0f, descriptionHeight - 4f);
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

    private TextMeshProUGUI CreateText(string name, Transform parent, string content, float size, Color color)
    {
        TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
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
}
