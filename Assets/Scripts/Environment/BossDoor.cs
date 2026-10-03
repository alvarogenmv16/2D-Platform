using UnityEngine;

// Blocks the path to the boss until every KeyBlock in the level has been
// broken. Open() is wired to a KeyManager's OnAllKeysCollected UnityEvent
// in the Inspector - this script has no reference to KeyManager at all,
// same decoupling as an arena controller listening to EnemyHealth.OnDied.
[RequireComponent(typeof(Collider2D))]
public class BossDoor : MonoBehaviour
{
    [SerializeField] private Animator animator; // optional: plays an "OpenTrigger"
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip openSound;

    private bool isOpen = false;

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        GetComponent<Collider2D>().enabled = false;

        sfxPlayer?.Play(openSound);
        animator?.SetTrigger("OpenTrigger");
    }
}
