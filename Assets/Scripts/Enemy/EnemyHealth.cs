using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private Animator animator;
    [SerializeField] private MonoBehaviour[] aiComponentsToDisable;

    [Header("Knockback")]
    [SerializeField] private float knockbackForceX = 5f;
    [SerializeField] private float knockbackForceY = 3f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Fall before death (flying enemies)")]
    [SerializeField] private bool fallsBeforeDeath = false;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float fallGravityScale = 3f;

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

        // Flying enemies need to fall to the ground FIRST, so the death
        // animation plays on the floor, not floating mid-air. Ground
        // enemies skip this entirely (fallsBeforeDeath is false for them).
        if (fallsBeforeDeath)
        {
            yield return StartCoroutine(FallToGround());
        }

        if (rb != null) rb.simulated = false;
        if (col != null) col.enabled = false;

        if (animator != null)
        {
            // Clear persistent bool parameters (e.g. IsAttacking) so an Any State
            // transition gated by one of them can't fire again and override EnemyDeath.
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(param.name, false);
                }
            }

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

    private IEnumerator FallToGround()
    {
        if (rb == null) yield break;

        if (col != null) col.isTrigger = false;

        rb.gravityScale = fallGravityScale;

        bool grounded = false;
        float fallSafetyTimer = 0f;
        float maxFallWaitTime = 2f; // safety fallback

        while (!grounded && fallSafetyTimer < maxFallWaitTime)
        {
            grounded = groundCheck != null &&
                    Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            fallSafetyTimer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
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