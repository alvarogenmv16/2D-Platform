using UnityEngine;
using UnityEngine.Events;

// Tracks progress on a level's key-gate puzzle: a fixed number of KeyBlocks
// scattered through the level each call CollectKey() once broken. Fires
// OnAllKeysCollected exactly once, when the last one comes in - wire a
// BossDoor's Open() to it in the Inspector, the same UnityEvent-wiring
// convention EnemyHealth.OnDied already uses for its listeners.
//
// Exposed as a scene-local singleton (Instance) rather than a serialized
// field on KeyBlock: KeyBlock instances come from a Prefab Asset, and a
// Prefab Asset can never hold a reference to a scene object like this
// level's KeyManager - that reference silently reverts to None the moment
// it's assigned outside the scene. A static Instance sidesteps that instead
// of requiring a manual per-instance override on every placed block.
public class KeyManager : MonoBehaviour
{
    public static KeyManager Instance { get; private set; }

    [SerializeField] private int totalKeys = 3;

    public UnityEvent<int, int> OnKeyCountChanged; // (collected, total) - optional, for a HUD counter
    public UnityEvent OnAllKeysCollected;

    private int collectedKeys = 0;

    // Read by listeners that subscribe late (in their own Start, possibly
    // after this Start already fired the initial OnKeyCountChanged), so
    // they can sync to the current progress instead of waiting for a key.
    public int CollectedKeys => collectedKeys;
    public int TotalKeys => totalKeys;
    public bool AllKeysCollected => collectedKeys >= totalKeys;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        OnKeyCountChanged?.Invoke(collectedKeys, totalKeys);
    }

    public void CollectKey()
    {
        if (collectedKeys >= totalKeys) return;

        collectedKeys++;
        OnKeyCountChanged?.Invoke(collectedKeys, totalKeys);

        if (collectedKeys >= totalKeys)
        {
            OnAllKeysCollected?.Invoke();
        }
    }

    // Undoes one CollectKey - for a key that gets put back in the level
    // (KeyBlock.Restore, e.g. when RisingFlameSea resets its section to
    // "just before the key"), so taking it again doesn't count it twice.
    // Listeners like KeyChalice snuff the matching flame through
    // OnKeyCountChanged. A door already opened by OnAllKeysCollected stays
    // open (BossDoor has no Close) - re-collecting just calls Open() again,
    // which it ignores.
    public void ReturnKey()
    {
        if (collectedKeys <= 0) return;

        collectedKeys--;
        OnKeyCountChanged?.Invoke(collectedKeys, totalKeys);
    }
}
