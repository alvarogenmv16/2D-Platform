using System.Collections.Generic;
using UnityEngine;

// Remembers where the player was standing on solid Ground a short moment
// ago, so a hazard hit can snap them back to it (Hollow Knight-style spike
// bump-back). Uses a small time-delayed history instead of the literal last
// grounded frame, so walking off a ledge onto a hazard doesn't respawn the
// player right at the edge, visually on top of the hazard.
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRespawnPoint : MonoBehaviour
{
    // How far back in time the safe position lags behind the player's
    // actual grounded position. Bigger = more buffer between the respawn
    // point and a ledge, but also further from where they actually were.
    [SerializeField] private float safePositionDelay = 0.6f;

    private PlayerMovement playerMovement;
    private Rigidbody2D rb;

    private readonly Queue<(float time, Vector2 position)> groundedHistory = new();

    public Vector2 LastSafePosition { get; private set; }

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
        LastSafePosition = transform.position;
        groundedHistory.Enqueue((Time.time, LastSafePosition));
    }

    private void FixedUpdate()
    {
        // isGrounded only ever comes from the Ground layer (hazards are a
        // separate trigger layer), so this can never record a position
        // inside/above a hazard.
        if (playerMovement.IsGrounded)
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

    // Called by SceneFader while the screen is fully black, so the jump
    // itself is never visible.
    public void SnapToLastSafePosition()
    {
        transform.position = LastSafePosition;
        rb.linearVelocity = Vector2.zero;
    }

    public void SetMovementEnabled(bool enabled)
    {
        playerMovement.enabled = enabled;
    }
}
