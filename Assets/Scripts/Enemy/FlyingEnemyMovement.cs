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
    // Returns true once close enough to be considered "arrived".
    public bool MoveTowards(Vector2 targetPosition, float speed)
    {
        Vector2 toTarget = targetPosition - rb.position;
        float distance = toTarget.magnitude;

        if (distance < 0.1f)
        {
            rb.linearVelocity = Vector2.zero;
            return true;
        }

        rb.linearVelocity = toTarget.normalized * speed;
        return false;
    }

    public void Stop()
    {
        rb.linearVelocity = Vector2.zero;
    }
}