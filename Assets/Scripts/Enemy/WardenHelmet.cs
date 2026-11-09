using System.Collections;
using UnityEngine;

// The Warden's weak point: only its helmet can be hit. A trigger BoxCollider2D
// on the root (where EnemyHealth lives - the player's attack looks for
// EnemyHealth on the collider it hits) that follows the helmet as the pose
// changes: each frame it looks up the sprite Visuals is showing and moves the
// box to that pose's helmet.
//
// Each hit (EnemyHealth.OnHealthChanged going down) briefly flashes the
// helmet white: a child SpriteRenderer (HelmetFlash, on Visuals) shows that
// pose's white helmet overlay - same size and pivot as the pose sprite, so it
// sits exactly on top - and fades it out.
[RequireComponent(typeof(BoxCollider2D))]
public class WardenHelmet : MonoBehaviour
{
    [System.Serializable]
    private struct PoseHelmet
    {
        public Sprite pose;          // the body sprite (Warden_Idle0, ...)
        public Vector2 offset;       // helmet centre, from the root (feet)
        public Vector2 size;
        public Sprite flash;         // white helmet overlay for that pose
    }

    [SerializeField] private SpriteRenderer body;        // Visuals
    [SerializeField] private SpriteRenderer flashRenderer;
    [SerializeField] private PoseHelmet[] poses;

    [Header("Hit flash")]
    [SerializeField] private float flashAlpha = 0.9f;
    [SerializeField] private float flashTime = 0.15f;

    private BoxCollider2D helmetBox;
    private EnemyHealth health;
    private Sprite lastPose;
    private float lastHealth = float.MaxValue;
    private Coroutine flashRoutine;

    private void Awake()
    {
        helmetBox = GetComponent<BoxCollider2D>();
        helmetBox.isTrigger = true;

        health = GetComponent<EnemyHealth>();
        if (health != null) health.OnHealthChanged.AddListener(OnHealthChanged);

        if (flashRenderer != null)
        {
            SetFlashAlpha(0f);
        }
    }

    private void OnDestroy()
    {
        if (health != null) health.OnHealthChanged.RemoveListener(OnHealthChanged);
    }

    private void LateUpdate()
    {
        if (body == null || body.sprite == lastPose) return;

        lastPose = body.sprite;
        foreach (PoseHelmet p in poses)
        {
            if (p.pose != lastPose) continue;

            helmetBox.offset = p.offset;
            helmetBox.size = p.size;
            if (flashRenderer != null) flashRenderer.sprite = p.flash;
            return;
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        // Only real hits (health going down), not the bar's refresh at full.
        if (current < lastHealth && current < max) Flash();
        lastHealth = current;
    }

    private void Flash()
    {
        if (flashRenderer == null) return;

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
        flashRoutine = null;
    }

    private void SetFlashAlpha(float alpha)
    {
        Color c = flashRenderer.color;
        c.a = alpha;
        flashRenderer.color = c;
    }
}
