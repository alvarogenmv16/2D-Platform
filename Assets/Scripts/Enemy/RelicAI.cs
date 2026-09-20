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
    // X is frozen the instant the dive starts; only Y moves. Aimed well past
    // where it will actually stop - diveDuration below is what ends the
    // fall, not distance, so it never gets there.
    [SerializeField] private float fallDistance = 20f;
    // The crystal stays completely still for this long after committing -
    // RelicAttack.anim is already playing (so its warning frames show), but
    // physical movement doesn't start until this elapses. Without this the
    // warning frames flash by mid-fall instead of reading as a held pose.
    [SerializeField] private float anticipationDelay = 0.15f;
    // Time from the start of the dive (anticipation included) to impact
    // (damage + screen shake). Keep this matched to RelicAttack.anim's last
    // keyframe - it's what makes the hit land exactly when the impact
    // sprite appears. Must be greater than anticipationDelay, or there's no
    // visible fall left between the hold and the impact.
    [SerializeField] private float diveDuration = 0.35f;

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
    private Vector2 originPosition;
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

        if (stateTimer >= anticipationDelay)
        {
            movement.MoveTowards(diveTargetPosition, diveSpeed);
        }
        else
        {
            // Otherwise the leftover velocity from Chasing keeps carrying it
            // sideways during the hold, instead of a clean stationary pose.
            movement.Stop();
        }

        if (stateTimer >= diveDuration && !hasDealtDamageThisDive)
        {
            if (weapon != null)
            {
                weapon.TryHitPlayer();
            }

            hasDealtDamageThisDive = true;
            stateTimer = 0f;
            currentState = RelicState.Stuck;
            sfxPlayer?.Play(impactSound);
        }
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
