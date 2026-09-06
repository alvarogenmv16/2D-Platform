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
        rb.linearVelocity = new Vector2(
            direction * moveSpeed,
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