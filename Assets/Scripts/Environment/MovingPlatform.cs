using System.Collections.Generic;
using UnityEngine;

// Moves back and forth (or loops) between a set of waypoints, carrying
// along anything standing on top of it. PlayerMovement overwrites the
// player's horizontal velocity every FixedUpdate, so friction alone would
// never carry the player with the platform - instead this applies its own
// frame-to-frame movement delta directly to every rider's Rigidbody2D
// position, on top of whatever velocity the rider already has.
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 2f;
    // Pause at each waypoint before continuing, in seconds.
    [SerializeField] private float pauseAtWaypoint = 0f;
    // False = ping-pong back and forth between the waypoints in order.
    // True = loop from the last waypoint back to the first.
    [SerializeField] private bool loop = false;

    private Rigidbody2D rb;
    private int targetIndex = 1;
    private int direction = 1;
    private float pauseTimer = 0f;

    // Any Rigidbody2D currently in contact with the platform's top/sides -
    // not just the Player, so a future enemy standing on a platform gets
    // carried too, for free.
    private readonly HashSet<Rigidbody2D> riders = new();

    // =========================
    // START
    // =========================
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Kinematic: driven entirely by the waypoint logic below, never by
        // gravity or incoming forces/collisions.
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (waypoints != null && waypoints.Length > 0)
        {
            rb.position = waypoints[0].position;
            targetIndex = waypoints.Length > 1 ? 1 : 0;
        }
    }

    // =========================
    // FIXED UPDATE
    // =========================
    private void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 previousPosition = rb.position;
        Vector2 target = waypoints[targetIndex].position;
        Vector2 newPosition = Vector2.MoveTowards(previousPosition, target, moveSpeed * Time.fixedDeltaTime);

        rb.MovePosition(newPosition);

        // Carry every rider along by exactly how far the platform just
        // moved, on top of their own velocity - see class comment.
        Vector2 delta = newPosition - previousPosition;
        if (delta != Vector2.zero)
        {
            foreach (Rigidbody2D rider in riders)
            {
                rider.position += delta;
            }
        }

        if (newPosition == target)
        {
            AdvanceWaypoint();
            pauseTimer = pauseAtWaypoint;
        }
    }

    private void AdvanceWaypoint()
    {
        if (loop)
        {
            targetIndex = (targetIndex + 1) % waypoints.Length;
            return;
        }

        // Ping-pong: bounce direction off both ends of the array.
        if (targetIndex + direction < 0 || targetIndex + direction >= waypoints.Length)
        {
            direction *= -1;
        }

        targetIndex += direction;
    }

    // =========================
    // RIDER TRACKING
    // =========================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.rigidbody != null)
        {
            riders.Add(collision.rigidbody);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.rigidbody != null)
        {
            riders.Remove(collision.rigidbody);
        }
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);

            // Skip the closing segment (last -> first) when ping-ponging,
            // since the platform never actually travels it.
            if (!loop && i == waypoints.Length - 1) continue;

            Transform next = waypoints[(i + 1) % waypoints.Length];
            if (next == null) continue;

            Gizmos.DrawLine(waypoints[i].position, next.position);
        }
    }
}
