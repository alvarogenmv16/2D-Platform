using UnityEngine;

// Final boss (Morthos, TempleScene). Asleep until the player comes close:
// lying still on its first frame, no animation, no roar. Once the player
// is within awakeRange it wakes up - one full idle cycle first, so the
// wake-up roar (BeastRoar, on the idle clip) plays before anything else -
// and from then on it never loses the player again, whatever the distance.
//
// Awake, it slowly walks after the player along the ground (walk pose =
// beast_1, the middle idle frame), and stops to bite when they're in reach.
//
// Attack 1 - bite, same idea as MawAI: the bite starts the moment the
// player is inside the mouth's hitbox (EnemyWeapon.IsPlayerInRange, the
// same box TryHitPlayer checks), and the damage itself lands on the frame
// the jaws snap shut, through an Animation Event (OnBite via
// BeastAttackRelay).
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyMovement))]
public class BeastAI : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    private enum BeastState
    {
        Dormant,
        WakingUp,
        Chasing,
        Biting
    }

    [SerializeField] private BeastState currentState = BeastState.Dormant;

    [SerializeField] private Transform player;
    [SerializeField] private float awakeRange = 14f;

    // Child transform holding the sprite and the weapon pivot. Only THIS
    // gets flipped, never the root (which holds the Rigidbody2D/Collider2D)
    // - same convention as the other enemies. Sprites face right.
    [SerializeField] private Transform visuals;
    [SerializeField] private Animator animator;

    [Header("Wake up")]
    // Time standing in place (idle + roar) before the chase starts. Keep it
    // matched to BeastIdle.anim's length so exactly one roar cycle plays.
    [SerializeField] private float wakeUpDuration = 1.05f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 2.5f;
    // Horizontal dead zone: with the player (almost) straight above or below
    // it doesn't walk or turn, instead of jittering left/right under them.
    [SerializeField] private float stopDistanceX = 0.75f;

    [Header("Bite")]
    [SerializeField] private EnemyWeapon biteWeapon;
    // Time the bite animation is allowed to play before going back to idle.
    // Keep this matched to BeastBite.anim's total length - the hit itself is
    // fired by the Animation Event, not by this timer.
    [SerializeField] private float biteDuration = 0.75f;
    // Minimum time between the end of one bite and the start of the next.
    [SerializeField] private float biteCooldown = 1.2f;

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip biteSound; // plays on the snap frame

    private EnemyMovement movement;
    private float stateTimer;
    private float biteCooldownTimer;

    // 1 = facing right, -1 = facing left. Frozen while biting.
    private float facingDirection = 1f;
    private Vector3 baseVisualsScale;

    // =========================
    // START
    // =========================
    private void Start()
    {
        movement = GetComponent<EnemyMovement>();

        if (visuals != null)
        {
            baseVisualsScale = visuals.localScale;
            // Respect whichever way it was placed facing in the scene.
            facingDirection = Mathf.Sign(baseVisualsScale.x);
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
        // Looked up again if lost: the Player that survives a scene
        // transition replaces this scene's local one.
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null) return;
            player = playerObject.transform;
        }

        if (biteCooldownTimer > 0f)
        {
            biteCooldownTimer -= Time.fixedDeltaTime;
        }

        switch (currentState)
        {
            case BeastState.Dormant: HandleDormant(); break;
            case BeastState.WakingUp: HandleWakingUp(); break;
            case BeastState.Chasing: HandleChasing(); break;
            case BeastState.Biting: HandleBiting(); break;
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    private void HandleDormant()
    {
        movement.Stop();

        if (Vector2.Distance(transform.position, player.position) <= awakeRange)
        {
            UpdateFacing();
            stateTimer = 0f;
            if (animator != null) animator.SetBool("IsAwake", true);
            currentState = BeastState.WakingUp;
        }
    }

    private void HandleWakingUp()
    {
        movement.Stop();
        UpdateFacing();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= wakeUpDuration)
        {
            currentState = BeastState.Chasing;
        }
    }

    // Never goes back to sleep: once awake, it hunts the player for good.
    private void HandleChasing()
    {
        // Facing first: flipping Visuals moves the WeaponPivot to the other
        // side, so the hitbox check below must use the up-to-date facing.
        UpdateFacing();

        if (biteWeapon != null && biteWeapon.IsPlayerInRange())
        {
            // In reach: stand still (idle pose) and bite as soon as the
            // cooldown allows, instead of walking into/under the player.
            SetMoving(false);

            if (biteCooldownTimer <= 0f)
            {
                StartBite();
            }
            return;
        }

        float deltaX = player.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= stopDistanceX)
        {
            SetMoving(false);
            return;
        }

        movement.Move(Mathf.Sign(deltaX), chaseSpeed);
        SetMoving(true);
    }

    private void StartBite()
    {
        stateTimer = 0f;
        SetMoving(false);
        if (animator != null) animator.SetTrigger("BiteTrigger");
        currentState = BeastState.Biting;
    }

    private void HandleBiting()
    {
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= biteDuration)
        {
            biteCooldownTimer = biteCooldown;
            currentState = BeastState.Chasing;
        }
    }

    // Called via an Animation Event on BeastBite.anim (through
    // BeastAttackRelay), on the frame the jaws snap shut.
    public void OnBite()
    {
        if (currentState != BeastState.Biting) return;

        if (biteWeapon != null)
        {
            biteWeapon.TryHitPlayer();
        }

        // Explicit null check instead of ?. (Unity fake-null, see KeyBlock).
        if (sfxPlayer != null) sfxPlayer.Play(biteSound);
    }

    // Walk pose on/off. Also stops the body when not walking.
    private void SetMoving(bool moving)
    {
        if (!moving) movement.Stop();
        if (animator != null) animator.SetBool("IsMoving", moving);
    }

    private void UpdateFacing()
    {
        float deltaX = player.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= stopDistanceX) return;

        float newFacingDirection = Mathf.Sign(deltaX);
        if (newFacingDirection == facingDirection) return;

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
        Gizmos.DrawWireSphere(transform.position, awakeRange);
    }
}
