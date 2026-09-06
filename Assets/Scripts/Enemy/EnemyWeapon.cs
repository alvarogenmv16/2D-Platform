using UnityEngine;

// Represents a very simple weapon/attack point attached to the enemy.
// Detects whether the player is within range at the moment of impact.
public class EnemyWeapon : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float damage = 1f;
    [SerializeField] private float attackRadius = 2f;
    [SerializeField] private LayerMask playerLayer;

    // =========================
    // PROPERTIES
    // =========================

    public Vector2 AttackPointPosition => transform.position;

    // =========================
    // FUNCTIONS
    // =========================

    // Checks for the player inside the attack radius and applies damage
    // if found. Called by EnemyAttack at the moment the attack lands.
    public void TryHitPlayer()
    {
        Collider2D hit = Physics2D.OverlapCircle(AttackPointPosition, attackRadius, playerLayer);

        if (hit == null) return;

        PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, AttackPointPosition);
        }
    }

    // =========================
    // DEBUG
    // =========================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}