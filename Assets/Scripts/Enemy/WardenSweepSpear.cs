using UnityEngine;

// One spear of the Warden's sweep attack: crosses the whole screen along a
// horizontal lane. Spawned (and set up through Launch) by WardenAI.
//
//   1. Aim: the spear's tip peeks in at the screen edge it comes from, and a
//      turquoise beam lights up along its lane, pulsing - the warning.
//   2. Flight: it shoots across to the other edge, hurting the player on
//      contact, and the beam fades out behind it.
//   3. Destroys itself once it's off the other side.
//
// Children: Spear (moves; WardenSpear1 points right) and Lane (static beam,
// the portal column sprite turned on its side).
public class WardenSweepSpear : MonoBehaviour
{
    [SerializeField] private Transform spear;
    [SerializeField] private SpriteRenderer lane;

    [Header("Damage")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private LayerMask playerLayer;
    // Hit box around the spear, in its own (unscaled) units.
    [SerializeField] private Vector2 hitBoxSize = new Vector2(4.6f, 0.75f);

    [Header("Motion")]
    [SerializeField] private float speed = 16f;
    // How much of the spear peeks in during the warning.
    [SerializeField] private float peekLength = 1.2f;
    [SerializeField] private float spearLength = 4.3f; // world length of the sprite at its scale

    [Header("Lane beam")]
    [SerializeField] private float laneMinAlpha = 0.2f;
    [SerializeField] private float laneMaxAlpha = 0.55f;
    [SerializeField] private float lanePulseSpeed = 12f;
    [SerializeField] private float laneFadeTime = 0.3f;

    [Header("Sound (optional)")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip launchSound;

    private float direction = 1f;   // +1 travels right, -1 left
    private float endX;
    private float aimTime;
    private float timer;
    private bool flying;
    private float fadeTimer;
    private Vector3 laneBaseScale;

    private void Awake()
    {
        if (lane != null) laneBaseScale = lane.transform.localScale;
    }

    // dir: +1 = from the left edge to the right, -1 = the other way.
    // leftX/rightX: the visible screen edges at that moment.
    public void Launch(float dir, float leftX, float rightX, float warningTime)
    {
        direction = dir >= 0f ? 1f : -1f;
        aimTime = warningTime;

        float startEdge = direction > 0f ? leftX : rightX;
        endX = (direction > 0f ? rightX : leftX) + direction * (spearLength + 1f);

        if (spear != null)
        {
            Vector3 s = spear.localScale;
            s.x = Mathf.Abs(s.x) * direction; // the sprite points right
            spear.localScale = s;
            // Only the tip inside the screen.
            spear.position = new Vector3(startEdge - direction * (spearLength * 0.5f - peekLength), transform.position.y, 0f);
        }

        if (lane != null)
        {
            // The column sprite grows "up" from its base: turned so its base
            // sits on the start edge and it stretches across the screen.
            lane.transform.position = new Vector3(startEdge, transform.position.y, 0f);
            lane.transform.rotation = Quaternion.Euler(0f, 0f, direction > 0f ? -90f : 90f);
            float width = Mathf.Abs(rightX - leftX);
            lane.transform.localScale = new Vector3(laneBaseScale.x, width / 4f, laneBaseScale.z); // sprite is 4 units long
            SetLaneAlpha(0f);
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (!flying)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(timer * lanePulseSpeed * (1f + timer));
            float appear = Mathf.Clamp01(timer / 0.15f);
            SetLaneAlpha(appear * Mathf.Lerp(laneMinAlpha, laneMaxAlpha, pulse));

            if (timer >= aimTime)
            {
                flying = true;
                if (sfxPlayer != null) sfxPlayer.Play(launchSound);
            }
            return;
        }

        fadeTimer += Time.deltaTime;
        SetLaneAlpha(laneFadeTime > 0f ? laneMaxAlpha * (1f - Mathf.Clamp01(fadeTimer / laneFadeTime)) : 0f);

        if (spear == null) { Destroy(gameObject); return; }

        spear.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);

        bool pastEnd = direction > 0f ? spear.position.x >= endX : spear.position.x <= endX;
        if (pastEnd) Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (!flying || spear == null) return;

        Vector2 size = new Vector2(hitBoxSize.x * Mathf.Abs(spear.lossyScale.x), hitBoxSize.y * Mathf.Abs(spear.lossyScale.y));
        Collider2D hit = Physics2D.OverlapBox(spear.position, size, 0f, playerLayer);
        if (hit == null) return;

        // PlayerHealth's own invulnerability window keeps this to one hit.
        if (hit.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(damage, spear.position);
        }
    }

    private void SetLaneAlpha(float alpha)
    {
        if (lane == null) return;

        Color c = lane.color;
        c.a = alpha;
        lane.color = c;
    }

    private void OnDrawGizmosSelected()
    {
        if (spear == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(spear.position, new Vector3(hitBoxSize.x * Mathf.Abs(spear.lossyScale.x), hitBoxSize.y * Mathf.Abs(spear.lossyScale.y), 0f));
    }
}
