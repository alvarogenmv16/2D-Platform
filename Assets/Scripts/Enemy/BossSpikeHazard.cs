using UnityEngine;

// Lives on a standalone prefab, instantiated by BossAI's spike attack —
// not part of the boss hierarchy. Its own Animator handles the telegraph
// timing (Telegraph -> Has Exit Time -> Erupt -> retract); this script
// reacts to the Animation Events for sound and damages the player
// continuously for as long as it's actually up (erupted through retracting),
// not just on the single erupt frame.
public class BossSpikeHazard : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float damage = 1f;
    [SerializeField] private float hitRadius = 0.4f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float lifetimeSeconds = 2f; // safety net; destroys itself even if the Animation Event never fires

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip startSound; // plays on spawn, telegraph begins
    [SerializeField] private AudioClip emergeSound; // plays on the erupt frame
    [SerializeField] private AudioClip retractSound; // plays as it starts sinking back down

    // True from the erupt frame until this object is destroyed — covers
    // erupting, sitting fully up, and retracting, all as one continuous
    // damage window instead of a single instant.
    private bool isDangerous = false;

    // =========================
    // START
    // =========================
    private void Start()
    {
        sfxPlayer?.Play(startSound);
        Destroy(gameObject, lifetimeSeconds);
    }

    // =========================
    // FIXED UPDATE
    // =========================
    private void FixedUpdate()
    {
        if (!isDangerous) return;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, hitRadius, playerLayer);

        if (hit == null) return;

        PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            // PlayerHealth's own invulnerability window (not a timer here)
            // is what stops this from re-hitting every single physics step —
            // it just lands again the instant that window expires, as long
            // as the player is still standing in it.
            playerHealth.TakeDamage(damage, transform.position);
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Called via an Animation Event on the Erupt frame of this prefab's own clip.
    public void OnSpikeErupt()
    {
        sfxPlayer?.Play(emergeSound);
        isDangerous = true;
    }

    // Called via an Animation Event on the first retract keyframe of this prefab's own clip.
    public void OnSpikeRetract()
    {
        sfxPlayer?.Play(retractSound);
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
