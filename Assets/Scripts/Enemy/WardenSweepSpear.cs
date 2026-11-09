using UnityEngine;

// One flying spear of the Warden: crosses the screen along a straight line.
// Spawned (and set up through Launch / LaunchAlong) by WardenAI.
//   - Sweep: horizontal lanes, from one screen edge to the other.
//   - Phase 2 diagonal punch: diagonal lines falling from above the screen
//     down to the floor.
//
//   1. Aim: the spear's tip peeks in at the start of its line and a
//      turquoise beam lights up along the line, pulsing - the warning.
//   2. Flight: it shoots along the line, hurting the player on contact, and
//      the beam fades out.
//   3. Destroys itself once it has gone past the end of the line.
//
// Children: Spear (moves; WardenSpear1 points along +x, so it's rotated to
// the flight direction) and Lane (static beam, the portal column sprite
// turned to lie along the line).
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

    private Vector2 direction = Vector2.right;
    private float travelLength;     // from the line's start to its end
    private float travelled;
    private float aimTime;
    private float timer;
    private bool flying;
    private float fadeTimer;
    private Vector3 laneBaseScale;

    private void Awake()
    {
        if (lane != null) laneBaseScale = lane.transform.localScale;
    }

    // Horizontal sweep. dir: +1 = from the left edge to the right, -1 = the
    // other way. leftX/rightX: the visible screen edges at that moment.
    public void Launch(float dir, float leftX, float rightX, float warningTime)
    {
        float d = dir >= 0f ? 1f : -1f;
        Vector2 start = new Vector2(d > 0f ? leftX : rightX, transform.position.y);
        LaunchAlong(new Vector2(d, 0f), start, Mathf.Abs(rightX - leftX), warningTime, false, -1f);
    }

    // Any straight line: from lineStart along lineDirection for lineLength.
    // beamFromEnd: the warning beam is brightest at the end of the line
    // instead of the start (diagonal spears: bright where they hit the floor,
    // where the player is). flightSpeed < 0 keeps the prefab's speed.
    public void LaunchAlong(Vector2 lineDirection, Vector2 lineStart, float lineLength, float warningTime,
                            bool beamFromEnd, float flightSpeed)
    {
        direction = lineDirection.sqrMagnitude > 0f ? lineDirection.normalized : Vector2.right;
        travelLength = lineLength + spearLength + 1f;
        aimTime = warningTime;
        if (flightSpeed > 0f) speed = flightSpeed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (spear != null)
        {
            // Only the tip inside the line's start.
            spear.position = lineStart - direction * (spearLength * 0.5f - peekLength);
            spear.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (lane != null)
        {
            // The column sprite grows "up" from its base: turned so it lies
            // along the line, base at whichever end should be brightest.
            Vector2 baseEnd = beamFromEnd ? lineStart + direction * lineLength : lineStart;
            float beamAngle = beamFromEnd ? angle + 90f : angle - 90f;
            lane.transform.SetPositionAndRotation(baseEnd, Quaternion.Euler(0f, 0f, beamAngle));
            lane.transform.localScale = new Vector3(laneBaseScale.x, lineLength / 4f, laneBaseScale.z); // sprite is 4 units long
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

        float step = speed * Time.deltaTime;
        spear.position += (Vector3)(direction * step);
        travelled += step;

        if (travelled >= travelLength) Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (!flying || spear == null) return;

        Collider2D hit = Physics2D.OverlapBox(spear.position, HitSize, spear.eulerAngles.z, playerLayer);
        if (hit == null) return;

        // PlayerHealth's own invulnerability window keeps this to one hit.
        if (hit.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(damage, spear.position);
        }
    }

    private Vector2 HitSize => new Vector2(hitBoxSize.x * Mathf.Abs(spear.lossyScale.x), hitBoxSize.y * Mathf.Abs(spear.lossyScale.y));

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
        Gizmos.matrix = Matrix4x4.TRS(spear.position, spear.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, HitSize);
    }
}
