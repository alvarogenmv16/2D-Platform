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
    [SerializeField] private float attackRange = 1.5f;

    private EnemyMovement movement;

    // =========================
    // START
    // =========================

    private void Start()
    {
        movement = GetComponent<EnemyMovement>();

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

    private void HandleStateBehavior()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                movement.Stop();
                break;

            case EnemyState.Chasing:
                // Move toward the player: +1 if the player is to the right, -1 if to the left
                float direction = Mathf.Sign(player.position.x - transform.position.x);
                movement.Move(direction);
                break;

            case EnemyState.Attacking:
                // Stand still for now. Actual attack logic (cooldown + damage)
                // comes in the next phase.
                movement.Stop();
                break;
        }
    }

    // =========================
    // DEBUG
    // =========================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}