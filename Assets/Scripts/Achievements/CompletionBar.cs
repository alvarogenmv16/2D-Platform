using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "% of the game completed" bar (main menu, achievements screen): the soul
// bar's frame and fill (UI_powerBar0 / UI_powerBar1) with the number inside.
// The fill is Achievements.Percent; when it went up since the last time a
// bar was shown, it fills up to the new value instead of jumping.
public class CompletionBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;          // Image Type: Filled, Horizontal, Origin Left
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private float fillTime = 1.2f;

    private const string LastShownKey = "Achievements.LastShownPercent";

    // For bars built in code (AchievementsScreen). Call before enabling.
    public void Setup(Image fill, TextMeshProUGUI text)
    {
        fillImage = fill;
        percentText = text;
    }

    private void OnEnable()
    {
        float target = Achievements.Percent;
        float from = Mathf.Min(PlayerPrefs.GetFloat(LastShownKey, 0f), target);

        SetValue(from);
        StartCoroutine(FillTo(from, target));

        PlayerPrefs.SetFloat(LastShownKey, target);
        PlayerPrefs.Save();
    }

    private IEnumerator FillTo(float from, float to)
    {
        if (Mathf.Approximately(from, to)) yield break;

        yield return new WaitForSecondsRealtime(0.3f);
        for (float t = 0f; t < fillTime; t += Time.unscaledDeltaTime)
        {
            SetValue(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / fillTime)));
            yield return null;
        }
        SetValue(to);
    }

    private void SetValue(float value)
    {
        if (fillImage != null) fillImage.fillAmount = value;
        if (percentText != null) percentText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
