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
        if (enemyAI != null) enemyAI.enabled = false;
        if (enemyMovement != null) enemyMovement.enabled = false;
        if (enemyAttack != null) enemyAttack.enabled = false;

        if (rb != null) rb.simulated = false;
        if (col != null) col.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger("DeathTrigger");

            // Actively wait until the Animator has actually entered the
            // EnemyDeath state, instead of assuming one frame is enough.
            // If the enemy dies mid-transition (e.g. right after an attack),
            // it can take more than one frame to resolve — reading the clip
            // length too early was the cause of the enemy vanishing instantly.
            int safetyFrameLimit = 60; // ~1 second at 60fps, avoids an infinite loop if misconfigured
            int framesWaited = 0;

            while (!animator.GetCurrentAnimatorStateInfo(0).IsName("EnemyDeath") && framesWaited < safetyFrameLimit)
            {
                framesWaited++;
                yield return null;
            }

            float deathAnimationLength = animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(deathAnimationLength);
        }

        Destroy(gameObject);
    }
}