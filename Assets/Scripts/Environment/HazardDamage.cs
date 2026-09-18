using UnityEngine;

// Deals damage to the Player on contact. Works on any Collider2D marked as
// a trigger - a single spike, or a whole Hazards Tilemap covering many
// painted spike tiles at once.
[RequireComponent(typeof(Collider2D))]
public class HazardDamage : MonoBehaviour
{
    [SerializeField] private float damage = 1f;

    private Collider2D hazardCollider;

    private void Awake()
    {
        hazardCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerHealth playerHealth)) return;

        // Approximate hit source as the closest point on the hazard to the
        // player, so knockback (driven by OnDamaged) pushes away from
        // wherever on the hazard shape was actually touched.
        Vector2 hitSourcePosition = hazardCollider.ClosestPoint(other.transform.position);
        bool damageApplied = playerHealth.TakeDamage(damage, hitSourcePosition);

        // Only bump the player back on a hit that actually landed - not on
        // every OnTriggerStay2D frame while they're still invulnerable from
        // the last one.
        if (damageApplied && other.TryGetComponent(out PlayerRespawnPoint respawnPoint))
        {
            SceneFader.FlashAndTeleport(respawnPoint);
        }
    }
}
