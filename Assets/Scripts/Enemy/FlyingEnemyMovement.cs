using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingEnemyMovement : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    private Rigidbody2D rb;

    // =========================
    // START
    // =========================
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Moves in a straight line toward targetPosition at the given speed.
    // Returns true once arrived — snaps exactly onto the target instead
    // of letting velocity carry it past, which at high speeds caused it
    // to oscillate around the target and never register as "arrived".
    public bool MoveTowards(Vector2 targetPosition, float speed)
    {
        Vector2 currentPosition = rb.position;
        float distance = Vector2.Distance(currentPosition, targetPosition);
        float stepThisFrame = speed * Time.fixedDeltaTime;

        if (distance <= stepThisFrame)
        {
            // This frame's movement would reach or overshoot the target:
            // snap precisely onto it instead, for a clean, hard impact.
            rb.MovePosition(targetPosition);
            rb.linearVelocity = Vector2.zero;
            return true;
        }

        rb.linearVelocity = (targetPosition - currentPosition).normalized * speed;
        return false;
    }

    public void Stop()
    {
        rb.linearVelocity = Vector2.zero;
    }
}