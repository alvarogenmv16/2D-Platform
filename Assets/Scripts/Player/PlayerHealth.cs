using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    // =========================
    // START
    // =========================

    private void Start()
    {
        currentHealth = maxHealth;
    }

    // =========================
    // FUNCTIONS
    // =========================

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);

        Debug.Log($"Player took {amount} damage. Current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        // Placeholder for now. Later this can trigger a death animation,
        // disable player input, respawn logic, a game over screen, etc.
        Debug.Log("Player died!");
    }
}