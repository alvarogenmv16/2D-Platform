using UnityEngine;

// Blocks the path to the boss until every KeyBlock in the level has been
// broken. Subscribes itself to the scene's KeyManager (KeyManager.Instance),
// so a door dropped into the scene from its prefab needs no Inspector
// wiring at all - same reason KeyBlock uses the static Instance: a Prefab
// Asset can't hold a reference to a scene object. Open() is still public,
// so it can also be wired to any other UnityEvent by hand if needed.
[RequireComponent(typeof(Collider2D))]
public class BossDoor : MonoBehaviour
{
    [SerializeField] private Animator animator; // optional: plays an "OpenTrigger"

    [Header("Sprite swap (optional)")]
    // Closed sprite is whatever the SpriteRenderer starts with; on Open() it
    // switches to openSprite. Leave empty if an Animator handles the visuals.
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite openSprite;

    [Header("Key gate")]
    // Off = the door ignores KeyManager and only opens when something calls
    // Open() directly (e.g. a UnityEvent wired in the Inspector).
    [SerializeField] private bool opensWhenAllKeysCollected = true;

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip openSound;

    private bool isOpen = false;

    private void Start()
    {
        if (!opensWhenAllKeysCollected) return;

        if (KeyManager.Instance == null)
        {
            Debug.LogWarning($"{name}: BossDoor found no KeyManager in the scene, it will stay closed.", this);
            return;
        }

        KeyManager.Instance.OnAllKeysCollected.AddListener(Open);

        // Covers a KeyManager that already finished before this Start ran.
        if (KeyManager.Instance.AllKeysCollected)
        {
            Open();
        }
    }

    private void OnDestroy()
    {
        if (KeyManager.Instance != null)
        {
            KeyManager.Instance.OnAllKeysCollected.RemoveListener(Open);
        }
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        GetComponent<Collider2D>().enabled = false;

        if (spriteRenderer != null && openSprite != null)
        {
            spriteRenderer.sprite = openSprite;
        }

        // Explicit null checks instead of ?. - an unassigned Inspector
        // reference is a Unity "fake null" that ?. doesn't catch (see KeyBlock).
        if (sfxPlayer != null) sfxPlayer.Play(openSound);
        if (animator != null) animator.SetTrigger("OpenTrigger");
    }
}
