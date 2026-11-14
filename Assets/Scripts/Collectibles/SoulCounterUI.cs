using TMPro;
using UnityEngine;

// HUD counter for SoulScore. Put it on a TextMeshProUGUI inside the scene's
// Canvas (e.g. next to a small soul icon). It reads the static score, so it
// needs no rebinding when the player crosses scenes.
[RequireComponent(typeof(TextMeshProUGUI))]
public class SoulCounterUI : MonoBehaviour
{
    [SerializeField] private string format = "x {0}";

    private TextMeshProUGUI label;

    private void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        SoulScore.OnChanged += Refresh;
        Refresh(SoulScore.Total);
    }

    private void OnDisable()
    {
        SoulScore.OnChanged -= Refresh;
    }

    private void Refresh(int total)
    {
        label.text = string.Format(format, total);
    }
}
