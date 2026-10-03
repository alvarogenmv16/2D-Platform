using UnityEngine;

// A single key block for a Level3-style key-gate puzzle: a block that
// breaks in one hit from the player's attack and reports to a KeyManager
// tracking how many have been collected. Deliberately not built on
// EnemyHealth - that component carries knockback, an Animator "DieTrigger"
// state machine and fall-before-death logic meant for combatants, none of
// which applies to a one-shot breakable block.
[RequireComponent(typeof(Collider2D))]
public class KeyBlock : MonoBehaviour
{
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip breakSound;

    private bool isBroken = false;

    // Called by PlayerAttackHitbox when its attack overlap box hits this
    // block's collider - same calling convention as EnemyHealth.TakeDamage.
    public void Break()
    {
        if (isBroken) return;
        isBroken = true;

        GetComponent<Collider2D>().enabled = false;

        // Destroying the GameObject also destroys its AudioSource, cutting
        // the sound off mid-playback - so the destroy delay is derived from
        // breakSound's own length instead of a hand-picked number, and stays
        // correct even if the clip is swapped out later.
        float delay = 0f;

        // Explicit null checks instead of ?. - Unity's overloaded ==/!=
        // on UnityEngine.Object correctly detects an unassigned Inspector
        // reference, but ?. bypasses that overload and throws instead.
        if (sfxPlayer != null)
        {
            sfxPlayer.Play(breakSound);

            if (breakSound != null)
            {
                delay = breakSound.length;
            }
        }

        if (KeyManager.Instance != null)
        {
            KeyManager.Instance.CollectKey();
        }

        Destroy(gameObject, delay);
    }
}
