using UnityEngine;

// A beam that grows continuously along its own local +X axis, from 0 up to
// maxDistance, then retracts back to 0 and repeats - a descending laser
// curtain, not an instant full-length beam. Speed and max distance are both
// tunable per instance, so several of these at different speeds make for
// varied platforming hazards out of the same prefab.
//
// Growth is along local +X (not world space) because that's the axis
// crystal_ray's own artwork runs along (a wide strip, emitter on the left).
// Rotate the GameObject to point it wherever it needs to go - e.g. -90 on Z
// makes it grow straight down.
//
// Actual damage is handled entirely by HazardDamage, sitting on the same
// trigger collider - this script only owns the growing/shrinking visual and
// keeps the collider's size tracking it, so the hit area always matches
// exactly what's drawn on screen.
//
// The sprite's pivot must sit at the EMITTER end of the image (not center),
// and Draw Mode must be Tiled or Sliced - otherwise growing m_Size stretches
// from the middle instead of extending from the emitter, or has no visual
// effect at all.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class DescendingLaser : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float speed = 4f; // units/sec the beam grows or retracts
    [SerializeField] private float maxDistance = 6f; // how far it reaches at full extension
    // Beat of stillness at full extension and back at the origin, before
    // reversing direction. 0 means it reverses instantly.
    [SerializeField] private float pauseAtEnds = 0f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D hazardCollider;
    private float currentLength;
    private int direction = 1; // 1 = growing, -1 = retracting
    private float pauseTimer;

    // =========================
    // START
    // =========================
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hazardCollider = GetComponent<BoxCollider2D>();

        // HazardDamage only reacts to trigger colliders - enforced here so
        // a forgotten checkbox in the Inspector can't silently turn this
        // into a solid wall instead of a hazard.
        hazardCollider.isTrigger = true;

        ApplyLength();
    }

    // =========================
    // FIXED UPDATE
    // =========================
    private void FixedUpdate()
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            return;
        }

        currentLength += direction * speed * Time.fixedDeltaTime;

        if (currentLength >= maxDistance)
        {
            currentLength = maxDistance;
            direction = -1;
            pauseTimer = pauseAtEnds;
        }
        else if (currentLength <= 0f)
        {
            currentLength = 0f;
            direction = 1;
            pauseTimer = pauseAtEnds;
        }

        ApplyLength();
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Grows the sprite and its hitbox from this object's own origin, along
    // local +X, keeping both anchored at the emitter instead of growing
    // from the center.
    private void ApplyLength()
    {
        spriteRenderer.size = new Vector2(currentLength, spriteRenderer.size.y);

        hazardCollider.size = new Vector2(currentLength, hazardCollider.size.y);
        hazardCollider.offset = new Vector2(currentLength / 2f, hazardCollider.offset.y);
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * maxDistance);
    }
}
