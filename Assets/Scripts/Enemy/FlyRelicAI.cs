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
        Attacking,
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
    // Total time the attack animation is allowed to hold (charge + beam +
    // a beat of the beam lingering) before it flips back to Idle. Keep this
    // matched to FlyRelicAttack.anim's total length - the actual hit is
    // fired separately, via an Animation Event (OnBeamFire), not by this
    // timer, so only the RETURN timing needs to track the clip here.
    [SerializeField] private float attackDuration = 0.6f;

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
            case FlyRelicState.Attacking: HandleAttacking(); break;
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

    private void StartAttack()
    {
        stateTimer = 0f;

        if (animator != null) animator.SetBool("IsAttacking", true);

        currentState = FlyRelicState.Attacking;
    }

    private void HandleAttacking()
    {
        // Never moves while attacking - facing is already locked in from
        // the last Chasing frame, so the beam fires wherever it was aimed.
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= attackDuration)
        {
            if (animator != null) animator.SetBool("IsAttacking", false);
            currentState = FlyRelicState.Returning;
        }
    }

    // Called via an Animation Event on FlyRelicAttack.anim (through
    // FlyRelicAttackRelay, since events can only reach components on the
    // same GameObject as the Animator), on the exact frame the beam sprite
    // appears - same reasoning as RelicAI's OnAttackImpact.
    public void OnBeamFire()
    {
        if (currentState != FlyRelicState.Attacking) return;

        if (weapon != null)
        {
            weapon.TryHitPlayer();
        }

        sfxPlayer?.Play(beamSound);
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
