using System.Collections;
using UnityEngine;

// A trap variant of KeyBlock: looks the same to the player but punishes
// the swing instead of rewarding it - breaks the same way, then damages
// anything on playerLayer within explosionRadius. Never touches KeyManager,
// so breaking one doesn't count towards the level's key total.
[RequireComponent(typeof(Collider2D))]
public class FakeKeyBlock : MonoBehaviour, IBreakable
{
    [Header("Explosion")]
    [SerializeField] private float explosionDamage = 1f;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip explosionSound;
    // Second of explosionSound where the actual boom starts - the clip is
    // alarm beeps first, then the blast, so the damage/FX must land HERE,
    // not at the end of the clip. 0 = use the clip's full length.
    // fake_key.mp3: 5 beeps up to ~1.4s, blast onset at 1.47s.
    [SerializeField] private float explosionMoment = 1.47f;

    [Header("Visual warning (optional)")]
    [SerializeField] private FakeKeyBlockTelegraph telegraph;

    private bool isBroken = false;
    private float soundRemainingAfterBlast;

    // Called by PlayerAttackHitbox when its attack overlap box hits this
    // block's collider - same calling convention as KeyBlock.Break.
    public void Break()
    {
        if (isBroken) return;
        isBroken = true;

        GetComponent<Collider2D>().enabled = false;

        // The actual damage lands on the blast inside the sound (after the
        // alarm), not the instant the block is hit - the alarm part doubles
        // as the fuse delay the player has to get away.
        float delay = 0f;

        if (sfxPlayer != null)
        {
            sfxPlayer.Play(explosionSound);

            if (explosionSound != null)
            {
                bool hasMoment = explosionMoment > 0f && explosionMoment < explosionSound.length;
                delay = hasMoment ? explosionMoment : explosionSound.length;
                soundRemainingAfterBlast = explosionSound.length - delay;
            }
        }

        // Shows the blast radius and the time left for the whole fuse.
        telegraph?.BeginTelegraph(explosionRadius, delay);

        StartCoroutine(ExplodeAfterDelay(delay));
    }

    private IEnumerator ExplodeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Before the overlap check: it also snaps the block back from the
        // fuse shake, so the blast is measured from its real position.
        float fxDuration = telegraph != null ? telegraph.PlayExplosion() : 0f;

        // Checked here, not at the moment of the hit, so a player who
        // retreats during the fuse delay can actually dodge the blast.
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, playerLayer);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(explosionDamage, transform.position);
            }
        }

        // Kept alive (already hidden by the telegraph) until both the
        // explosion FX and the blast's sound tail finish - the FX run on
        // this block's coroutines and the AudioSource lives on it too, so
        // destroying it earlier would cut the explosion sound off.
        Destroy(gameObject, Mathf.Max(fxDuration, soundRemainingAfterBlast));
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
