using UnityEngine;

// Safety net for the boss's body itself: BossAI already tries to land beside
// the player instead of on top of them, but if they end up overlapping
// anyway (player walks into the boss, gets pushed together, etc.), this
// deals damage instead of letting contact be free. Same OverlapBox +
// damage pattern as EnemyWeapon, just continuous instead of animation-triggered.
public class BossContactDamage : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float damage = 1f;
    // Small and offset downward on purpose — this represents the ground
    // under the boss's feet, not its whole body, so standing near it (but
    // not directly under it) doesn't count as contact.
    [SerializeField] private Vector2 checkSize = new Vector2(1f, 0.4f);
    [SerializeField] private float checkOffsetY = -0.5f;
    [SerializeField] private LayerMask playerLayer;
    // Re-check interval, not a hit cooldown — PlayerHealth's own invulnerability
    // window already prevents repeat damage; this just avoids querying every
    // single physics step while overlapping.
    [SerializeField] private float checkInterval = 1.5f;

    private float checkTimer;

    // =========================
    // FIXED UPDATE
    // =========================
    private void FixedUpdate()
    {
        checkTimer -= Time.fixedDeltaTime;
        if (checkTimer > 0f) return;
        checkTimer = checkInterval;

        Vector2 checkPosition = (Vector2)transform.position + new Vector2(0f, checkOffsetY);
        Collider2D hit = Physics2D.OverlapBox(checkPosition, checkSize, 0f, playerLayer);
        if (hit == null) return;

        PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, checkPosition);
        }
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 checkPosition = (Vector2)transform.position + new Vector2(0f, checkOffsetY);
        Gizmos.DrawWireCube(checkPosition, checkSize);
    }
}
