using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private EnemyAI enemyAI;
    [SerializeField] private EnemyAttack enemyAttack;

    [Header("Knockback")]
    [SerializeField] private float knockbackForceX = 5f;
    [SerializeField] private float knockbackForceY = 3f;
    [SerializeField] private float knockbackDuration = 0.2f;

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

    public void TakeDamage(float amount, Vector2 hitSourcePosition)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"Enemy took {amount} damage. Current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            // Lethal hit: skip knockback entirely and go straight to
            // death, same reasoning as the player's TakeDamage — avoids
            // a knockback coroutine racing against the death sequence
            // for control of the Rigidbody2D and AI components.
            Die();
        }
        else
        {
            StartCoroutine(KnockbackRoutine(hitSourcePosition));
        }
    }

    private IEnumerator KnockbackRoutine(Vector2 hitSourcePosition)
    {
        float direction = Mathf.Sign(transform.position.x - hitSourcePosition.x);
        if (direction == 0f) direction = -1f;

        // Disable AI/attack briefly so their FixedUpdate calls don't
        // immediately overwrite the knockback velocity we're about to set.
        if (enemyAI != null) enemyAI.enabled = false;
        if (enemyAttack != null) enemyAttack.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * knockbackForceX, knockbackForceY);
        }

        yield return new WaitForSeconds(knockbackDuration);

        // Don't re-enable if the enemy died during the knockback stun —
        // DeathSequence() has already taken ownership of these components.
        if (!isDead)
        {
            if (enemyAI != null) enemyAI.enabled = true;
            if (enemyAttack != null) enemyAttack.enabled = true;
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

            int safetyFrameLimit = 60;
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