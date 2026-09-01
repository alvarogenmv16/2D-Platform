using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float maxHealth = 2f;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private EnemyAttack enemyAttack;

    private float currentHealth;
    private bool isDead = false;
    private Rigidbody2D rb;
    private Collider2D col;

    // =========================
    // START
    // =========================

    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    // =========================
    // FUNCTIONS
    // =========================

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"Enemy took {amount} damage. Current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("Enemy died!");

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Stop chasing, attacking and moving immediately.
        if (enemyAI != null) enemyAI.enabled = false;
        if (enemyMovement != null) enemyMovement.enabled = false;
        if (enemyAttack != null) enemyAttack.enabled = false;

        // Stop physics and disable collision so the corpse doesn't keep
        // blocking the player or reacting to hits while the death
        // animation plays.
        if (rb != null) rb.simulated = false;
        if (col != null) col.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger("DeathTrigger");

            // Wait one frame so the Animator processes the transition
            // before we read the current state's length.
            yield return null;

            float deathAnimationLength = animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(deathAnimationLength);
        }

        Destroy(gameObject);
    }
}