using UnityEngine;

public class FlyingEnemyAI : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    private enum FlyingEnemyState
    {
        Idle,
        Telegraph,
        Locking,
        Diving,
        Stuck,
        Returning
    }

    [SerializeField] private FlyingEnemyState currentState = FlyingEnemyState.Idle;

    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 6f;

    [Header("Telegraph (windup)")]
    [SerializeField] private float telegraphHeight = 0.5f;
    [SerializeField] private float telegraphSpeed = 3f;

    [Header("Lock-on")]
    [SerializeField] private float lockOnDuration = 0.4f;

    [Header("Dive")]
    [SerializeField] private float diveSpeed = 14f;

    [Header("Stuck (player's attack window)")]
    [SerializeField] private float stuckDuration = 1f;

    [Header("Return")]
    [SerializeField] private float returnSpeed = 4f;

    [SerializeField] private EnemyWeapon weapon;

    private FlyingEnemyMovement movement;
    private Vector2 originPosition;
    private Vector2 telegraphTargetPosition;
    private Vector2 diveTargetPosition;
    private float stateTimer;
    private bool hasDealtDamageThisDive;

    // =========================
    // START
    // =========================
    private void Start()
    {
        movement = GetComponent<FlyingEnemyMovement>();
        originPosition = transform.position;

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

        switch (currentState)
        {
            case FlyingEnemyState.Idle: HandleIdle(); break;
            case FlyingEnemyState.Telegraph: HandleTelegraph(); break;
            case FlyingEnemyState.Locking: HandleLocking(); break;
            case FlyingEnemyState.Diving: HandleDiving(); break;
            case FlyingEnemyState.Stuck: HandleStuck(); break;
            case FlyingEnemyState.Returning: HandleReturning(); break;
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    private void HandleIdle()
    {
        movement.Stop();

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= detectionRange)
        {
            // Capture the windup destination once, right when the player is spotted
            telegraphTargetPosition = (Vector2)transform.position + Vector2.up * telegraphHeight;
            currentState = FlyingEnemyState.Telegraph;
        }
    }

    private void HandleTelegraph()
    {
        bool arrived = movement.MoveTowards(telegraphTargetPosition, telegraphSpeed);

        if (arrived)
        {
            stateTimer = 0f;
            currentState = FlyingEnemyState.Locking;
        }
    }

    private void HandleLocking()
    {
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= lockOnDuration)
        {
            // Captured ONCE here — intentionally not updated during the
            // dive. The dive relies on high speed, not homing, to be fair.
            diveTargetPosition = player.position;
            hasDealtDamageThisDive = false;
            currentState = FlyingEnemyState.Diving;
        }
    }

    private void HandleDiving()
    {
        bool arrived = movement.MoveTowards(diveTargetPosition, diveSpeed);

        if (arrived && !hasDealtDamageThisDive)
        {
            if (weapon != null)
            {
                weapon.TryHitPlayer();
            }
            hasDealtDamageThisDive = true;

            stateTimer = 0f;
            currentState = FlyingEnemyState.Stuck;
        }
    }

    private void HandleStuck()
    {
        // Just stay put here — this IS the player's attack window.
        // No extra code needed; PlayerAttackHitbox already detects any
        // enemy in the "Enemy" layer via OverlapCircleAll.
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= stuckDuration)
        {
            currentState = FlyingEnemyState.Returning;
        }
    }

    private void HandleReturning()
    {
        bool arrived = movement.MoveTowards(originPosition, returnSpeed);

        if (arrived)
        {
            currentState = FlyingEnemyState.Idle;
        }
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}