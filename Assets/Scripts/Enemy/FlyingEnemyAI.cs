using UnityEngine;
using Unity.Cinemachine;
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
    [SerializeField] private float maxDiveDuration = 1.5f; // safety fallback

    [Header("Stuck (player's attack window)")]
    [SerializeField] private float stuckDuration = 1f;

    [Header("Return")]
    [SerializeField] private float returnSpeed = 4f;

    [SerializeField] private EnemyWeapon weapon;
    [SerializeField] private CinemachineImpulseSource impulseSource;    // Optional: for screen shake when hitting the player

    private FlyingEnemyMovement movement;
    private Vector2 originPosition;
    private Vector2 telegraphTargetPosition;
    private Vector2 diveTargetPosition;
    private float stateTimer;
    private bool hasDealtDamageThisDive;
    private Collider2D col; // Add this line to store the Collider2D reference

    // =========================
    // START
    // =========================
    private void Start()
    {
        movement = GetComponent<FlyingEnemyMovement>();
        col = GetComponent<Collider2D>(); // add this line
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
            diveTargetPosition = player.position;
            hasDealtDamageThisDive = false;

            // Stop colliding physically during the dive/stuck window — the
            // enemy needs to reach the player's exact captured position,
            // which a solid collider would otherwise block. Actual damage
            // is still handled by EnemyWeapon's OverlapCircle, independent
            // of physical collision.
            if (col != null) col.isTrigger = true;

            currentState = FlyingEnemyState.Diving;
        }
    }

    private void HandleDiving()
    {
        stateTimer += Time.fixedDeltaTime;

        bool arrived = movement.MoveTowards(diveTargetPosition, diveSpeed);
        bool timedOut = stateTimer >= maxDiveDuration;

        if ((arrived || timedOut) && !hasDealtDamageThisDive)
        {
            if (weapon != null)
            {
                weapon.TryHitPlayer();
            }

            // Screen shake right at the moment of impact, not before or after
            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse();
            }

            hasDealtDamageThisDive = true;
            stateTimer = 0f;
            currentState = FlyingEnemyState.Stuck;
        }
    }

    private void HandleStuck()
    {
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= stuckDuration)
        {
            // Solid again once it starts flying back — it shouldn't pass
            // through the player (or anything else) during normal flight.
            if (col != null) col.isTrigger = false;

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