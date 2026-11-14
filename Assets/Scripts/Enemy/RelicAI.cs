using UnityEngine;

// Floating relic enemy. Unlike FlyingEnemyAI (which locks onto a single
// captured point and dives diagonally toward it), this one keeps re-tracking
// the player's X for the entire Chasing window - no lock - and then commits
// to a purely vertical slam straight down from wherever it ends up. Contact
// damage on landing reuses EnemyWeapon, same as the other flying enemies.
[RequireComponent(typeof(FlyingEnemyMovement))]
public class RelicAI : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    private enum RelicState
    {
        Idle,
        Chasing,
        Diving,
        Stuck,
        Returning
    }

    [SerializeField] private RelicState currentState = RelicState.Idle;

    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 6f;

    [Header("Chasing (horizontal tracking, no lock)")]
    [SerializeField] private float chaseSpeed = 5f;
    // How long it keeps re-tracking the player's X before committing to the
    // dive - re-read every FixedUpdate, so it's never a stale snapshot the
    // way FlyingEnemyAI's Locking phase is.
    [SerializeField] private float chaseDuration = 1.2f;

    [Header("Dive (vertical only)")]
    [SerializeField] private float diveSpeed = 20f;
    // X is frozen the instant the dive starts; only Y moves. It falls until
    // it actually touches the ground (groundLayer), up to this far.
    [SerializeField] private float fallDistance = 20f;
    // The crystal stays completely still for this long after committing -
    // RelicAttack.anim is already playing (so its warning frames show), but
    // physical movement doesn't start until this elapses. Without this the
    // warning frames flash by mid-fall instead of reading as a held pose.
    [SerializeField] private float anticipationDelay = 0.15f;
    // Safety net only: if no ground is found in this time, it slams anyway.
    [SerializeField] private float diveDuration = 2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Attack animation timing (RelicAttack.anim)")]
    // While falling, the clip holds on its last falling frame (it's frozen
    // once it reaches this time) so it never shows the impact mid-air...
    [SerializeField] private float holdFallFrameAt = 0.3f;
    // ...and jumps to the impact frame the moment the ground is touched -
    // together with the sound, the screen shake and the damage.
    [SerializeField] private float impactFrameTime = 0.35f;

    [Header("Stuck (player's attack window)")]
    [SerializeField] private float stuckDuration = 1f;

    [Header("Return")]
    [SerializeField] private float returnSpeed = 6f;

    [SerializeField] private EnemyWeapon weapon;
    [SerializeField] private Animator animator;

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip awakeSound; // plays once, entering Chasing
    [SerializeField] private AudioClip impactSound; // plays once, entering Stuck (the slam)
    [SerializeField] private AudioSource hoverAudioSource; // loops while Chasing; Loop on, Play On Awake off

    private FlyingEnemyMovement movement;
    private Rigidbody2D rb;
    private Collider2D body;
    private RelicImpactShake impactShake;
    private Vector2 originPosition;
    private Vector2 diveTargetPosition;
    private float stateTimer;
    private bool hasDealtDamageThisDive;
    private ContactFilter2D groundFilter;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

    private static readonly int AttackState = Animator.StringToHash("RelicAttack");

    // =========================
    // START
    // =========================
    private void Start()
    {
        movement = GetComponent<FlyingEnemyMovement>();
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<Collider2D>();
        impactShake = GetComponentInChildren<RelicImpactShake>();
        originPosition = transform.position;

        // Solid ground only - hazard tilemaps are triggers on the Ground layer.
        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(groundLayer);
        groundFilter.useTriggers = false;

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
            case RelicState.Idle: HandleIdle(); break;
            case RelicState.Chasing: HandleChasing(); break;
            case RelicState.Diving: HandleDiving(); break;
            case RelicState.Stuck: HandleStuck(); break;
            case RelicState.Returning: HandleReturning(); break;
        }

        UpdateHoverSound();
    }

    private void UpdateHoverSound()
    {
        if (hoverAudioSource == null) return;

        bool shouldHover = currentState == RelicState.Chasing;

        if (shouldHover && !hoverAudioSource.isPlaying)
        {
            hoverAudioSource.Play();
        }
        else if (!shouldHover && hoverAudioSource.isPlaying)
        {
            hoverAudioSource.Stop();
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
            stateTimer = 0f;
            currentState = RelicState.Chasing;
            sfxPlayer?.Play(awakeSound);
        }
    }

    private void HandleChasing()
    {
        stateTimer += Time.fixedDeltaTime;

        // Re-read the player's X every frame - this is the "no lock"
        // difference from FlyingEnemyAI: it keeps adjusting for as long as
        // it's chasing, instead of freezing a target once at the start.
        Vector2 chaseTarget = new Vector2(player.position.x, originPosition.y);
        movement.MoveTowards(chaseTarget, chaseSpeed);

        if (stateTimer >= chaseDuration)
        {
            StartDive();
        }
    }

    private void StartDive()
    {
        // X is captured here and never touched again during the dive -
        // the slam falls straight down along whatever line it committed to.
        diveTargetPosition = new Vector2(transform.position.x, transform.position.y - fallDistance);
        hasDealtDamageThisDive = false;
        stateTimer = 0f;

        // Covers both Diving and Stuck, same as FlyingEnemyAI's IsAttacking -
        // ends only when Stuck finishes, right before Returning.
        if (animator != null) animator.SetBool("IsAttacking", true);

        currentState = RelicState.Diving;
    }

    private void HandleDiving()
    {
        stateTimer += Time.fixedDeltaTime;
        HoldFallFrameUntilImpact();

        if (stateTimer < anticipationDelay)
        {
            // Otherwise the leftover velocity from Chasing keeps carrying it
            // sideways during the hold, instead of a clean stationary pose.
            movement.Stop();
            return;
        }

        // Is the ground within this physics step's fall? Then land on it now.
        float step = diveSpeed * Time.fixedDeltaTime;
        if (TryFindGround(step + 0.05f, out float distance))
        {
            rb.position += Vector2.down * Mathf.Max(0f, distance - 0.01f);
            Impact();
            return;
        }

        movement.MoveTowards(diveTargetPosition, diveSpeed);

        // Fell all the way (or for too long) without finding ground.
        if (stateTimer >= diveDuration || rb.position.y <= diveTargetPosition.y + 0.01f)
        {
            Impact();
        }
    }

    // Box-casts the relic's own collider straight down.
    private bool TryFindGround(float maxDistance, out float distance)
    {
        distance = 0f;
        if (body == null) return false;

        int count = body.Cast(Vector2.down, groundFilter, groundHits, maxDistance);
        if (count == 0) return false;

        distance = groundHits[0].distance;
        for (int i = 1; i < count; i++) distance = Mathf.Min(distance, groundHits[i].distance);
        return true;
    }

    // The slam: impact frame, sound, screen shake and damage, all on the
    // same physics step the relic touches the ground.
    private void Impact()
    {
        movement.Stop();

        if (animator != null)
        {
            animator.speed = 1f;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == AttackState && state.length > 0f)
            {
                animator.Play(AttackState, 0, Mathf.Clamp01(impactFrameTime / state.length));
            }
        }

        if (!hasDealtDamageThisDive && weapon != null) weapon.TryHitPlayer();
        hasDealtDamageThisDive = true;

        sfxPlayer?.Play(impactSound);
        if (impactShake != null) impactShake.OnImpactShake();

        stateTimer = 0f;
        currentState = RelicState.Stuck;
    }

    // RelicAttack.anim runs anticipation -> falling frames -> impact. Freeze
    // it on the last falling frame while still in the air, so the impact
    // frame only ever appears when Impact() jumps to it.
    private void HoldFallFrameUntilImpact()
    {
        if (animator == null) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash != AttackState) return;

        if (state.normalizedTime * state.length >= holdFallFrameAt) animator.speed = 0f;
    }

    private void HandleStuck()
    {
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= stuckDuration)
        {
            if (animator != null) animator.SetBool("IsAttacking", false);
            currentState = RelicState.Returning;
        }
    }

    // EnemyHealth disables this on death: never leave the Animator frozen,
    // or the death animation wouldn't play.
    private void OnDisable()
    {
        if (animator != null) animator.speed = 1f;
    }

    private void HandleReturning()
    {
        bool arrived = movement.MoveTowards(originPosition, returnSpeed);

        if (arrived)
        {
            currentState = RelicState.Idle;
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
