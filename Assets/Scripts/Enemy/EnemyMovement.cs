using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float moveSpeed = 3f;

    private Rigidbody2D rb;


    // =========================
    // START
    // =========================

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }


    // =========================
    // UPDATE / FIXED UPDATE
    // =========================

    private void FixedUpdate()
    {
    }


    // =========================
    // FUNCTIONS
    // =========================

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
