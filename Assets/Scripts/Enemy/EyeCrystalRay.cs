using UnityEngine;

// Phase 2 hazard segment. EyeAI spawns a whole row (or column) of these at
// once, spaced across the room like BossAI's spike row, always leaving one
// random segment out as a gap — the player has to actually reach that gap.
// Unlike a stationary spike, each segment then slides all the way across
// the room along its own axis (horizontal segments sweep horizontally,
// vertical ones sweep vertically) until it exits the far side, instead of
// sitting fixed in one spot — that travel is what actually guarantees full
// coverage of the room, rather than relying on each segment's static size
// matching the room exactly. Telegraphs first (dim, harmless, doesn't move
// yet) so there's time to read where it'll start, then goes solid/dangerous
// and starts sliding. Damages the player once and only once: if they still
// end up trapped against a segment when it goes solid, repeatedly chipping
// them every time their invulnerability window expires would be unfair
// given they can't get away, so a single hit is all any one segment can
// ever deal.
[RequireComponent(typeof(SpriteRenderer))]
public class EyeCrystalRay : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float damage = 1f;
    // Matches the beam sprite's actual rendered bounds — tune per prefab
    // variant (wide+thin for a horizontal segment, thin+tall for a vertical
    // segment).
    [SerializeField] private Vector2 hitboxSize = new Vector2(2f, 0.6f);
    // Local-space offset from this object's pivot, rotated along with it —
    // lets the hitbox be nudged to match the sprite's actual art (which
    // rarely sits perfectly centered on its pivot) without moving the
    // GameObject itself.
    [SerializeField] private Vector2 hitboxOffset;
    [SerializeField] private LayerMask playerLayer;

    [Header("Timing")]
    [SerializeField] private float telegraphDuration = 0.75f; // harmless warning window, at the spawn edge, before it starts sliding
    [SerializeField] private Color telegraphColor = new Color(1f, 1f, 1f, 0.35f);

    private SpriteRenderer spriteRenderer;
    private Color activeColor;
    private bool isDangerous = false;
    private bool hasHit = false;

    private Vector2 moveDirection; // unit axis vector: (+-1, 0) or (0, +-1)
    private float speed;
    private float travelLimit; // world-space X or Y coordinate where this segment is destroyed
    private bool movingOnX;

    // =========================
    // START
    // =========================
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        activeColor = spriteRenderer.color;
    }

    private void Start()
    {
        spriteRenderer.color = telegraphColor;
        Invoke(nameof(Activate), telegraphDuration);
    }

    // =========================
    // FIXED UPDATE
    // =========================
    private void FixedUpdate()
    {
        if (!isDangerous) return;

        transform.position += (Vector3)(moveDirection * speed * Time.fixedDeltaTime);

        if (!hasHit)
        {
            Vector2 boxCenter = transform.TransformPoint(hitboxOffset);
            Collider2D hit = Physics2D.OverlapBox(boxCenter, hitboxSize, transform.eulerAngles.z, playerLayer);

            if (hit != null)
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();

                if (playerHealth != null)
                {
                    // Set before TakeDamage, not after — guarantees this
                    // segment never lands a second hit even if it somehow
                    // got re-entered the same physics step.
                    hasHit = true;
                    playerHealth.TakeDamage(damage, boxCenter);
                }
            }
        }

        float currentCoord = movingOnX ? transform.position.x : transform.position.y;
        float travelSign = movingOnX ? Mathf.Sign(moveDirection.x) : Mathf.Sign(moveDirection.y);
        bool reachedLimit = travelSign > 0f ? currentCoord >= travelLimit : currentCoord <= travelLimit;

        if (reachedLimit)
        {
            Destroy(gameObject);
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Called by EyeAI right after instantiating this segment, at the wall/
    // floor/ceiling it should start from.
    public void Launch(Vector2 moveDirection, float speed, float travelLimit)
    {
        this.moveDirection = moveDirection;
        this.speed = speed;
        this.travelLimit = travelLimit;
        movingOnX = Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.y);
    }

    private void Activate()
    {
        isDangerous = true;
        spriteRenderer.color = activeColor;
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(hitboxOffset, hitboxSize);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
