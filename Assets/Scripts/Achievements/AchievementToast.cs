using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The "Achievement unlocked" pop-up: slides in at the top-right corner with
// the achievement's card and name, plays the unlock sound (both set in
// Resources/Achievements), stays a few seconds and slides out. Several
// unlocks in a row queue up. Created on demand by Achievements.Unlock and
// kept across scenes - nothing to place in any scene. Runs on unscaled time
// and ignores the audio pause, so it also works in the pause menu.
public class AchievementToast : MonoBehaviour
{
    private const float Width = 560f;
    private const float Height = 150f;
    private const float Margin = 30f;
    private const float IconPadding = 12f;
    private const float SlideTime = 0.35f;
    private const float StayTime = 3.5f;

    private static AchievementToast instance;

    private readonly Queue<AchievementDatabase.Entry> queue = new Queue<AchievementDatabase.Entry>();
    private RectTransform panel;
    private Image icon;
    private TextMeshProUGUI titleText;
    private AudioSource audioSource;
    private bool showing;

    // No domain reload when entering Play Mode in this project: start clean.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    public static void Show(AchievementDatabase.Entry entry)
    {
        if (instance == null)
        {
            GameObject go = new GameObject("AchievementToast");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AchievementToast>();
            instance.Build();
        }

        instance.queue.Enqueue(entry);
        if (!instance.showing) instance.StartCoroutine(instance.ShowQueue());
    }

    private IEnumerator ShowQueue()
    {
        showing = true;
        while (queue.Count > 0)
        {
            AchievementDatabase.Entry entry = queue.Dequeue();
            titleText.text = entry.title;
            icon.sprite = entry.icon;
            icon.enabled = entry.icon != null;

            Vector2 hidden = new Vector2(Width + Margin, -Margin);
            Vector2 shown = new Vector2(-Margin, -Margin);

            PlaySound();
            yield return Slide(hidden, shown);
            yield return new WaitForSecondsRealtime(StayTime);
            yield return Slide(shown, hidden);
        }
        showing = false;
    }

    private void PlaySound()
    {
        AchievementDatabase database = Achievements.Database;
        if (database == null || database.unlockSound == null) return;
        audioSource.PlayOneShot(database.unlockSound, database.unlockVolume);
    }

    private IEnumerator Slide(Vector2 from, Vector2 to)
    {
        for (float t = 0f; t < SlideTime; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / SlideTime);
            panel.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
            yield return null;
        }
        panel.anchoredPosition = to;
    }

    private void Build()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true; // the pause menu pauses the rest

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100; // above the pause menu

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelGo = new GameObject("Panel", typeof(RectTransform));
        panelGo.transform.SetParent(transform, false);
        panel = (RectTransform)panelGo.transform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f);
        panel.sizeDelta = new Vector2(Width, Height);
        panel.anchoredPosition = new Vector2(Width + Margin, -Margin);

        Image background = panelGo.AddComponent<Image>();
        background.color = new Color(0.06f, 0.03f, 0.1f, 0.92f);
        background.raycastTarget = false;

        Outline border = panelGo.AddComponent<Outline>();
        border.effectColor = new Color(0.75f, 0.55f, 1f, 0.9f);
        border.effectDistance = new Vector2(2f, -2f);

        // The card on the left, keeping its proportions.
        GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(panel, false);
        RectTransform iconRect = (RectTransform)iconGo.transform;
        iconRect.anchorMin = new Vector2(0f, 0f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        float iconHeight = Height - IconPadding * 2f;
        iconRect.sizeDelta = new Vector2(iconHeight * 0.84f, -IconPadding * 2f);
        iconRect.anchoredPosition = new Vector2(IconPadding, 0f);
        icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        float textLeft = IconPadding * 2f + iconHeight * 0.84f;
        CreateText("Header", "ACHIEVEMENT UNLOCKED", 22f, new Color(0.8f, 0.65f, 1f), 0.58f, 0.9f, textLeft);
        titleText = CreateText("Title", "", 32f, Color.white, 0.12f, 0.58f, textLeft);
        titleText.fontStyle = FontStyles.Bold;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMax = 32f;
        titleText.fontSizeMin = 16f;
    }

    private TextMeshProUGUI CreateText(string name, string content, float size, Color color, float yMin, float yMax, float left)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(panel, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, yMin);
        rect.anchorMax = new Vector2(1f, yMax);
        rect.offsetMin = new Vector2(left, 0f);
        rect.offsetMax = new Vector2(-20f, 0f);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }
}
