using UnityEngine;

// A minimal marker for the soul left behind at the player's death position.
// Marks itself to survive scene reloads, so it stays in the world even
// after the player respawns via a scene reset.
public class PlayerSoul : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    // Tracks the currently active soul in the game, if any. Static so it
    // persists across scene reloads just like the soul GameObject itself.
    private static PlayerSoul currentInstance;

    // =========================
    // START
    // =========================

    private void Awake()
    {
        // If a soul already exists from a previous death, destroy it now.
        // Only one soul should ever exist in the world at a time.
        if (currentInstance != null)
        {
            Destroy(currentInstance.gameObject);
        }

        currentInstance = this;

        // Survive the upcoming scene reload so the soul remains exactly
        // where the player died, instead of being destroyed along with
        // everything else in the scene.
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        // Clear the static reference if this instance is the one being
        // destroyed, so a stale reference doesn't linger after pickup
        // logic destroys it later.
        if (currentInstance == this)
        {
            currentInstance = null;
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Intentionally empty for now. Pickup logic (restoring health,
    // currency, etc.) will be added here later.
}