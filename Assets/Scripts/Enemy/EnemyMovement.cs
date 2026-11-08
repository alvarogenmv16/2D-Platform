using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float moveSpeed = 5f;

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

    // direction should be -1, 0 or 1 (sign only). Passing arbitrary
    // fractional values will scale speed unintentionally.
    public void Move(float direction)
    {
        Move(direction, moveSpeed);
    }

    // Same as Move(direction), but at an explicit speed instead of
    // moveSpeed - for enemies that switch between speeds (e.g. a charge).
    public void Move(float direction, float speed)
    {
        rb.linearVelocity = new Vector2(
            direction * speed,
            rb.linearVelocity.y
        );
    }

    public void Stop()
    {
        rb.linearVelocity = new Vector2(
            0f,
            rb.linearVelocity.y
        );
    }
}