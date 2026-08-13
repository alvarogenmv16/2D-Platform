using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================
    [SerializeField] private float attackCooldown = 1f;

    private float attackCooldownTimer = 0f;
    // =========================
    // START
    // =========================

    private void Start()
    {
    }

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

        attackCooldownTimer = attackCooldown;
    }
}