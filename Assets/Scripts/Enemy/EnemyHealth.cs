using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float maxHealth = 3f;

    private float currentHealth;
    private bool isDead = false;

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

        // Placeholder for now. Later this can trigger a death animation,
        // drop loot, play a sound, etc. before actually removing the enemy.
        Destroy(gameObject);
    }
}