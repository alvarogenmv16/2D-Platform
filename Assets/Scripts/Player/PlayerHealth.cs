using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float maxHealth = 5f;

    private float currentHealth;

    // Fired whenever health changes, passing (currentHealth, maxHealth).
    // The UI health bar subscribes to this to update itself, without
    // PlayerHealth needing to know the UI exists at all.
    public UnityEvent<float, float> OnHealthChanged;

    // Fired once, exactly when health reaches 0. The future death
    // animation, input-disabling, game over screen, etc. all subscribe
    // to this instead of being hardcoded inside Die().
    public UnityEvent OnDied;

    private bool isDead = false;

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

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"Player took {amount} damage. Current health: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        Debug.Log("Player died!");

        OnDied?.Invoke();
    }
}