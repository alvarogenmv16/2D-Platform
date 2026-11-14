using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Boss title card, the same one the Warden gets in TempleScene: the screen
// fades to black, the boss' title fades in and holds, fades out, and the
// scene fades back in. Built at runtime on its own overlay canvas (drawn
// above everything, SceneFader included), so a scene needs no UI wiring -
// just a title sprite.
//
// Run it from a MonoBehaviour: yield return TitleCard.Play(...).
public static class TitleCard
{
    [Serializable]
    public class Settings
    {
        public Sprite titleSprite;
        // Fraction of the screen width the title takes up.
        [Range(0.2f, 1f)] public float titleWidth = 1f;
        public float fadeToBlackTime = 1.2f;
        public float titleFadeTime = 0.8f;
        public float titleHoldTime = 2f;
        public float fadeBackTime = 1f;
    }

    // atBlack: runs once the screen is fully black, before the title shows
    // (swap things in the scene, start the music...).
    public static IEnumerator Play(Settings settings, Action atBlack = null)
    {
        Image black, title;
        GameObject canvas = BuildOverlay(settings, out black, out title);

        yield return Fade(black, 0f, 1f, settings.fadeToBlackTime);

        atBlack?.Invoke();

        yield return Fade(title, 0f, 1f, settings.titleFadeTime);
        yield return new WaitForSeconds(settings.titleHoldTime);
        yield return Fade(title, 1f, 0f, settings.titleFadeTime);

        yield return Fade(black, 1f, 0f, settings.fadeBackTime);

        UnityEngine.Object.Destroy(canvas);
    }

    private static GameObject BuildOverlay(Settings settings, out Image black, out Image title)
    {
        GameObject canvasGo = new GameObject("TitleCardOverlay", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // above every other UI, the SceneFader included

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        black = CreateImage("Black", canvasGo.transform);
        black.color = new Color(0f, 0f, 0f, 0f);
        black.raycastTarget = true;
        RectTransform blackRect = black.rectTransform;
        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        blackRect.offsetMin = Vector2.zero;
        blackRect.offsetMax = Vector2.zero;

        title = CreateImage("Title", canvasGo.transform);
        title.sprite = settings.titleSprite;
        title.preserveAspect = true;
        title.color = new Color(1f, 1f, 1f, 0f);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f - settings.titleWidth * 0.5f, 0.2f);
        titleRect.anchorMax = new Vector2(0.5f + settings.titleWidth * 0.5f, 0.8f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        return canvasGo;
    }

    private static Image CreateImage(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static IEnumerator Fade(Image image, float from, float to, float duration)
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
