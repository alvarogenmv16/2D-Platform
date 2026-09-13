using UnityEngine;

// Placed at the end of a level's playable area. The moment the player
// reaches it, triggers SceneFader's fade-to-black transition into the next
// scene - the same mechanism EyeAI uses for its Phase 2 jump, just fired by
// reaching a spot instead of a health threshold.
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "SampleScene";
    // Name of an empty GameObject placed in nextSceneName marking where the
    // player should land. Looked up by name (same pattern SceneFader already
    // uses for the health UI) since the incoming player isn't part of that
    // scene's saved data.
    [SerializeField] private string entryPointName = "PlayerEntryPoint";

    private Collider2D triggerCollider;
    private bool hasTriggered = false;

    private void Start()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;
        // One-shot: never fire again, same convention as the boss/eye arena triggers.
        triggerCollider.enabled = false;

        SceneFader.FadeToScene(nextSceneName, entryPointName);
    }
}
