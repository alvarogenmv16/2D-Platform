using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private Animator animator;

    // Any behaviour that drives this enemy's AI/movement/attacks.
    // Melee enemy: EnemyAI, EnemyMovement, EnemyAttack.
    // Flying enemy: FlyingEnemyAI, FlyingEnemyMovement.
    // Generalized to MonoBehaviour so this single script works for any
    // enemy type without needing a separate EnemyHealth variant each time.
    [SerializeField] private MonoBehaviour[] aiComponentsToDisable;

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

        SetAiComponentsEnabled(false);

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * knockbackForceX, knockbackForceY);
        }

        yield return new WaitForSeconds(knockbackDuration);

        if (!isDead)
        {
            SetAiComponentsEnabled(true);
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
        SetAiComponentsEnabled(false);

        if (rb != null) rb.simulated = false;
        if (col != null) col.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger("DieTrigger");

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

    private void SetAiComponentsEnabled(bool enabled)
    {
        foreach (MonoBehaviour component in aiComponentsToDisable)
        {
            if (component != null)
            {
                component.enabled = enabled;
            }
        }
    }
}