using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The "Achievement unlocked" pop-up: slides in at the top-right corner,
// stays a few seconds and slides out. Several unlocks in a row queue up.
// Created on demand by Achievements.Unlock and kept across scenes - nothing
// to place in any scene. Runs on unscaled time, so it also works while the
// game is paused.
public class AchievementToast : MonoBehaviour
{
    private const float Width = 520f;
    private const float Height = 120f;
    private const float Margin = 30f;
    private const float SlideTime = 0.35f;
    private const float StayTime = 3f;

    private static AchievementToast instance;

    private readonly Queue<AchievementDatabase.Entry> queue = new Queue<AchievementDatabase.Entry>();
    private RectTransform panel;
    private TextMeshProUGUI titleText;
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

            Vector2 hidden = new Vector2(Width + Margin, -Margin);
            Vector2 shown = new Vector2(-Margin, -Margin);

            yield return Slide(hidden, shown);
            yield return new WaitForSecondsRealtime(StayTime);
            yield return Slide(shown, hidden);
        }
        showing = false;
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

        CreateText("Header", "ACHIEVEMENT UNLOCKED", 22f, new Color(0.8f, 0.65f, 1f), 0.62f, 0.95f);
        titleText = CreateText("Title", "", 34f, Color.white, 0.08f, 0.6f);
        titleText.fontStyle = FontStyles.Bold;
    }

    private TextMeshProUGUI CreateText(string name, string content, float size, Color color, float yMin, float yMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(panel, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, yMin);
        rect.anchorMax = new Vector2(1f, yMax);
        rect.offsetMin = new Vector2(24f, 0f);
        rect.offsetMax = new Vector2(-24f, 0f);

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
