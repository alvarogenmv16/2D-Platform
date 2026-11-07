using UnityEngine;

// Decorative chalice next to the boss door that shows the key-gate progress:
// its flame lights up once the key with this chalice's keyNumber has been
// collected. Place one per key (e.g. 4 chalices for 4 keys) and number them
// 1, 2, 3, 4 - the first key broken lights chalice 1, the second chalice 2,
// and so on, whichever KeyBlock in the level it was.
//
// Purely visual - no collider and NOT an IBreakable, so (unlike the puzzle's
// ChaliceFlame) the player's sword does nothing to it. Subscribes itself to
// KeyManager.Instance, same as BossDoor, so the prefab works when dropped
// into a scene without any Inspector wiring besides keyNumber.
public class KeyChalice : MonoBehaviour
{
    // 1-based: lit while KeyManager's collected count is >= this.
    [SerializeField, Min(1)] private int keyNumber = 1;
    [SerializeField] private GameObject flame;

    [Header("Sound (optional)")]
    // Empty by default: KeyBlock already plays its own break sound.
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip lightSound;

    private bool isLit;
    private bool hasSynced;

    private void Start()
    {
        if (KeyManager.Instance == null)
        {
            Debug.LogWarning($"{name}: KeyChalice found no KeyManager in the scene.", this);
            SetLit(false);
            return;
        }

        if (keyNumber > KeyManager.Instance.TotalKeys)
        {
            Debug.LogWarning($"{name}: keyNumber {keyNumber} is higher than KeyManager's totalKeys ({KeyManager.Instance.TotalKeys}), it will never light.", this);
        }

        KeyManager.Instance.OnKeyCountChanged.AddListener(HandleKeyCountChanged);

        // Read the current count directly: KeyManager.Start may already have
        // fired its initial OnKeyCountChanged before this subscribed.
        SetLit(KeyManager.Instance.CollectedKeys >= keyNumber);
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
        SetLit(collected >= keyNumber);
    }

    private void SetLit(bool lit)
    {
        // First sync is silent: a chalice that starts lit shouldn't play the sound.
        bool justLit = hasSynced && lit && !isLit;

        isLit = lit;
        hasSynced = true;

        if (flame != null)
        {
            flame.SetActive(lit);
        }

        // Explicit null check instead of ?. (Unity fake-null, see KeyBlock).
        if (justLit && sfxPlayer != null)
        {
            sfxPlayer.Play(lightSound);
        }
    }
}
