using UnityEngine;

// Represents a very simple weapon/attack point attached to the enemy.
// Detects whether the player is within range at the moment of impact.
public class EnemyWeapon : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float damage = 1f;
    [SerializeField] private Vector2 attackSize = new Vector2(2.5f, 1f);
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
        Collider2D hit = FindPlayerInRange();

        if (hit == null) return;

        PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, AttackPointPosition);
        }
    }

    // Same area TryHitPlayer checks, without applying damage - lets an AI
    // decide to START an attack exactly when the player is inside the hitbox.
    public bool IsPlayerInRange()
    {
        return FindPlayerInRange() != null;
    }

    private Collider2D FindPlayerInRange()
    {
        return Physics2D.OverlapBox(AttackPointPosition, attackSize, 0f, playerLayer);
    }

    // =========================
    // DEBUG
    // =========================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position, attackSize);
    }
}