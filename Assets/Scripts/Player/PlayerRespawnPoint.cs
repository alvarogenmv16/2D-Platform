using System.Collections.Generic;
using UnityEngine;

// Remembers where the player was standing on solid Ground a short moment
// ago, so a hazard hit can snap them back to it (Hollow Knight-style spike
// bump-back). Uses a small time-delayed history instead of the literal last
// grounded frame, so walking off a ledge onto a hazard doesn't respawn the
// player right at the edge, visually on top of the hazard.
//
// A position is only recorded when it's genuinely safe:
// - grounded on solid ground (PlayerMovement ignores trigger colliders, so
//   hazard tiles never count as ground, whatever layer they're on),
// - not on a MovingPlatform (it can carry the player next to a hazard),
// - no IHazard within hazardMargin of the player's collider,
// - not while a bump-back is in progress (BeginRespawn .. snap).
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRespawnPoint : MonoBehaviour
{
    // How far back in time the safe position lags behind the player's
    // actual grounded position. Bigger = more buffer between the respawn
    // point and a ledge, but also further from where they actually were.
    [SerializeField] private float safePositionDelay = 0.6f;

    // Extra room around the player's collider that must be free of hazards
    // for a position to count as safe (x = each side, y = above and below).
    // Keeps respawns off ledges right next to spikes/fire and out of
    // hazards that only just brush the player's feet.
    [SerializeField] private Vector2 hazardMargin = new Vector2(1f, 0.5f);

    private PlayerMovement playerMovement;
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private bool isRespawning;

    private readonly Queue<(float time, Vector2 position)> groundedHistory = new();
    private readonly List<Collider2D> hazardHits = new();
    private ContactFilter2D hazardFilter;

    public Vector2 LastSafePosition { get; private set; }

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();

        // Hazards are triggers on any layer, so check them all, triggers included.
        hazardFilter = ContactFilter2D.noFilter;
        hazardFilter.useTriggers = true;

        ResetSafePosition(transform.position);
    }

    private void FixedUpdate()
    {
        // GroundedCollider is also checked for null (Unity's overloaded
        // ==null, which is true for a destroyed object too) because a
        // scene transition can destroy the old scene's ground collider
        // mid-flight, between one FixedUpdate refreshing it and the next -
        // the player survives the load via DontDestroyOnLoad, but a
        // collider it was standing on in the old scene does not.
        if (!isRespawning
            && playerMovement.IsGrounded && playerMovement.GroundedCollider != null
            && playerMovement.GroundedCollider.GetComponent<MovingPlatform>() == null
            && !IsNearHazard())
        {
            groundedHistory.Enqueue((Time.time, (Vector2)transform.position));
        }

        // Drop samples older than the delay window, but always leave at
        // least one so LastSafePosition never goes empty. While airborne
        // for longer than the delay, this naturally collapses down to just
        // the single most recent grounded sample.
        while (groundedHistory.Count > 1 && Time.time - groundedHistory.Peek().time > safePositionDelay)
        {
            groundedHistory.Dequeue();
        }

        LastSafePosition = groundedHistory.Peek().position;
    }

    private bool IsNearHazard()
    {
        Bounds bounds = bodyCollider != null
            ? bodyCollider.bounds
            : new Bounds(transform.position, Vector3.one);

        Vector2 size = (Vector2)bounds.size + hazardMargin * 2f;
        int count = Physics2D.OverlapBox(bounds.center, size, 0f, hazardFilter, hazardHits);

        for (int i = 0; i < count; i++)
        {
            if (hazardHits[i].TryGetComponent(out IHazard _)) return true;
        }

        return false;
    }

    // Called by SceneFader as the bump-back starts: stops recording, so the
    // frames spent knocked back next to the hazard during the fade can't
    // become the new "safe" spot.
    public void BeginRespawn()
    {
        isRespawning = true;
    }

    // Called by SceneFader while the screen is fully black, so the jump
    // itself is never visible.
    public void SnapToLastSafePosition()
    {
        Vector2 target = LastSafePosition;
        transform.position = target;
        rb.position = target;
        rb.linearVelocity = Vector2.zero;

        ResetSafePosition(target);
    }

    // Forgets the whole history and starts over from position - for any
    // teleport (bump-back, RisingFlameSea's section restart...), so samples
    // from before it can't be picked as the next safe spot.
    public void ResetSafePosition(Vector2 position)
    {
        groundedHistory.Clear();
        groundedHistory.Enqueue((Time.time, position));
        LastSafePosition = position;
    }

    public void SetMovementEnabled(bool enabled)
    {
        playerMovement.enabled = enabled;

        // Movement coming back on = the bump-back is over.
        if (enabled)
        {
            isRespawning = false;
        }
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Collider2D col = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireCube(col.bounds.center, (Vector2)col.bounds.size + hazardMargin * 2f);
        }

        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(LastSafePosition, 0.3f);
        }
    }
}
