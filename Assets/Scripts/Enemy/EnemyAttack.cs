using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private Animator animator;

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

        // Only trigger the animation here. Actual damage is applied later,
        // via an Animation Event on the frame the tongue is fully extended
        // — not instantly when the attack starts.
        if (animator != null)
        {
            animator.SetTrigger("AttackTrigger");
        }

        attackCooldownTimer = attackCooldown;
    }
}