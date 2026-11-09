using Unity.Cinemachine;
using UnityEngine;

// Final boss (Morthos, TempleScene). Asleep until the player comes close:
// lying still (beast_0), no animation, no roar. Once the player is within
// awakeRange it plays its one-shot wake-up (BeastWake.anim: gets up, then
// holds the roar frame for the whole scream, with BeastRoar firing the
// sound + camera shake) and from then on it never loses the player again,
// whatever the distance. The lying pose is never used again after that.
//
// Awake, it slowly walks after the player along the ground (walk pose =
// beast_1 + WalkBob's step bounce), and stops to bite when they're in
// reach. Standing still it just holds beast_1 - no roar.
//
// Phases - one per third of its health (read from EnemyHealth):
//   1 (100% -> 2/3): bite only.
//   2 (2/3 -> 1/3):  bite + charge.
//   3 (1/3 -> 0):    everything (bite + charge + leap).
// Each phase has its own music track (BeastPhaseMusic); phase 1's starts
// when the wake-up roar ends.
//
// Attack 1 - bite, same idea as MawAI: the bite starts the moment the
// player is inside the mouth's hitbox (EnemyWeapon.IsPlayerInRange, the
// same box TryHitPlayer checks), and the damage itself lands on the frame
// the jaws snap shut, through an Animation Event (OnBite via
// BeastAttackRelay).
//
// Attack 2 - charge, same idea as HoundAI: when the player is far enough
// away it crouches (BeastChargeWindup.anim, turning to keep facing them),
// then locks the direction and dashes past where the player was. It hurts
// once on contact, and stops at walls, ledges, after overshooting the
// target or on a timeout, then takes a moment to recover.
//
// Attack 3 - leap from the ceiling (phase 3):
//   1. Vanish: BeastLeapVanish.anim (crouch -> dissolve -> faded) while the
//      sprite fades out. From here until it lands it has no collider: it
//      can't be hit and doesn't hurt.
//   2. Track: a shadow appears on the floor and follows the player, a bit
//      slower than they run, growing and darkening.
//   3. Lock: the shadow stops and pulses red - last chance to move away.
//   4. Fall: it drops from above the camera onto the shadow, lands
//      (BeastLeapLand.anim), shakes the camera and hits everything in a
//      wide area around it, then stays still a moment - the window to
//      punish it.
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
        Biting,
        ChargeWindup,
        Charging,
        ChargeRecover,
        LeapVanish,
        LeapTracking,
        LeapLocked,
        LeapFalling,
        LeapLanding
    }

    [SerializeField] private BeastState currentState = BeastState.Dormant;
    // Shown for debugging only - it's worked out from EnemyHealth.
    [SerializeField] private int currentPhase = 1;

    [SerializeField] private Transform player;
    [SerializeField] private float awakeRange = 14f;

    // Child transform holding the sprite and the weapon pivot. Only THIS
    // gets flipped, never the root (which holds the Rigidbody2D/Collider2D)
    // - same convention as the other enemies. Sprites face right.
    [SerializeField] private Transform visuals;
    [SerializeField] private Animator animator;

    [Header("Wake up")]
    // Time standing in place (getting up + roar) before the chase starts.
    // Keep it matched to BeastWake.anim's length (0.7s getting up + the
    // 3.56s scream held on the roar frame).
    [SerializeField] private float wakeUpDuration = 4.26f;

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

    [Header("Charge (phase 2+)")]
    // Hitbox checked every step of the dash (the head/front of the body).
    [SerializeField] private EnemyWeapon chargeWeapon;
    // Only charges when the player is between these horizontal distances
    // (closer than min it just walks/bites) and roughly at its height.
    [SerializeField] private float minChargeDistance = 5f;
    [SerializeField] private float maxChargeDistance = 20f;
    [SerializeField] private float maxChargeHeight = 3f;
    // Crouch before the dash. Keep it matched to BeastChargeWindup.anim.
    [SerializeField] private float chargeWindupDuration = 0.8f;
    [SerializeField] private float chargeSpeed = 13f;
    // How far past the player's (locked) position the dash keeps going.
    [SerializeField] private float chargeOvershoot = 3f;
    [SerializeField] private float maxChargeDuration = 2.5f; // safety fallback
    [SerializeField] private float chargeRecoverDuration = 0.9f;
    // Time between the end of one charge and the next one being allowed.
    [SerializeField] private float chargeCooldown = 4f;
    // Grace time before the first charge once phase 2 starts.
    [SerializeField] private float firstChargeDelay = 1.5f;
    // Walls/floor the dash stops at. Trigger colliders (e.g. Hazards
    // tilemaps on the Ground layer) are ignored.
    [SerializeField] private LayerMask groundLayer;

    [Header("Leap (phase 3)")]
    [SerializeField] private SpriteRenderer bodyRenderer; // faded out while it's gone
    // Floor marker. Detached from the Beast at Start so it can move freely.
    [SerializeField] private SpriteRenderer leapShadow;
    // Area hurt on landing (wide, centred on the body) + its own short,
    // heavy camera shake - the root's impulse source is the roar's.
    [SerializeField] private EnemyWeapon leapImpactWeapon;
    [SerializeField] private CinemachineImpulseSource leapImpactShake;
    // Keep it matched to BeastLeapVanish.anim; the fade to invisible runs
    // over its last leapFadeTime seconds.
    [SerializeField] private float leapVanishDuration = 1f;
    [SerializeField] private float leapFadeTime = 0.3f;
    [SerializeField] private float leapTrackDuration = 2f;
    // Player runs at 5: just below it, so running away outpaces the shadow
    // and standing still gets caught.
    [SerializeField] private float shadowFollowSpeed = 4f;
    [SerializeField] private float leapLockDuration = 0.5f;
    [SerializeField] private float leapFallSpeed = 30f;
    // Still on the ground after landing. Keep matched to BeastLeapLand.anim.
    [SerializeField] private float leapLandDuration = 1f;
    [SerializeField] private float leapCooldown = 7f;
    // Grace time before the first leap once phase 3 starts.
    [SerializeField] private float firstLeapDelay = 1.5f;
    [SerializeField] private Color shadowColor = new Color(0.12f, 0.02f, 0.2f, 0.75f);
    [SerializeField] private Color shadowLockedColor = new Color(0.75f, 0.05f, 0.1f, 0.85f);

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip biteSound;   // plays on the snap frame
    [SerializeField] private AudioClip chargeSound; // plays when the dash starts
    [SerializeField] private AudioClip leapLandSound; // plays on landing
    [SerializeField] private BeastPhaseMusic phaseMusic;

    [Header("UI")]
    // Shown when the wake-up roar ends (it hides itself when the boss dies).
    // Left empty in the prefab - a prefab can't point at scene objects - and
    // looked up in the scene instead.
    [SerializeField] private BossHealthUI bossHealthUI;

    private EnemyMovement movement;
    private EnemyHealth health;
    private Collider2D col;
    private Rigidbody2D rb;
    private float baseGravityScale;
    private float leapCooldownTimer;
    private float shadowX;
    private float shadowGroundY;
    private Vector3 shadowBaseScale = Vector3.one;
    private WalkBob walkBob; // on Visuals, optional
    private float stateTimer;
    private float biteCooldownTimer;
    private float chargeCooldownTimer;

    private float chargeDirection;
    private float chargeTargetX;
    private bool hasDealtDamageThisCharge;
    private ContactFilter2D solidGroundFilter;
    private readonly RaycastHit2D[] rayHits = new RaycastHit2D[1];

    // 1 = facing right, -1 = facing left. Frozen while biting/charging.
    private float facingDirection = 1f;
    private Vector3 baseVisualsScale;

    private bool IsAwake => currentState != BeastState.Dormant && currentState != BeastState.WakingUp;

    // =========================
    // AWAKE / START
    // =========================
    private void Awake()
    {
        // Subscribed here (not OnEnable): EnemyHealth disables this
        // component for a moment on every hit, and no phase change may be
        // missed because of that.
        health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.OnHealthChanged.AddListener(OnHealthChanged);
            health.OnDied.AddListener(OnDied);
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged.RemoveListener(OnHealthChanged);
            health.OnDied.RemoveListener(OnDied);
        }
        // The shadow was detached at Start, so it isn't destroyed with us.
        if (leapShadow != null) Destroy(leapShadow.gameObject);
    }

    private void Start()
    {
        movement = GetComponent<EnemyMovement>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        baseGravityScale = rb.gravityScale;

        if (leapShadow != null)
        {
            leapShadow.transform.SetParent(null, true);
            shadowBaseScale = leapShadow.transform.localScale;
            leapShadow.gameObject.SetActive(false);
        }

        solidGroundFilter = new ContactFilter2D();
        solidGroundFilter.SetLayerMask(groundLayer);
        solidGroundFilter.useTriggers = false;

        if (visuals != null)
        {
            baseVisualsScale = visuals.localScale;
            // Respect whichever way it was placed facing in the scene.
            facingDirection = Mathf.Sign(baseVisualsScale.x);
            walkBob = visuals.GetComponent<WalkBob>();
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

        if (biteCooldownTimer > 0f) biteCooldownTimer -= Time.fixedDeltaTime;
        if (chargeCooldownTimer > 0f) chargeCooldownTimer -= Time.fixedDeltaTime;
        if (leapCooldownTimer > 0f) leapCooldownTimer -= Time.fixedDeltaTime;

        switch (currentState)
        {
            case BeastState.Dormant: HandleDormant(); break;
            case BeastState.WakingUp: HandleWakingUp(); break;
            case BeastState.Chasing: HandleChasing(); break;
            case BeastState.Biting: HandleBiting(); break;
            case BeastState.ChargeWindup: HandleChargeWindup(); break;
            case BeastState.Charging: HandleCharging(); break;
            case BeastState.ChargeRecover: HandleChargeRecover(); break;
            case BeastState.LeapVanish: HandleLeapVanish(); break;
            case BeastState.LeapTracking: HandleLeapTracking(); break;
            case BeastState.LeapLocked: HandleLeapLocked(); break;
            case BeastState.LeapFalling: HandleLeapFalling(); break;
            case BeastState.LeapLanding: HandleLeapLanding(); break;
        }
    }

    // =========================
    // PHASES
    // =========================

    // 1/3 of the max health per phase. Phases only ever go up.
    private void OnHealthChanged(float current, float max)
    {
        if (current <= 0f || max <= 0f) return; // dying, not a new phase

        float ratio = current / max;
        int phase = ratio > 2f / 3f ? 1 : ratio > 1f / 3f ? 2 : 3;
        if (phase <= currentPhase) return;

        currentPhase = phase;
        chargeCooldownTimer = Mathf.Max(chargeCooldownTimer, firstChargeDelay);
        if (currentPhase >= 3) leapCooldownTimer = Mathf.Max(leapCooldownTimer, firstLeapDelay);

        // Before the roar ends the music isn't on yet - it starts with
        // whatever phase it's in by then.
        if (IsAwake && phaseMusic != null) phaseMusic.PlayPhase(currentPhase);
    }

    // EnemyHealth disables this component and plays EnemyDeath (BeastDeath.anim:
    // lying down, then dissolving into smoke) before destroying the Beast.
    // This only tidies up what the AI itself left running.
    private void OnDied()
    {
        if (phaseMusic != null) phaseMusic.PlayVictory(); // battle track fades out
        if (walkBob != null) walkBob.SetWalking(false);
        if (leapShadow != null) leapShadow.gameObject.SetActive(false);
        SetBodyAlpha(1f);
    }

    private void ShowHealthBar()
    {
        if (bossHealthUI == null)
        {
            // Include inactive: the bar starts hidden (it may be its own
            // barRoot, so the whole object is switched off).
            bossHealthUI = FindFirstObjectByType<BossHealthUI>(FindObjectsInactive.Include);
        }

        if (bossHealthUI != null) bossHealthUI.Show();
    }

    private bool CanCharge => currentPhase >= 2;
    private bool CanLeap => currentPhase >= 3;

    // =========================
    // STATES
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
            // The roar is over: the fight (its music and health bar) starts.
            if (phaseMusic != null) phaseMusic.PlayPhase(currentPhase);
            ShowHealthBar();
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
        float deltaY = player.position.y - transform.position.y;

        // Leap goes first when both are ready: it's the rarer attack.
        if (CanLeap && leapCooldownTimer <= 0f)
        {
            StartLeap();
            return;
        }

        if (CanCharge && chargeCooldownTimer <= 0f
            && Mathf.Abs(deltaX) >= minChargeDistance
            && Mathf.Abs(deltaX) <= maxChargeDistance
            && Mathf.Abs(deltaY) <= maxChargeHeight)
        {
            StartChargeWindup();
            return;
        }

        if (Mathf.Abs(deltaX) <= stopDistanceX)
        {
            SetMoving(false);
            return;
        }

        movement.Move(Mathf.Sign(deltaX), chaseSpeed);
        SetMoving(true);
    }

    // ---------- Bite ----------

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

    // ---------- Charge ----------

    private void StartChargeWindup()
    {
        stateTimer = 0f;
        SetMoving(false);
        if (animator != null) animator.SetTrigger("ChargeTrigger");
        currentState = BeastState.ChargeWindup;
    }

    private void HandleChargeWindup()
    {
        movement.Stop();
        // Keeps turning toward the player while crouched, so it always
        // charges the way it's looking.
        UpdateFacing();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer < chargeWindupDuration) return;

        // Captured ONCE here, never updated during the dash (same as the
        // Hound): a homing charge would be undodgeable.
        chargeDirection = facingDirection;
        chargeTargetX = player.position.x + chargeDirection * chargeOvershoot;
        hasDealtDamageThisCharge = false;
        stateTimer = 0f;

        if (animator != null) animator.SetBool("IsCharging", true);
        if (sfxPlayer != null) sfxPlayer.Play(chargeSound);

        currentState = BeastState.Charging;
    }

    private void HandleCharging()
    {
        stateTimer += Time.fixedDeltaTime;

        movement.Move(chargeDirection, chargeSpeed);

        // Damage on contact at any point of the dash, only once per charge -
        // the Player/Enemy layers don't collide, so it runs straight through.
        if (!hasDealtDamageThisCharge && chargeWeapon != null && chargeWeapon.IsPlayerInRange())
        {
            chargeWeapon.TryHitPlayer();
            hasDealtDamageThisCharge = true;
        }

        bool passedTarget = (transform.position.x - chargeTargetX) * chargeDirection >= 0f;
        bool timedOut = stateTimer >= maxChargeDuration;

        if (passedTarget || timedOut || HasWallAhead() || !HasGroundAhead())
        {
            EndCharge();
        }
    }

    private void EndCharge()
    {
        movement.Stop();
        stateTimer = 0f;
        if (animator != null) animator.SetBool("IsCharging", false);
        currentState = BeastState.ChargeRecover;
    }

    private void HandleChargeRecover()
    {
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= chargeRecoverDuration)
        {
            chargeCooldownTimer = chargeCooldown;
            currentState = BeastState.Chasing;
        }
    }

    // ---------- Leap ----------

    private void StartLeap()
    {
        stateTimer = 0f;
        SetMoving(false);
        if (animator != null) animator.SetTrigger("LeapTrigger");
        currentState = BeastState.LeapVanish;
    }

    private void HandleLeapVanish()
    {
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        float fadeStart = leapVanishDuration - leapFadeTime;
        SetBodyAlpha(1f - Mathf.Clamp01((stateTimer - fadeStart) / Mathf.Max(leapFadeTime, 0.01f)));

        if (stateTimer < leapVanishDuration) return;

        // Gone: no collider (can't be hit, can't hurt) and no gravity, or it
        // would fall through the floor without its collider.
        SetBodyAlpha(0f);
        SetBodySolid(false);

        shadowX = player.position.x;
        shadowGroundY = transform.position.y;
        UpdateShadowGround();
        if (leapShadow != null) leapShadow.gameObject.SetActive(true);
        UpdateShadowVisual(0f, false);

        stateTimer = 0f;
        currentState = BeastState.LeapTracking;
    }

    private void HandleLeapTracking()
    {
        stateTimer += Time.fixedDeltaTime;

        shadowX = Mathf.MoveTowards(shadowX, player.position.x, shadowFollowSpeed * Time.fixedDeltaTime);
        UpdateShadowGround();
        UpdateShadowVisual(Mathf.Clamp01(stateTimer / leapTrackDuration), false);

        if (stateTimer >= leapTrackDuration)
        {
            stateTimer = 0f;
            currentState = BeastState.LeapLocked;
        }
    }

    private void HandleLeapLocked()
    {
        stateTimer += Time.fixedDeltaTime;
        UpdateShadowVisual(1f, true);

        if (stateTimer < leapLockDuration) return;

        // Drop in from just above the top of the screen, right over the
        // shadow, facing the player.
        float startY = shadowGroundY + 10f;
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            startY = Mathf.Max(startY, cam.transform.position.y + cam.orthographicSize + 1f);
        }

        Vector2 start = new Vector2(shadowX, startY);
        rb.position = start;
        transform.position = start;
        rb.linearVelocity = new Vector2(0f, -leapFallSpeed);

        UpdateFacing();
        SetBodyAlpha(1f);
        if (animator != null) animator.SetBool("IsLeapFalling", true);

        currentState = BeastState.LeapFalling;
    }

    // Moved by hand (collider still off, so it can't snag on platforms on
    // the way down) until it reaches the floor under the shadow.
    private void HandleLeapFalling()
    {
        float nextY = rb.position.y - leapFallSpeed * Time.fixedDeltaTime;

        if (nextY > shadowGroundY)
        {
            rb.linearVelocity = new Vector2(0f, -leapFallSpeed);
            UpdateShadowVisual(1f, true);
            return;
        }

        Vector2 landed = new Vector2(shadowX, shadowGroundY);
        rb.position = landed;
        transform.position = landed;
        rb.linearVelocity = Vector2.zero;
        SetBodySolid(true);

        if (leapShadow != null) leapShadow.gameObject.SetActive(false);
        if (animator != null) animator.SetBool("IsLeapFalling", false);

        if (leapImpactWeapon != null) leapImpactWeapon.TryHitPlayer();
        if (leapImpactShake != null) leapImpactShake.GenerateImpulse();
        if (sfxPlayer != null) sfxPlayer.Play(leapLandSound);

        stateTimer = 0f;
        currentState = BeastState.LeapLanding;
    }

    private void HandleLeapLanding()
    {
        movement.Stop();
        stateTimer += Time.fixedDeltaTime;

        if (stateTimer >= leapLandDuration)
        {
            leapCooldownTimer = leapCooldown;
            // Don't chain straight into a charge right after landing.
            chargeCooldownTimer = Mathf.Max(chargeCooldownTimer, firstChargeDelay);
            currentState = BeastState.Chasing;
        }
    }

    // Floor right under the shadow: cast down from the player's height
    // (solid colliders only), so it sits on whatever the player stands
    // above. Keeps the last floor found if there's none (e.g. over a pit).
    private void UpdateShadowGround()
    {
        Vector2 origin = new Vector2(shadowX, player.position.y + 0.5f);
        if (Physics2D.Raycast(origin, Vector2.down, solidGroundFilter, rayHits, 30f) > 0)
        {
            shadowGroundY = rayHits[0].point.y;
        }
    }

    // progress 0..1 while tracking: grows and darkens. Locked: pulses red.
    private void UpdateShadowVisual(float progress, bool locked)
    {
        if (leapShadow == null) return;

        leapShadow.transform.position = new Vector3(shadowX, shadowGroundY, 0f);
        leapShadow.transform.localScale = shadowBaseScale * Mathf.Lerp(0.5f, 1f, progress);

        Color c = shadowColor;
        c.a *= Mathf.Lerp(0.35f, 1f, progress);
        if (locked)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 25f);
            c = Color.Lerp(shadowColor, shadowLockedColor, pulse);
        }
        leapShadow.color = c;
    }

    private void SetBodyAlpha(float alpha)
    {
        if (bodyRenderer == null) return;
        Color c = bodyRenderer.color;
        c.a = alpha;
        bodyRenderer.color = c;
    }

    private void SetBodySolid(bool solid)
    {
        if (col != null) col.enabled = solid;
        rb.gravityScale = solid ? baseGravityScale : 0f;
        rb.linearVelocity = Vector2.zero;
    }

    // The dash stops at walls and at platform edges, so it never runs off a
    // ledge. Both probes look one physics step ahead, since at chargeSpeed
    // it covers a big distance every FixedUpdate.
    private bool HasWallAhead()
    {
        if (col == null) return false;

        Vector2 front = GetChargeFront(out float lookAhead);
        return Physics2D.Raycast(front, new Vector2(chargeDirection, 0f), solidGroundFilter, rayHits, lookAhead) > 0;
    }

    private bool HasGroundAhead()
    {
        if (col == null) return true;

        Vector2 front = GetChargeFront(out float lookAhead);
        Vector2 ledgeProbe = new Vector2(front.x + chargeDirection * lookAhead, col.bounds.min.y + 0.1f);
        return Physics2D.Raycast(ledgeProbe, Vector2.down, solidGroundFilter, rayHits, 0.6f) > 0;
    }

    private Vector2 GetChargeFront(out float lookAhead)
    {
        Bounds bounds = col.bounds;
        lookAhead = chargeSpeed * Time.fixedDeltaTime + 0.05f;
        return new Vector2(bounds.center.x + chargeDirection * bounds.extents.x, bounds.center.y);
    }

    // =========================
    // HELPERS
    // =========================

    // Walk pose on/off. Also stops the body when not walking.
    private void SetMoving(bool moving)
    {
        if (!moving) movement.Stop();
        if (animator != null) animator.SetBool("IsMoving", moving);
        if (walkBob != null) walkBob.SetWalking(moving);
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

        // Charge window: the player must stand between the two red boxes'
        // inner and outer edges (either side) for a charge to start.
        Gizmos.color = Color.red;
        Vector3 center = transform.position;
        Gizmos.DrawWireCube(center, new Vector3(minChargeDistance * 2f, maxChargeHeight * 2f, 0f));
        Gizmos.DrawWireCube(center, new Vector3(maxChargeDistance * 2f, maxChargeHeight * 2f, 0f));
    }
}
