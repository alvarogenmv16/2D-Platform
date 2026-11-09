using UnityEngine;

// The warning column of a WardenSpearPortal: a turquoise beam of light that
// rises out of the portal while it's charging, pulsing, so the player sees
// where the spear is about to come out. On the erupt frame (OnSpikeErupt -
// the same Animation Event BossSpikeHazard receives, since Animation Events
// reach every script on the object) the column flares and fades, and the
// spear shoots up out of it; on OnSpikeRetract the spear sinks back.
//
// The spear grows from its base (pivot at the bottom, localScale.y 0 -> 1)
// instead of sliding up from below, so it looks right on any floor, even a
// thin platform with nothing underneath to hide it.
public class WardenPortalTelegraph : MonoBehaviour
{
    [Header("Column")]
    [SerializeField] private SpriteRenderer column;
    [SerializeField] private float growTime = 0.25f;
    [SerializeField] private float minAlpha = 0.35f;
    [SerializeField] private float maxAlpha = 0.75f;
    [SerializeField] private float pulseSpeed = 14f;
    [SerializeField] private float fadeOutTime = 0.2f;

    [Header("Spear (shoots up out of the column)")]
    [SerializeField] private Transform spear;
    [SerializeField] private float spearRiseTime = 0.08f;
    [SerializeField] private float spearSinkTime = 0.12f;

    private Vector3 columnScale;
    private Vector3 spearScale;
    private float timer;
    private bool erupted;
    private float fadeTimer;

    private float spearFrom, spearTo, spearMoveTime, spearTimer;

    private void Awake()
    {
        if (spear != null)
        {
            spearScale = spear.localScale;
            SetSpearGrowth(0f);
            spear.gameObject.SetActive(false);
        }

        if (column != null)
        {
            columnScale = column.transform.localScale;
            column.transform.localScale = new Vector3(columnScale.x, 0f, columnScale.z);
            SetAlpha(0f);
        }
    }

    private void Update()
    {
        UpdateSpear();

        if (column == null) return;

        if (!erupted)
        {
            timer += Time.deltaTime;
            float grow = growTime > 0f ? Mathf.Clamp01(timer / growTime) : 1f;
            column.transform.localScale = new Vector3(columnScale.x, columnScale.y * grow, columnScale.z);

            // Pulses faster the closer it gets to erupting.
            float pulse = 0.5f + 0.5f * Mathf.Sin(timer * pulseSpeed * (1f + timer));
            SetAlpha(grow * Mathf.Lerp(minAlpha, maxAlpha, pulse));
            return;
        }

        fadeTimer += Time.deltaTime;
        float t = fadeOutTime > 0f ? Mathf.Clamp01(fadeTimer / fadeOutTime) : 1f;
        SetAlpha(1f - t);
        if (t >= 1f) column.enabled = false;
    }

    // Animation Event on the erupt frame (shared with BossSpikeHazard).
    public void OnSpikeErupt()
    {
        erupted = true;
        fadeTimer = 0f;
        if (column != null) column.transform.localScale = columnScale;
        SetAlpha(1f);

        if (spear != null) spear.gameObject.SetActive(true);
        MoveSpear(1f, spearRiseTime);
    }

    // Animation Event: the spear starts sinking back.
    public void OnSpikeRetract()
    {
        MoveSpear(0f, spearSinkTime);
    }

    private void MoveSpear(float target, float duration)
    {
        if (spear == null) return;

        spearFrom = spearScale.y != 0f ? spear.localScale.y / spearScale.y : 0f;
        spearTo = target;
        spearMoveTime = duration;
        spearTimer = 0f;
    }

    private void UpdateSpear()
    {
        if (spear == null || spearTimer >= spearMoveTime) return;

        spearTimer += Time.deltaTime;
        float t = spearMoveTime > 0f ? Mathf.Clamp01(spearTimer / spearMoveTime) : 1f;
        // Fast start, eased end: a stab, not a slide.
        float eased = 1f - (1f - t) * (1f - t);
        SetSpearGrowth(Mathf.Lerp(spearFrom, spearTo, eased));

        if (t >= 1f && spearTo <= 0f) spear.gameObject.SetActive(false);
    }

    private void SetSpearGrowth(float growth)
    {
        spear.localScale = new Vector3(spearScale.x, spearScale.y * growth, spearScale.z);
    }

    private void SetAlpha(float alpha)
    {
        if (column == null) return;

        Color c = column.color;
        c.a = alpha;
        column.color = c;
    }
}
