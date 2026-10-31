using Unity.Cinemachine;
using UnityEngine;

// Ground beast enemy (Level3). Same attack philosophy as FlyingEnemyAI -
// notice the player, lock onto their position ONCE, then commit to a very
// fast lunge toward that captured point - but on the ground: the dive
// becomes a horizontal charge along the floor. The lock is still a single
// snapshot (no homing), so the player can dodge by jumping over it or
// moving after the lock. Instead of flying back to an origin it just
// recovers where it stopped and, if the player is still around, aims again.
[RequireComponent(typeof(EnemyMovement))]
public class HoundAI : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    private enum HoundState
    {
        Idle,
        Alert,
        Locking,
        Charging,
        Recovering
    }

    [SerializeField] private HoundState currentState = HoundState.Idle;

    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 7f;
    // Bigger than detectionRange on purpose: once awake it keeps hunting
    // the player a bit further before going back to Idle.
    [SerializeField] private float loseRange = 11f;

    // Child transform holding the sprite and the weapon pivot. Only THIS
    // gets flipped, never the root (which holds the Rigidbody2D/Collider2D)
    // - same convention as EnemyAI/MawAI.
    [SerializeField] private Transform visuals;
    [SerializeField] private Animator animator;

    [Header("Alert (howl)")]
    // How long it holds the howl before starting to aim. Keep this matched
    // to HoundAlert.anim's total length.
    [SerializeField] private float alertDuration = 0.9f;

    [Header("Lock-on (crouched windup)")]
    // Time crouched in the windup pose before the charge. The player's
    // position is captured once, at the END of this window. Keep this
    // matched to the time hound_charge1 appears in HoundCharge.anim.
    [SerializeField] private float lockOnDuration = 0.5f;

    [Header("Charge")]
    [SerializeField] private float chargeSpeed = 18f;
    // How far past the locked point it keeps running - a charge that
    // stopped exactly on the player would look like it braked on purpose.
    [SerializeField] private float chargeOvershoot = 2f;
    [SerializeField] private float maxChargeDuration = 1.5f; // safety fallback
    [SerializeField] private LayerMask groundLayer; // walls/ledges that stop the charge
    [SerializeField] private EnemyWeapon weapon;

    [Header("Recover (player's attack window)")]
    [SerializeField] private float recoverDuration = 1f;

    [Header("Wall impact (puzzle variant)")]
    // When on, charging into a HoundBreakableWall breaks it and kills this
    // hound outright - meant for a variant with huge maxHealth that the
    // player can't realistically kill with the sword. Off = normal hound.
    [SerializeField] private bool diesOnWallImpact = false;
    [SerializeField] private AudioClip wallImpactSound;
    [SerializeField] private CinemachineImpulseSource impulseSource; // optional screen shake on impact

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip alertSound; // plays once, entering Alert (howl)
    [SerializeField] private AudioClip chargeSound; // plays once, when the charge starts

    private EnemyMovement movement;
    private EnemyHealth health;
    private Collider2D col;
    private float stateTimer;
    private float chargeDirection;
    private float chargeTargetX;
    private bool hasDealtDamageThisCharge;

    // 1 = facing right, -1 = facing left. The sprites face right by default.
    private float facingDirection = 1f;
    private Vector3 baseVisualsScale;

    // =========================
    // START
    // =========================
    private void Start()
    {
        movement = GetComponent<EnemyMovement>();
        health = GetComponent<EnemyHealth>();
        col = GetComponent<Collider2D>();

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
            case HoundState.Idle: HandleIdle(); break;
            case HoundState.Alert: HandleAlert(); break;
            case HoundState.Locking: HandleLocking(); break;
            case HoundState.Charging: HandleCharging(); break;
            case HoundState.Recovering: HandleRecovering(); break;
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
            UpdateFacing(player.position.x - transform.position.x);

            // Trigger, not bool: the howl is a one-shot that returns to
            // Idle on its own via exit time.
            if (animator != null) animator.SetTrigger("AlertTrigger");
            sfxPlayer?.Play(alertSound);

            currentState = HoundState.Alert;
        }
    }

    private void HandleAlert()
    {
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= alertDuration)
        {
            StartLocking();
        }
    }

    private void StartLocking()
    {
        stateTimer = 0f;

        // Covers both Locking (crouch frame) and Charging (dash frame) -
        // same idea as FlyingEnemyAI's IsAttacking spanning Diving+Stuck.
        if (animator != null) animator.SetBool("IsCharging", true);

        currentState = HoundState.Locking;
    }

    private void HandleLocking()
    {
        movement.Stop();
        // Keeps turning toward the player while crouched, so it always
        // charges the way it's looking.
        UpdateFacing(player.position.x - transform.position.x);
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= lockOnDuration)
        {
            // Captured ONCE here, never updated during the charge - same
            // reasoning as FlyingEnemyAI: a homing charge would be undodgeable.
            chargeDirection = facingDirection;
            chargeTargetX = player.position.x + chargeDirection * chargeOvershoot;
            hasDealtDamageThisCharge = false;
            stateTimer = 0f;

            sfxPlayer?.Play(chargeSound);

            currentState = HoundState.Charging;
        }
    }

    private void HandleCharging()
    {
        stateTimer += Time.fixedDeltaTime;

        movement.Move(chargeDirection, chargeSpeed);

        // Damage on contact at any point of the charge (not only at the
        // end, like the flyer's dive), and only once per charge - the
        // Player/Enemy layers don't collide, so it runs straight through.
        if (!hasDealtDamageThisCharge && weapon != null && weapon.IsPlayerInRange())
        {
            weapon.TryHitPlayer();
            hasDealtDamageThisCharge = true;
        }

        Collider2D wall = GetWallAhead();

        // Puzzle variant: slamming into a HoundBreakableWall at full charge
        // breaks the wall AND kills the hound. Plain walls still just stop it.
        if (diesOnWallImpact && wall != null && wall.TryGetComponent(out HoundBreakableWall breakableWall))
        {
            CrashIntoWall(breakableWall);
            return;
        }

        bool passedTarget = (transform.position.x - chargeTargetX) * chargeDirection >= 0f;
        bool timedOut = stateTimer >= maxChargeDuration;

        if (passedTarget || timedOut || wall != null || !HasGroundAhead())
        {
            EndCharge();
        }
    }

    private void CrashIntoWall(HoundBreakableWall breakableWall)
    {
        EndCharge();

        breakableWall.Break();
        sfxPlayer?.Play(wallImpactSound);
        impulseSource?.GenerateImpulse();

        // Same death sequence as any other enemy (animation, AI disabled,
        // destroyed after EnemyDeath), just not reachable through damage.
        health?.Kill();
    }

    private void EndCharge()
    {
        movement.Stop();
        stateTimer = 0f;

        if (animator != null) animator.SetBool("IsCharging", false);

        currentState = HoundState.Recovering;
    }

    private void HandleRecovering()
    {
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= recoverDuration)
        {
            // Already awake: aim again straight away (no second howl) while
            // the player is still around, otherwise calm down to Idle.
            if (DistanceToPlayer() <= loseRange)
            {
                StartLocking();
            }
            else
            {
                currentState = HoundState.Idle;
            }
        }
    }

    // The charge stops at walls and at platform edges, so it never runs off
    // a ledge. Both probes look one physics step ahead, since at chargeSpeed
    // it covers a big distance every FixedUpdate.

    // Returns the wall's collider (not just true/false) so the caller can
    // tell a HoundBreakableWall apart from regular level geometry.
    private Collider2D GetWallAhead()
    {
        if (col == null) return null;

        Vector2 front = GetChargeFront(out float lookAhead);
        RaycastHit2D hit = Physics2D.Raycast(front, new Vector2(chargeDirection, 0f), lookAhead, groundLayer);
        return hit.collider;
    }

    private bool HasGroundAhead()
    {
        if (col == null) return true;

        Vector2 front = GetChargeFront(out float lookAhead);
        Vector2 ledgeProbe = new Vector2(front.x + chargeDirection * lookAhead, col.bounds.min.y + 0.1f);
        return Physics2D.Raycast(ledgeProbe, Vector2.down, 0.5f, groundLayer);
    }

    private Vector2 GetChargeFront(out float lookAhead)
    {
        Bounds bounds = col.bounds;
        lookAhead = chargeSpeed * Time.fixedDeltaTime + 0.05f;
        return new Vector2(bounds.center.x + chargeDirection * bounds.extents.x, bounds.center.y);
    }

    private float DistanceToPlayer()
    {
        return Vector2.Distance(transform.position, player.position);
    }

    private void UpdateFacing(float deltaX)
    {
        float newFacingDirection = Mathf.Sign(deltaX);
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

        if (currentState == HoundState.Charging)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, new Vector3(chargeTargetX, transform.position.y, 0f));
        }
    }
}
