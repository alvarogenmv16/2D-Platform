using UnityEngine;

// Falls straight down at a fixed speed (set by whoever spawns it, e.g.
// EyePortal) until it either hits the player or passes the arena floor —
// dodge-only, no destroying it. No Rigidbody2D/trigger collider: same
// OverlapCircle-polling pattern as BossSpikeHazard, just moving instead of
// stationary.
public class EyeRock : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float damage = 1f;
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private LayerMask playerLayer;

    private float fallSpeed;
    private float floorY;
    private bool hasLaunched = false;

    // =========================
    // FIXED UPDATE
    // =========================
    private void FixedUpdate()
    {
        if (!hasLaunched) return;

        transform.position += Vector3.down * fallSpeed * Time.fixedDeltaTime;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, hitRadius, playerLayer);

        if (hit != null)
        {
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage, transform.position);
            }

            Destroy(gameObject);
            return;
        }

        if (transform.position.y <= floorY)
        {
            Destroy(gameObject);
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Called by EyePortal right after instantiating this rock.
    public void Launch(float speed, float targetFloorY)
    {
        fallSpeed = speed;
        floorY = targetFloorY;
        hasLaunched = true;
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
