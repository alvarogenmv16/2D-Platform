using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private EnemyWeapon weapon;

    private float attackCooldownTimer = 0f;

    // =========================
    // UPDATE / FIXED UPDATE
    // =========================

    private void FixedUpdate()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.fixedDeltaTime;
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    public void Attack()
    {
        if (attackCooldownTimer > 0f)
        {
            return;
        }

        Debug.Log("Enemy attacks!");

        if (weapon != null)
        {
            weapon.TryHitPlayer();
        }

        attackCooldownTimer = attackCooldown;
    }
}