using UnityEngine;

// Lives on a standalone prefab, instantiated by BossAI's spike attack —
// not part of the boss hierarchy. Its own Animator handles the telegraph
// timing (Telegraph -> Has Exit Time -> Erupt); this script only reacts to
// the Animation Event fired on the erupt frame and cleans itself up.
public class BossSpikeHazard : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private float damage = 1f;
    [SerializeField] private float hitRadius = 0.4f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float lifetimeSeconds = 2f; // safety net; destroys itself even if the Animation Event never fires

    // =========================
    // START
    // =========================
    private void Start()
    {
        Destroy(gameObject, lifetimeSeconds);
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Called via an Animation Event on the Erupt frame of this prefab's own clip.
    public void OnSpikeErupt()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, hitRadius, playerLayer);

        if (hit == null) return;

        PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, transform.position);
        }
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
