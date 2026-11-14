using UnityEngine;

// Floating relic enemy - same chase philosophy as RelicAI, rotated 90
// degrees. RelicAI tracks the player's X at a fixed hover height and then
// drops straight down; this one tracks the player's Y at a fixed hover X
// (so it lines up with the player's height) and then fires a horizontal
// beam. It never moves during the attack itself - only RelicAI's dive did.
[RequireComponent(typeof(FlyingEnemyMovement))]
public class FlyRelicAI : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    private enum FlyRelicState
    {
        Idle,
        Chasing,
        Charging,   // locked in place and aim: the player's window to dodge
        Firing,     // beam out
        Returning
    }

    [SerializeField] private FlyRelicState currentState = FlyRelicState.Idle;

    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 6f;

    // Child transform holding the sprite and the weapon pivot. Only THIS
    // gets flipped, never the root (which holds the Rigidbody2D/Collider2D)
    // - same convention as EnemyAI/FlyingEnemyChase.
    [SerializeField] private Transform visuals;

    [Header("Chasing (vertical tracking, no lock)")]
    [SerializeField] private float chaseSpeed = 5f;
    // How long it keeps re-tracking the player's Y before committing to the
    // attack - re-read every FixedUpdate, so it's never a stale snapshot.
    [SerializeField] private float chaseDuration = 1.2f;

    [Header("Attack (stationary beam)")]
    [SerializeField] private EnemyWeapon weapon;
    [SerializeField] private Animator animator;
    // Once the chase ends the relic freezes - position and aim - and holds
    // its charging pose this long before the beam goes off. This is the
    // player's window to get out of the line.
    [SerializeField] private float chargeDuration = 0.7f;
    // FlyRelicAttack.anim is held on its charging frame from this time
    // until the charge is over, then plays on to the beam frame (whose
    // Animation Event, OnBeamFire, is what actually fires the beam).
    [SerializeField] private float chargeHoldAt = 0.2f;
    // How long the beam stays out and hurts after firing.
    [SerializeField] private float beamDuration = 0.35f;
    // Rest after an attack before it can notice the player again.
    [SerializeField] private float cooldown = 1f;

    [Header("Return")]
    [SerializeField] private float returnSpeed = 6f;

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip awakeSound; // plays once, entering Chasing
    [SerializeField] private AudioClip beamSound; // plays once, entering Attacking
    [SerializeField] private AudioSource hoverAudioSource; // loops while Chasing; Loop on, Play On Awake off

    private FlyingEnemyMovement movement;
    private Vector2 originPosition;
    private float stateTimer;
    private float cooldownTimer;

    private static readonly int AttackState = Animator.StringToHash("FlyRelicAttack");

    // 1 = facing right, -1 = facing left. Frozen once the attack commits,
    // same reasoning as RelicAI freezing X at the start of the dive.
    private float facingDirection = 1f;
    private Vector3 baseVisualsScale;

    // =========================
    // START
    // =========================
    private void Start()
    {
        movement = GetComponent<FlyingEnemyMovement>();
        originPosition = transform.position;

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
            case FlyRelicState.Idle: HandleIdle(); break;
            case FlyRelicState.Chasing: HandleChasing(); break;
            case FlyRelicState.Charging: HandleCharging(); break;
            case FlyRelicState.Firing: HandleFiring(); break;
            case FlyRelicState.Returning: HandleReturning(); break;
        }

        UpdateHoverSound();
    }

    private void UpdateHoverSound()
    {
        if (hoverAudioSource == null) return;

        bool shouldHover = currentState == FlyRelicState.Chasing;

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

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.fixedDeltaTime;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= detectionRange)
        {
            stateTimer = 0f;
            currentState = FlyRelicState.Chasing;
            sfxPlayer?.Play(awakeSound);
        }
    }

    private void HandleChasing()
    {
        stateTimer += Time.fixedDeltaTime;

        UpdateFacing();

        // Re-read the player's Y every frame - it keeps lining up with the
        // player's height for as long as it's chasing, instead of freezing
        // a target once at the start. X stays put at the hover spot.
        Vector2 chaseTarget = new Vector2(originPosition.x, player.position.y);
        movement.MoveTowards(chaseTarget, chaseSpeed);

        if (stateTimer >= chaseDuration)
        {
            StartAttack();
        }
    }

    // The lock: from here on it doesn't move or turn - the beam goes
    // wherever it was aimed at this moment, however the player moves.
    private void StartAttack()
    {
        stateTimer = 0f;
        movement.Stop();

        if (animator != null) animator.SetBool("IsAttacking", true);

        currentState = FlyRelicState.Charging;
    }

    private void HandleCharging()
    {
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        if (animator == null)
        {
            if (stateTimer >= chargeDuration) OnBeamFire();
            return;
        }

        if (stateTimer < chargeDuration)
        {
            // Hold the charging pose.
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == AttackState && state.normalizedTime * state.length >= chargeHoldAt)
            {
                animator.speed = 0f;
            }
        }
        else
        {
            // Release: the clip plays on to the beam frame, whose Animation
            // Event calls OnBeamFire.
            animator.speed = 1f;

            // Safety net if the event never comes.
            if (stateTimer >= chargeDuration + 0.5f) OnBeamFire();
        }
    }

    // Called via an Animation Event on FlyRelicAttack.anim (through
    // FlyRelicAttackRelay, since events can only reach components on the
    // same GameObject as the Animator), on the exact frame the beam sprite
    // appears.
    public void OnBeamFire()
    {
        if (currentState != FlyRelicState.Charging) return;

        if (animator != null) animator.speed = 1f;
        sfxPlayer?.Play(beamSound);

        stateTimer = 0f;
        currentState = FlyRelicState.Firing;
        HandleFiring();
    }

    private void HandleFiring()
    {
        movement.Stop();

        // The beam hurts for as long as it's out, not just on its first
        // frame - but only along the line it was fired on. PlayerHealth's
        // invulnerability keeps this to a single hit.
        if (weapon != null) weapon.TryHitPlayer();

        stateTimer += Time.fixedDeltaTime;
        if (stateTimer >= beamDuration)
        {
            if (animator != null) animator.SetBool("IsAttacking", false);
            cooldownTimer = cooldown;
            currentState = FlyRelicState.Returning;
        }
    }

    // EnemyHealth disables this on hit and on death: never leave the
    // Animator frozen on the charging frame.
    private void OnDisable()
    {
        if (animator != null) animator.speed = 1f;
    }

    private void HandleReturning()
    {
        bool arrived = movement.MoveTowards(originPosition, returnSpeed);

        if (arrived)
        {
            currentState = FlyRelicState.Idle;
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

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
