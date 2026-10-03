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
}
