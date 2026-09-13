using UnityEngine;

// Marks a spot in a scene where the player should land after a scene
// transition. Looked up by "id" (not by GameObject name), so it keeps
// working if the object gets renamed or moved around in the Hierarchy, and
// so a single scene can hold several entry points - one per incoming
// connection - distinguished only by this id. Paired with the LevelExit
// (in whatever scene sends the player here) that carries the matching
// entryPointId.
public class LevelEntryPoint : MonoBehaviour
{
    [SerializeField] private string id = "Default";

    public string Id => id;

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position + Vector3.left * 0.5f, transform.position + Vector3.right * 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.down * 0.5f, transform.position + Vector3.up * 0.5f);
    }
}
