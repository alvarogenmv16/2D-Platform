using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    private enum EnemyState
    {
        Idle,
        Chasing,
        Attacking
    }

    [SerializeField] private EnemyState currentState = EnemyState.Idle;

    // Reference to the player. Assign in the Inspector, or auto-find by tag
    // in Start as a fallback (see below).
    [SerializeField] private Transform player;

    // Single source of truth for detection ranges. No separate colliders,
    // so the gizmo always matches exactly what the logic uses.
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float attackRange = 3f;

    // Child transform holding the sprite and the weapon pivot. Only THIS
    // gets flipped, never the root (which holds the Rigidbody2D/Collider2D),
    // so flipping never affects physics.
    [SerializeField] private Transform visuals;

    private EnemyMovement movement;
    private EnemyAttack attack;

    // 1 = facing right, -1 = facing left. Persists through Idle so the
    // enemy doesn't snap back to a default facing when the player leaves range.
    private float facingDirection = 1f;
    private Vector3 baseVisualsScale; // Used to flip the visuals without scaling them down to zero or negative.

    // =========================
    // START
    // =========================

    private void Start()
    {
        movement = GetComponent<EnemyMovement>();
        attack = GetComponent<EnemyAttack>();

        if (visuals != null)
        {
            baseVisualsScale = visuals.localScale;
        }
        // Fallback: if no player was assigned in the Inspector, try to find
        // one by tag. Assigning it manually is still preferred and more
        // explicit, but this avoids a null reference in quick prototyping.
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    // =========================
    // FIXED UPDATE
    // =========================

    private void FixedUpdate()
    {
        if (player == null) return;

        UpdateState();
        UpdateFacing();
        HandleStateBehavior();
    }

    // =========================
    // FUNCTIONS
    // =========================

    private void UpdateState()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attacking;
        }
        else if (distanceToPlayer <= detectionRange)
        {
            currentState = EnemyState.Chasing;
        }
        else
        {
            currentState = EnemyState.Idle;
        }
    }

    private void UpdateFacing()
    {
        // Only update facing while we actually know where the player is
        // relative to us (Chasing or Attacking). In Idle, keep the last
        // known facing instead of snapping to a default.
        if (currentState == EnemyState.Idle) return;

        float newFacingDirection = Mathf.Sign(player.position.x - transform.position.x);

        if (newFacingDirection != facingDirection)
        {
            facingDirection = newFacingDirection;

            if (visuals != null)
            {
                // Preserve the original magnitude (e.g. 0.5) captured in Start,
                // only flipping the X sign to mirror the sprite.
                visuals.localScale = new Vector3(
                    Mathf.Abs(baseVisualsScale.x) * facingDirection,
                    baseVisualsScale.y,
                    baseVisualsScale.z
                );
            }
        }
    }

    private void HandleStateBehavior()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                movement.Stop();
                break;

            case EnemyState.Chasing:
                movement.Move(facingDirection);
                break;

            case EnemyState.Attacking:
                movement.Stop();
                attack.Attack();
                break;
        }
    }

    // =========================
    // DEBUG
    // =========================

    private void OnDrawGizmosSelected()
    {
        // Detection range
        if (currentState == EnemyState.Chasing)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.yellow;
        }

        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range
        if (currentState == EnemyState.Attacking)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}