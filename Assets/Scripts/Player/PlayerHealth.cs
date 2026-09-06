using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float invulnerabilityDuration = 0.5f;

    private float currentHealth;

    // Fired whenever health changes, passing (currentHealth, maxHealth).
    // The UI health bar subscribes to this to update itself, without
    // PlayerHealth needing to know the UI exists at all.
    public UnityEvent<float, float> OnHealthChanged;

    // Fired once, exactly when health reaches 0. The future death
    // animation, input-disabling, game over screen, etc. all subscribe
    // to this instead of being hardcoded inside Die().
    public UnityEvent OnDied;

    // Fired whenever damage lands, passing (damageAmount, hitSourcePosition).
    // The hit source position lets listeners (like knockback) figure out
    // which direction to push the player.
    public UnityEvent<float, Vector2> OnDamaged;
    // Fired when the post-hit invulnerability window starts and ends.
    // Used by visual feedback (like the blink effect) to know exactly
    // how long to run, without duplicating invulnerabilityDuration elsewhere.
    public UnityEvent OnInvulnerabilityStart;
    public UnityEvent OnInvulnerabilityEnd;
    private bool isDead = false;
    private bool isInvulnerable = false;

    // =========================
    // START
    // =========================

    private void Start()
    {
        currentHealth = maxHealth;

        // Let any listener (like the UI) initialize with the starting value
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // =========================
    // FUNCTIONS
    // =========================

    public void TakeDamage(float amount, Vector2 hitSourcePosition)
    {
        // Ignore damage while dead or during the brief invulnerability
        // window right after getting hit. Without this, standing inside
        // an enemy's attack range would drain health every single frame.
        if (isDead || isInvulnerable) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"Player took {amount} damage. Current health: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            // Lethal hit: skip the hit reaction entirely and go straight to
            // death. Firing OnDamaged here would start a hit-stun coroutine
            // that races against the death sequence for control of the
            // Rigidbody2D and playerMovement.enabled.
            Die();
        }
        else
        {
            // Start the invulnerability timer so the player can't take
            // damage again immediately.
            OnDamaged?.Invoke(amount, hitSourcePosition);
            StartCoroutine(InvulnerabilityWindow());
        }
    }
    private IEnumerator InvulnerabilityWindow()
    {
        isInvulnerable = true;
        OnInvulnerabilityStart?.Invoke();

        yield return new WaitForSeconds(invulnerabilityDuration);

        isInvulnerable = false;
        OnInvulnerabilityEnd?.Invoke();
    }
    private void Die()
    {
        isDead = true;
        Debug.Log("Player died!");
        OnDied?.Invoke();
    }
}