using UnityEngine;

// Safety net for the boss's body itself: BossAI already tries to land beside
// the player instead of on top of them, but if they end up overlapping
// anyway (player walks into the boss, gets pushed together, etc.), this
// deals damage instead of letting contact be free. Same OverlapCircle +
// damage pattern as EnemyWeapon, just continuous instead of animation-triggered.
public class BossContactDamage : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float damage = 1f;
    [SerializeField] private float checkRadius = 0.6f;
    [SerializeField] private LayerMask playerLayer;
    // Re-check interval, not a hit cooldown — PlayerHealth's own invulnerability
    // window already prevents repeat damage; this just avoids querying every
    // single physics step while overlapping.
    [SerializeField] private float checkInterval = 0.2f;

    private float checkTimer;

    // =========================
    // FIXED UPDATE
    // =========================
    private void FixedUpdate()
    {
        checkTimer -= Time.fixedDeltaTime;
        if (checkTimer > 0f) return;
        checkTimer = checkInterval;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, checkRadius, playerLayer);
        if (hit == null) return;

        PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, transform.position);
        }
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}
