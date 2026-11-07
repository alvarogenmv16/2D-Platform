using UnityEngine;

// Decorative chalice next to the boss door that shows the key-gate progress:
// one flame lights up for every key collected. Purely visual - it has no
// collider and is NOT an IBreakable, so (unlike the puzzle's ChaliceFlame)
// the player's sword does nothing to it.
//
// Subscribes itself to KeyManager.Instance, same as BossDoor, so the prefab
// works when dropped into a scene without any Inspector wiring.
//
// Setup: each flame is a child GameObject (sprite + flicker Animator),
// dragged into "flames" in the order they should light. Keep its length
// equal to KeyManager's totalKeys - extra keys light nothing, extra flames
// never light.
public class KeyChalice : MonoBehaviour
{
    [SerializeField] private GameObject[] flames;

    [Header("Sound (optional)")]
    // Empty by default: KeyBlock already plays its own break sound.
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip lightSound;

    private int litCount = -1; // -1 = not synced yet, so the first sync is silent

    private void Start()
    {
        if (KeyManager.Instance == null)
        {
            Debug.LogWarning($"{name}: KeyChalice found no KeyManager in the scene.", this);
            SetLitCount(0);
            return;
        }

        KeyManager.Instance.OnKeyCountChanged.AddListener(HandleKeyCountChanged);

        // Read the current count directly: KeyManager.Start may already have
        // fired its initial OnKeyCountChanged before this subscribed.
        SetLitCount(KeyManager.Instance.CollectedKeys);
    }

    private void OnDestroy()
    {
        if (KeyManager.Instance != null)
        {
            KeyManager.Instance.OnKeyCountChanged.RemoveListener(HandleKeyCountChanged);
        }
    }

    private void HandleKeyCountChanged(int collected, int total)
    {
        SetLitCount(collected);
    }

    private void SetLitCount(int count)
    {
        bool isFirstSync = litCount < 0;
        bool gainedFlame = !isFirstSync && count > litCount;

        for (int i = 0; i < flames.Length; i++)
        {
            if (flames[i] != null)
            {
                flames[i].SetActive(i < count);
            }
        }

        litCount = count;

        // Explicit null check instead of ?. (Unity fake-null, see KeyBlock).
        if (gainedFlame && sfxPlayer != null)
        {
            sfxPlayer.Play(lightSound);
        }
    }
}
