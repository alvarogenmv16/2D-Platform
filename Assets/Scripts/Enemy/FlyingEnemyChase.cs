using UnityEngine;

// Minimal flying enemy behavior: flies in a straight line toward the player
// whenever they're within range, and stops otherwise. No telegraph, dive or
// attack pattern - unlike FlyingEnemyAI, which drives the full boss-style
// dive attack. Contact damage and death are handled separately (same
// division of responsibility as the ground Walker: BossContactDamage +
// EnemyHealth), so this only ever needs to worry about movement.
[RequireComponent(typeof(FlyingEnemyMovement))]
public class FlyingEnemyChase : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float moveSpeed = 3f;

    // Child transform holding the sprite. Only THIS gets flipped, never the
    // root (which holds the Rigidbody2D/Collider2D) - same convention as
    // EnemyAI, so flipping never affects physics.
    [SerializeField] private Transform visuals;

    private FlyingEnemyMovement movement;
    private float facingDirection = 1f;
    private Vector3 baseVisualsScale;

    private void Start()
    {
        movement = GetComponent<FlyingEnemyMovement>();

        if (visuals != null)
        {
            baseVisualsScale = visuals.localScale;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            UpdateFacing();
            movement.MoveTowards(player.position, moveSpeed);
        }
        else
        {
            movement.Stop();
        }
    }

    private void UpdateFacing()
    {
        float newFacingDirection = Mathf.Sign(player.position.x - transform.position.x);
        if (newFacingDirection == 0f || newFacingDirection == facingDirection) return;

        facingDirection = newFacingDirection;

        if (visuals != null)
        {
            visuals.localScale = new Vector3(
                Mathf.Abs(baseVisualsScale.x) * facingDirection,
                baseVisualsScale.y,
                baseVisualsScale.z
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
