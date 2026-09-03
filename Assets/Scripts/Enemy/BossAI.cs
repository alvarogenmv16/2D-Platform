using System.Collections;
using UnityEngine;

// Main boss state machine. Sits Dormant until BossArenaController calls
// Activate() (entry trigger). Flies freely around the arena, but always
// descends to the floor before attacking — never attacks mid-air. Attack
// choice (spikes vs scythe) is random each cycle. Death is handled entirely
// by EnemyHealth (this script is one of its aiComponentsToDisable), reusing
// its existing "EnemyDeath" state + fallsBeforeDeath support as-is.
[RequireComponent(typeof(FlyingEnemyMovement))]
public class BossAI : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    private enum BossState
    {
        Dormant,
        Summoning,
        Flying,
        Landing,
        Attacking
    }

    [SerializeField] private BossState currentState = BossState.Dormant;

    [SerializeField] private Transform player;
    [SerializeField] private Transform visuals;
    [SerializeField] private Animator animator;
    [SerializeField] private BossArenaController arena;
    [SerializeField] private GameObject spikeHazardPrefab;

    [Header("Flight")]
    [SerializeField] private float flySpeed = 8f;
    [SerializeField] private float flightHeightAboveFloor = 6f;

    [Header("Landing")]
    [SerializeField] private float landSpeed = 5f;
    // Optional: if assigned, landing stops as soon as this touches the Ground
    // layer, instead of relying on reaching arena.FloorY exactly. Removes the
    // need to hand-calibrate FloorY to the pixel — the boss just descends
    // until it actually finds solid ground, same idea as EnemyHealth.FallToGround.
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Attacks")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float spikeSpacing = 3f;

    private FlyingEnemyMovement movement;
    private Vector2 flightTarget;
    private Vector2 landingTarget;

    // 1 = facing right, -1 = facing left.
    private float facingDirection = 1f;
    private Vector3 baseVisualsScale;

    // =========================
    // START
    // =========================
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

    // =========================
    // FIXED UPDATE
    // =========================
    private void FixedUpdate()
    {
        if (player == null) return;

        switch (currentState)
        {
            case BossState.Flying: HandleFlying(); break;
            case BossState.Landing: HandleLanding(); break;
            // Dormant/Summoning/Attacking are driven by coroutines, nothing per-frame here.
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Called by BossArenaController when the player enters the boss area.
    public void Activate()
    {
        if (currentState != BossState.Dormant) return;

        currentState = BossState.Summoning;
        StartCoroutine(SummonSequence());
    }

    private IEnumerator SummonSequence()
    {
        if (animator != null)
        {
            animator.SetTrigger("SummonTrigger");
            yield return WaitForAnimatorState("BossSummon");
        }

        EnterFlying();
    }

    private void EnterFlying()
    {
        flightTarget = PickRandomFlightPoint();
        currentState = BossState.Flying;
    }

    private void HandleFlying()
    {
        bool arrived = movement.MoveTowards(flightTarget, flySpeed);

        if (arrived)
        {
            EnterLanding();
        }
    }

    private void EnterLanding()
    {
        float targetX = arena != null
            ? Mathf.Clamp(player.position.x, arena.LeftBoundX, arena.RightBoundX)
            : player.position.x;

        // Aim comfortably below the floor estimate — with groundCheck assigned,
        // HandleLanding stops the instant it detects solid ground, so this exact
        // Y never actually needs to be reached, only descended towards.
        // Without groundCheck, it falls back to the old exact-Y behavior.
        float floorEstimate = arena != null ? arena.FloorY : transform.position.y;
        float targetY = groundCheck != null ? floorEstimate - 5f : floorEstimate;

        // Captured once on entry, not continuously homing — same reasoning
        // as FlyingEnemyAI's dive target: high enough speed makes it read
        // as a deliberate landing, not a slow tracking shot.
        landingTarget = new Vector2(targetX, targetY);
        currentState = BossState.Landing;
    }

    private void HandleLanding()
    {
        UpdateFacing();

        if (groundCheck != null)
        {
            bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (grounded)
            {
                movement.Stop();
                currentState = BossState.Attacking;
                StartCoroutine(AttackSequence());
                return;
            }

            movement.MoveTowards(landingTarget, landSpeed);
            return;
        }

        bool arrived = movement.MoveTowards(landingTarget, landSpeed);

        if (arrived)
        {
            currentState = BossState.Attacking;
            StartCoroutine(AttackSequence());
        }
    }

    private IEnumerator AttackSequence()
    {
        UpdateFacing();

        bool useSpikeAttack = Random.Range(0, 2) == 0;
        string trigger = useSpikeAttack ? "SpikeAttackTrigger" : "ScytheAttackTrigger";
        string stateName = useSpikeAttack ? "BossSpikeAttack" : "BossScytheAttack";

        if (animator != null)
        {
            animator.SetTrigger(trigger);
            yield return WaitForAnimatorState(stateName);
        }

        yield return new WaitForSeconds(attackCooldown);

        EnterFlying();
    }

    // Called via BossAttackRelay on the release frame of the spike attack clip.
    public void SpawnSpikeRow()
    {
        if (spikeHazardPrefab == null || arena == null) return;

        float width = arena.RightBoundX - arena.LeftBoundX;
        int count = Mathf.Max(2, Mathf.RoundToInt(width / spikeSpacing));

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (float)(count - 1);
            float x = Mathf.Lerp(arena.LeftBoundX, arena.RightBoundX, t);
            Instantiate(spikeHazardPrefab, new Vector2(x, arena.FloorY), Quaternion.identity);
        }
    }

    private Vector2 PickRandomFlightPoint()
    {
        if (arena == null) return transform.position;

        float x = Random.Range(arena.LeftBoundX, arena.RightBoundX);
        float y = arena.FloorY + flightHeightAboveFloor;
        return new Vector2(x, y);
    }

    private void UpdateFacing()
    {
        if (player == null) return;

        float newFacingDirection = Mathf.Sign(player.position.x - transform.position.x);

        if (newFacingDirection != 0f && newFacingDirection != facingDirection)
        {
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
    }

    // Same "wait for the animator to actually reach this state, then wait its
    // length" pattern already used by EnemyHealth.DeathSequence, so attack/summon
    // timing always matches the real clip length instead of a hardcoded duration.
    private IEnumerator WaitForAnimatorState(string stateName)
    {
        int safetyFrameLimit = 90;
        int framesWaited = 0;

        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateName) && framesWaited < safetyFrameLimit)
        {
            framesWaited++;
            yield return null;
        }

        float stateLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(stateLength);
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
