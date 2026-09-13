using UnityEngine;

// Placed at the end of a level's playable area. The moment the player
// reaches it, triggers SceneFader's fade-to-black transition into the next
// scene - the same mechanism EyeAI uses for its Phase 2 jump, just fired by
// reaching a spot instead of a health threshold.
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "SampleScene";
    // Id of the LevelEntryPoint in nextSceneName marking where the player
    // should land. Defaults to "Default" to match LevelEntryPoint's own
    // default id, so a scene with a single entrance needs zero extra setup -
    // only give this (and the entry point) a matching custom id when a scene
    // has more than one incoming connection to tell apart.
    [SerializeField] private string entryPointId = "Default";

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

        SceneFader.FadeToScene(nextSceneName, entryPointId);
    }
}
