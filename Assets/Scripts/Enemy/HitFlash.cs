using System.Collections;
using UnityEngine;

// White hit flash over an enemy's whole body - the same look as the Warden's
// helmet flash (WardenHelmet), but without hand-made white sprites: a child
// SpriteRenderer copies whatever sprite the body is showing each frame and
// draws it with the HitFlashWhite material (Sprite-WhiteFlash shader), which
// paints the sprite's silhouette plain white. Each hit (EnemyHealth
// .OnHealthChanged going down) shows it and fades it out.
//
// Goes on the same GameObject as EnemyHealth. The overlay is created at
// runtime, so nothing else needs to be set up in the hierarchy.
[RequireComponent(typeof(EnemyHealth))]
public class HitFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;        // the animated sprite (e.g. Visuals_eye)
    [SerializeField] private Material flashMaterial;     // HitFlashWhite

    [Header("Hit flash")]
    [SerializeField] private float flashAlpha = 0.9f;
    [SerializeField] private float flashTime = 0.15f;

    private SpriteRenderer flashRenderer;
    private EnemyHealth health;
    private float lastHealth = float.MaxValue;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>(true);

        health = GetComponent<EnemyHealth>();
        health.OnHealthChanged.AddListener(OnHealthChanged);

        if (body != null && flashMaterial != null)
        {
            GameObject overlay = new GameObject("HitFlash");
            overlay.transform.SetParent(body.transform, false);

            flashRenderer = overlay.AddComponent<SpriteRenderer>();
            flashRenderer.sharedMaterial = flashMaterial;
            flashRenderer.color = new Color(1f, 1f, 1f, 0f);
            flashRenderer.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (health != null) health.OnHealthChanged.RemoveListener(OnHealthChanged);
    }

    private void LateUpdate()
    {
        if (flashRenderer == null || flashRoutine == null) return;

        // Follow the body's current animation frame, facing and sorting.
        flashRenderer.sprite = body.sprite;
        flashRenderer.flipX = body.flipX;
        flashRenderer.flipY = body.flipY;
        flashRenderer.sortingLayerID = body.sortingLayerID;
        flashRenderer.sortingOrder = body.sortingOrder + 1;
        flashRenderer.enabled = body.enabled && body.gameObject.activeInHierarchy;
    }

    private void OnHealthChanged(float current, float max)
    {
        // Only real hits (health going down), not a bar refresh at full.
        if (current < lastHealth && current < max) Flash();
        lastHealth = current;
    }

    private void Flash()
    {
        if (flashRenderer == null || !isActiveAndEnabled) return;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (float t = 0f; t < flashTime; t += Time.deltaTime)
        {
            SetFlashAlpha(flashAlpha * (1f - t / flashTime));
            yield return null;
        }
        SetFlashAlpha(0f);
        flashRenderer.enabled = false;
        flashRoutine = null;
    }

    private void SetFlashAlpha(float alpha)
    {
        Color c = flashRenderer.color;
        c.a = alpha;
        flashRenderer.color = c;
    }
}
