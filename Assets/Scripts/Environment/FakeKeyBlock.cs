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

    private bool isBroken = false;

    // Called by PlayerAttackHitbox when its attack overlap box hits this
    // block's collider - same calling convention as KeyBlock.Break.
    public void Break()
    {
        if (isBroken) return;
        isBroken = true;

        GetComponent<Collider2D>().enabled = false;

        // The actual damage lands when the sound finishes (the "explosion"
        // moment), not the instant the block is hit - this doubles as the
        // fuse delay before the block disappears, same length as the sound.
        float delay = 0f;

        if (sfxPlayer != null)
        {
            sfxPlayer.Play(explosionSound);

            if (explosionSound != null)
            {
                delay = explosionSound.length;
            }
        }

        StartCoroutine(ExplodeAfterDelay(delay));
    }

    private IEnumerator ExplodeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

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

        Destroy(gameObject);
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
