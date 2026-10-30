using UnityEngine;

// Floating skull enemy (Level3). Unlike the relics - which track a single
// axis and then commit to a straight-line attack - the Maw hunts the player
// freely in 2D: it first stops to "notice" the player (Alert pose), then
// chases toward a bite spot just in front of the player and snaps its jaws.
// Between bites it keeps chasing; if the player gets too far away it gives
// up and drifts back to where it started.
[RequireComponent(typeof(FlyingEnemyMovement))]
public class MawAI : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    private enum MawState
    {
        Idle,
        Alert,
        Chasing,
        Attacking,
        Returning
    }

    [SerializeField] private MawState currentState = MawState.Idle;

    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 6f;
    // Bigger than detectionRange on purpose, so the player can't make it
    // flicker between Chasing and Returning by standing right on the edge.
    [SerializeField] private float loseRange = 10f;

    // Child transform holding the sprite and the weapon pivot. Only THIS
    // gets flipped, never the root (which holds the Rigidbody2D/Collider2D)
    // - same convention as EnemyAI/FlyRelicAI.
    [SerializeField] private Transform visuals;
    [SerializeField] private Animator animator;

    [Header("Alert (player noticed)")]
    // How long it holds still in the Alert pose before starting the chase.
    // Keep this matched to MawAlert.anim's total length.
    [SerializeField] private float alertDuration = 0.6f;

    [Header("Chasing")]
    [SerializeField] private float chaseSpeed = 3.5f;
    // The Maw doesn't fly INTO the player - it aims for a spot this far in
    // front of them (on its own side), so the mouth ends up over the player
    // when it bites instead of the skull overlapping them. This only steers
    // the movement - the bite itself starts as soon as the player is inside
    // the weapon's hitbox, wherever the Maw happens to be.
    [SerializeField] private float biteDistance = 1.4f;

    [Header("Attack (bite)")]
    [SerializeField] private EnemyWeapon weapon;
    // Total time the bite animation is allowed to play (idle -> bite ->
    // idle) before going back to chasing. Keep this matched to MawBite.anim's
    // total length - the actual hit is fired separately, via an Animation
    // Event (OnBite), same split as FlyRelicAI's OnBeamFire.
    [SerializeField] private float attackDuration = 0.6f;
    // Minimum time between the end of one bite and the start of the next,
    // so it can't chain-bite the player while they're stuck in hit-stun.
    [SerializeField] private float attackCooldown = 0.8f;

    [Header("Return")]
    [SerializeField] private float returnSpeed = 4f;

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip alertSound; // plays once, entering Alert
    [SerializeField] private AudioClip biteSound; // plays once, on the bite frame
    [SerializeField] private AudioSource hoverAudioSource; // loops while Chasing; Loop on, Play On Awake off

    private FlyingEnemyMovement movement;
    private Vector2 originPosition;
    private float stateTimer;
    private float cooldownTimer;

    // 1 = facing right, -1 = facing left. The sprites face right by default.
    // Frozen while biting, so the bite lands where it was aimed.
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

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.fixedDeltaTime;
        }

        switch (currentState)
        {
            case MawState.Idle: HandleIdle(); break;
            case MawState.Alert: HandleAlert(); break;
            case MawState.Chasing: HandleChasing(); break;
            case MawState.Attacking: HandleAttacking(); break;
            case MawState.Returning: HandleReturning(); break;
        }

        UpdateHoverSound();
    }

    private void UpdateHoverSound()
    {
        if (hoverAudioSource == null) return;

        bool shouldHover = currentState == MawState.Chasing;

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

        if (DistanceToPlayer() <= detectionRange)
        {
            stateTimer = 0f;
            UpdateFacing();

            // Trigger, not bool: Alert is a one-shot that returns to Idle
            // on its own via exit time.
            if (animator != null) animator.SetTrigger("AlertTrigger");
            sfxPlayer?.Play(alertSound);

            currentState = MawState.Alert;
        }
    }

    private void HandleAlert()
    {
        movement.Stop();
        UpdateFacing();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= alertDuration)
        {
            currentState = MawState.Chasing;
        }
    }

    private void HandleChasing()
    {
        if (DistanceToPlayer() > loseRange)
        {
            currentState = MawState.Returning;
            return;
        }

        // Facing first: flipping Visuals moves the WeaponPivot to the other
        // side, so the hitbox check below must use the up-to-date facing.
        UpdateFacing();

        if (weapon != null && weapon.IsPlayerInRange())
        {
            // Player already inside the bite hitbox: hold position instead
            // of drifting toward the bite spot, and bite as soon as the
            // cooldown allows.
            movement.Stop();

            if (cooldownTimer <= 0f)
            {
                StartAttack();
            }
            return;
        }

        Vector2 biteSpot = (Vector2)player.position + new Vector2(-facingDirection * biteDistance, 0f);
        movement.MoveTowards(biteSpot, chaseSpeed);
    }

    private void StartAttack()
    {
        stateTimer = 0f;

        if (animator != null) animator.SetTrigger("BiteTrigger");

        currentState = MawState.Attacking;
    }

    private void HandleAttacking()
    {
        // Never moves or turns while biting - facing is already locked in
        // from the last Chasing frame.
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= attackDuration)
        {
            cooldownTimer = attackCooldown;
            currentState = MawState.Chasing;
        }
    }

    // Called via an Animation Event on MawBite.anim (through MawAttackRelay,
    // since events can only reach components on the same GameObject as the
    // Animator), on the exact frame the bite sprite appears.
    public void OnBite()
    {
        if (currentState != MawState.Attacking) return;

        if (weapon != null)
        {
            weapon.TryHitPlayer();
        }

        sfxPlayer?.Play(biteSound);
    }

    private void HandleReturning()
    {
        bool arrived = movement.MoveTowards(originPosition, returnSpeed);

        if (arrived)
        {
            // Back through Idle (not straight to Chasing), so if the player
            // is still nearby it plays the Alert pose again first.
            currentState = MawState.Idle;
        }
    }

    private float DistanceToPlayer()
    {
        return Vector2.Distance(transform.position, player.position);
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

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
