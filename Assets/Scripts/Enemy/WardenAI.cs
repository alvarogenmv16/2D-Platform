using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// The true final boss (TempleScene), revealed by WardenIntro. Stands in place
// and attacks from a distance with spears.
//
// Inactive until WardenIntro calls Activate() (once the scene is back from
// the black screen). Between attacks it waits attackCooldown in Idle, then
// picks one of its attacks at random (never the same one 3 times in a row).
//
// Attack 1 - punch (WardenPunch.anim): clenches both fists (Warden_Idle1),
// then raises one (Warden_Punch). On that frame (Animation Event OnPunch via
// WardenAttackRelay) a line of portals opens on the floor, centred under the
// player and spreading outwards, one after the other. Each portal is a
// WardenSpearPortal (same BossSpikeHazard as Level 1's spikes): it glows as
// a warning, then a spear shoots up out of it.
// Spots with no floor under them, or inside a wall, are skipped.
//
// Attack 2 - sweep (WardenSweep.anim): fists clenched, then the arm thrusts
// out (Warden_Spikes). On that frame (OnSweep) spears cross the whole screen
// horizontally, in volleys. There are 3 lanes, matching where the player can
// be: standing, at the top of a (powered-up) jump, and at the top of a
// double jump. Each volley fills 2 lanes and leaves 1 free - a different one
// from the previous volley - so there's always somewhere safe to be.
//
// Phase 2 (helmet off, below phase2HealthRatio) is frantic: shorter pauses,
// faster sweep volleys and warnings, and a chance to chain the next attack
// almost straight away (combo). The fists-up punch has two variants, picked
// at random: the portals, or diagonal spears - parallel diagonal lines
// falling from above the screen to the floor, in 2 volleys, the second
// shifted half a gap so the player has to move to the next safe corridor.
public class WardenAI : MonoBehaviour
{
    private enum WardenState
    {
        Inactive,
        Idle,
        Attacking,
        ChangingPhase,
    }

    private enum WardenAttack
    {
        Punch,
        Sweep,
        PunchDiagonal, // phase 2 only: same animation as Punch, diagonal spears
    }

    [SerializeField] private WardenState currentState = WardenState.Inactive; // debug
    [SerializeField] private Animator animator;

    [Header("Timing")]
    [SerializeField] private float firstAttackDelay = 1.5f;
    [FormerlySerializedAs("punchCooldown")]
    [SerializeField] private float attackCooldown = 3f;
    // Safety net: back to Idle even if the attack clip's end is never seen.
    [FormerlySerializedAs("maxPunchDuration")]
    [SerializeField] private float maxAttackDuration = 4f;

    [Header("Punch - portals")]
    [SerializeField] private GameObject portalPrefab;
    // Total portals: one under the player + the rest split to both sides.
    [SerializeField] private int portalCount = 5;
    [SerializeField] private float portalSpacing = 4.5f;
    // Delay between each ring of portals (centre first, then outwards).
    [SerializeField] private float portalStagger = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    // How far below the player's feet to look for floor for a portal.
    [SerializeField] private float floorSearchDepth = 8f;

    [Header("Sweep - spears across the screen")]
    [SerializeField] private GameObject sweepSpearPrefab;
    // Heights of the 3 lanes above the Warden's feet (the arena floor):
    // standing (hits a player on the ground), powered-up jump (above a
    // standing player's head, below a double-jumping player's feet), and
    // double jump (above the head at the top of a single jump).
    [SerializeField] private float[] laneHeights = { 0.9f, 3.6f, 6.8f };
    [SerializeField] private int sweepVolleys = 2;
    [SerializeField] private float volleyInterval = 1.4f;
    // How long each spear peeks in (with its lane lit) before it flies.
    [SerializeField] private float sweepWarningTime = 0.8f;

    [Header("Phase 2 - the helmet falls off")]
    [SerializeField] private int currentPhase = 1; // debug
    // Phase 2 starts once health drops to this fraction of the max.
    [SerializeField, Range(0.05f, 0.95f)] private float phase2HealthRatio = 0.5f;
    [SerializeField] private GameObject fallenHelmetPrefab;
    [SerializeField] private AudioClip phase2Music;
    // Safety net: back to fighting even if WardenIdle2 is never seen.
    [SerializeField] private float maxChangePhaseDuration = 3f;
    // How soon after the phase change the first phase-2 attack comes.
    [SerializeField] private float phase2FirstAttackDelay = 1f;

    [Header("Phase 2 - frantic pace")]
    [SerializeField] private float phase2AttackCooldown = 1.6f;
    // Chance that, right after an attack, the next one starts almost at once.
    [SerializeField, Range(0f, 1f)] private float phase2ComboChance = 0.35f;
    [SerializeField] private float phase2ComboDelay = 0.3f;
    [SerializeField] private float phase2VolleyInterval = 1f;
    [SerializeField] private float phase2SweepWarningTime = 0.65f;

    [Header("Phase 2 - diagonal spears (punch variant)")]
    // Chance that a phase-2 punch is the diagonal variant instead of portals.
    [SerializeField, Range(0f, 1f)] private float diagonalPunchChance = 0.5f;
    // Angle of the lines below the horizontal.
    [SerializeField, Range(20f, 70f)] private float diagonalAngle = 45f;
    // Horizontal gap between two lines where they meet the floor.
    [SerializeField] private float diagonalSpacing = 6f;
    [SerializeField] private int diagonalVolleys = 2;
    [SerializeField] private float diagonalVolleyInterval = 1f;
    [SerializeField] private float diagonalWarningTime = 0.7f;
    [SerializeField] private float diagonalSpeed = 20f;

    [Header("Death")]
    [SerializeField] private AudioClip victoryMusic;
    // The death pose holds still for deathFadeDelay, then fades out over
    // deathFadeTime - together a bit shorter than WardenDeath.anim, after
    // which EnemyHealth destroys the Warden.
    [SerializeField] private float deathFadeDelay = 1.2f;
    [SerializeField] private float deathFadeTime = 1.6f;

    private Transform player;
    private EnemyHealth health;
    private WardenHelmet weakPoint;
    private GameObject fallenHelmet; // the one knocked off at the phase change
    private float stateTimer;
    private WardenAttack lastAttack;
    private WardenAttack currentAttack;
    private int sameAttackStreak;
    private int lastSafeLane = -1;
    private ContactFilter2D solidGroundFilter;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
    private readonly Collider2D[] wallHits = new Collider2D[4];

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.OnDied.AddListener(OnDied);
            health.OnHealthChanged.AddListener(OnHealthChanged);
        }
        weakPoint = GetComponent<WardenHelmet>();

        // "Queries hit triggers" is on in this project: only real ground.
        solidGroundFilter = new ContactFilter2D();
        solidGroundFilter.SetLayerMask(groundLayer);
        solidGroundFilter.useTriggers = false;
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDied.RemoveListener(OnDied);
            health.OnHealthChanged.RemoveListener(OnHealthChanged);
        }
    }

    // Called by WardenIntro once the intro is over.
    public void Activate()
    {
        if (currentState != WardenState.Inactive) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;

        // Opening gesture (idle -> fists -> idle, WardenIntroIdle.anim), once,
        // now that the scene is visible again. Then it stands still.
        if (animator != null) animator.SetTrigger("IntroTrigger");

        stateTimer = attackCooldown - firstAttackDelay;
        currentState = WardenState.Idle;
    }

    private void Update()
    {
        switch (currentState)
        {
            case WardenState.Idle: HandleIdle(); break;
            case WardenState.Attacking: HandleAttacking(); break;
            case WardenState.ChangingPhase: HandleChangingPhase(); break;
        }
    }

    // =========================
    // PHASE CHANGE
    // =========================

    private void OnHealthChanged(float current, float max)
    {
        if (currentPhase != 1 || current <= 0f || max <= 0f) return;
        if (currentState == WardenState.Inactive) return;
        if (current / max > phase2HealthRatio) return;

        StartPhaseChange();
    }

    // The helmet is knocked off: Warden_ChangePhase pose (helmetless), the
    // helmet flies off its head and falls to the floor, the music changes.
    // Invulnerable until it's over; whatever attack was being prepared is
    // cancelled (spears already out keep going).
    private void StartPhaseChange()
    {
        currentPhase = 2;
        StopAllCoroutines();

        // Where the helmet is right now, before the pose (and the box) changes.
        Vector2 helmetPosition = weakPoint != null ? weakPoint.WeakPointCenter : (Vector2)transform.position + Vector2.up * 2.3f;
        if (weakPoint != null) weakPoint.SetVulnerable(false);

        if (animator != null)
        {
            animator.ResetTrigger("PunchTrigger");
            animator.ResetTrigger("SweepTrigger");
            animator.SetTrigger("ChangePhaseTrigger");
        }

        if (fallenHelmetPrefab != null)
        {
            fallenHelmet = SpawnFallenHelmet(helmetPosition);
        }

        WardenIntro intro = FindAnyObjectByType<WardenIntro>();
        if (intro != null) intro.ChangeMusic(phase2Music);

        stateTimer = 0f;
        currentState = WardenState.ChangingPhase;
    }

    private GameObject SpawnFallenHelmet(Vector2 position)
    {
        GameObject helmetObject = Instantiate(fallenHelmetPrefab, position, Quaternion.identity);
        if (helmetObject.TryGetComponent(out WardenFallenHelmet helmet))
        {
            // Flies away from the player, towards open floor.
            float side = player != null && player.position.x > transform.position.x ? -1f : 1f;
            // Same size it had on the Warden's head (the Warden is scaled up
            // in the scene; the prefab is at the unscaled size).
            helmet.Launch(side, Mathf.Abs(transform.lossyScale.x));
        }
        return helmetObject;
    }

    private void HandleChangingPhase()
    {
        stateTimer += Time.deltaTime;

        bool done = stateTimer > 0.2f && animator != null
            && animator.GetCurrentAnimatorStateInfo(0).IsName("WardenIdle2")
            && !animator.IsInTransition(0);

        if (!done && stateTimer < maxChangePhaseDuration) return;

        if (weakPoint != null) weakPoint.SetVulnerable(true);
        stateTimer = CurrentCooldown - phase2FirstAttackDelay;
        currentState = WardenState.Idle;
    }

    private bool IsPhase2 => currentPhase >= 2;
    private float CurrentCooldown => IsPhase2 ? phase2AttackCooldown : attackCooldown;

    private void HandleIdle()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer < CurrentCooldown || player == null) return;

        StartAttack(PickAttack());
    }

    private WardenAttack PickAttack()
    {
        bool punch = Random.value < 0.5f;

        // Never the same kind (punch / sweep) 3 times in a row.
        bool lastWasPunch = lastAttack != WardenAttack.Sweep;
        if (sameAttackStreak >= 2 && punch == lastWasPunch) punch = !punch;

        sameAttackStreak = punch == lastWasPunch ? sameAttackStreak + 1 : 1;

        WardenAttack attack = WardenAttack.Sweep;
        if (punch)
        {
            attack = IsPhase2 && Random.value < diagonalPunchChance ? WardenAttack.PunchDiagonal : WardenAttack.Punch;
        }

        lastAttack = attack;
        return attack;
    }

    private void StartAttack(WardenAttack attack)
    {
        currentAttack = attack;
        stateTimer = 0f;
        currentState = WardenState.Attacking;
        if (animator != null) animator.SetTrigger(attack == WardenAttack.Sweep ? "SweepTrigger" : "PunchTrigger");
    }

    private void HandleAttacking()
    {
        stateTimer += Time.deltaTime;

        // The clip goes back to Idle on its own (exit time); once it's there
        // after the attack, the cooldown starts.
        AnimatorStateInfo info = animator != null ? animator.GetCurrentAnimatorStateInfo(0) : default;
        bool backToIdle = stateTimer > 0.2f && animator != null
            && (info.IsName("WardenIdle") || info.IsName("WardenIdle2"))
            && !animator.IsInTransition(0);

        if (backToIdle || stateTimer >= maxAttackDuration)
        {
            // Phase 2 combo: sometimes the next attack comes almost at once.
            bool combo = IsPhase2 && Random.value < phase2ComboChance;
            stateTimer = combo ? CurrentCooldown - phase2ComboDelay : 0f;
            currentState = WardenState.Idle;
        }
    }

    // =========================
    // PUNCH
    // =========================

    // Animation Event (via WardenAttackRelay): the fist is up.
    public void OnPunch()
    {
        if (currentState != WardenState.Attacking) return;

        if (currentAttack == WardenAttack.PunchDiagonal)
        {
            if (sweepSpearPrefab != null) StartCoroutine(DiagonalVolleys());
            return;
        }

        if (portalPrefab != null && player != null) StartCoroutine(OpenPortals(player.position));
    }

    private IEnumerator OpenPortals(Vector2 target)
    {
        int perSide = Mathf.Max(0, (portalCount - 1) / 2);
        bool extraRight = (portalCount - 1) % 2 == 1; // even count: one more to the right

        for (int ring = 0; ring <= perSide + (extraRight ? 1 : 0); ring++)
        {
            if (ring == 0)
            {
                TrySpawnPortal(target.x, target.y);
            }
            else
            {
                if (ring <= perSide) TrySpawnPortal(target.x - ring * portalSpacing, target.y);
                TrySpawnPortal(target.x + ring * portalSpacing, target.y);
            }

            if (portalStagger > 0f) yield return new WaitForSeconds(portalStagger);
        }
    }

    private void TrySpawnPortal(float x, float referenceY)
    {
        // Looks for floor from a bit above the player's feet downwards.
        Vector2 origin = new Vector2(x, referenceY + 0.5f);

        // Inside a wall/solid block: no portal there.
        if (Physics2D.OverlapPoint(origin, solidGroundFilter, wallHits) > 0) return;

        int count = Physics2D.Raycast(origin, Vector2.down, solidGroundFilter, groundHits, floorSearchDepth);
        if (count == 0) return;

        Instantiate(portalPrefab, groundHits[0].point, Quaternion.identity);
    }

    // =========================
    // SWEEP
    // =========================

    // Animation Event (via WardenAttackRelay): the arm thrusts out.
    public void OnSweep()
    {
        if (currentState != WardenState.Attacking) return;

        if (sweepSpearPrefab != null) StartCoroutine(SweepVolleys());
    }

    private IEnumerator SweepVolleys()
    {
        float interval = IsPhase2 ? phase2VolleyInterval : volleyInterval;
        for (int v = 0; v < sweepVolleys; v++)
        {
            LaunchVolley();
            if (v < sweepVolleys - 1) yield return new WaitForSeconds(interval);
        }
    }

    private void LaunchVolley()
    {
        Camera cam = Camera.main;
        if (cam == null || laneHeights == null || laneHeights.Length == 0) return;

        // A free lane, different from the last volley's.
        int safeLane = Random.Range(0, laneHeights.Length);
        if (laneHeights.Length > 1 && safeLane == lastSafeLane)
        {
            safeLane = (safeLane + Random.Range(1, laneHeights.Length)) % laneHeights.Length;
        }
        lastSafeLane = safeLane;

        float halfWidth = cam.orthographicSize * cam.aspect;
        float leftX = cam.transform.position.x - halfWidth;
        float rightX = cam.transform.position.x + halfWidth;
        float direction = Random.value < 0.5f ? 1f : -1f; // either side, it doesn't matter
        float floorY = transform.position.y;

        for (int lane = 0; lane < laneHeights.Length; lane++)
        {
            if (lane == safeLane) continue;

            GameObject spearObject = Instantiate(sweepSpearPrefab, new Vector3(cam.transform.position.x, floorY + laneHeights[lane], 0f), Quaternion.identity);
            if (spearObject.TryGetComponent(out WardenSweepSpear spear))
            {
                spear.Launch(direction, leftX, rightX, IsPhase2 ? phase2SweepWarningTime : sweepWarningTime);
            }
        }
    }

    // =========================
    // DIAGONAL SPEARS (phase 2 punch variant)
    // =========================

    private IEnumerator DiagonalVolleys()
    {
        // Same slant for the whole attack; random offset, then each volley
        // shifted half a gap so the safe corridors move.
        float side = Random.value < 0.5f ? 1f : -1f;
        float offset = Random.Range(0f, diagonalSpacing);

        for (int v = 0; v < diagonalVolleys; v++)
        {
            LaunchDiagonalVolley(side, offset + v * diagonalSpacing * 0.5f);
            if (v < diagonalVolleys - 1) yield return new WaitForSeconds(diagonalVolleyInterval);
        }
    }

    // side: +1 = lines fall down-and-right, -1 = down-and-left.
    // offset: where the first line meets the floor, within one gap.
    private void LaunchDiagonalVolley(float side, float offset)
    {
        Camera cam = Camera.main;
        if (cam == null || diagonalSpacing <= 0.1f) return;

        float rad = diagonalAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad) * side, -Mathf.Sin(rad));

        float halfWidth = cam.orthographicSize * cam.aspect;
        float camX = cam.transform.position.x;
        float topY = cam.transform.position.y + cam.orthographicSize + 1f;
        float floorY = transform.position.y;

        // Each line: from above the top of the screen down to just under the
        // floor. Lines whose floor point is off-screen still cross the view
        // higher up, so the range is widened by the slant's horizontal reach.
        float height = Mathf.Max(1f, topY - floorY);
        float reach = height / Mathf.Tan(rad);
        // The line ends right at the floor (the beam's bright end must be
        // visible there); the spear itself flies on a bit into the ground,
        // hidden by the Ground tiles, before it's destroyed.
        float lengthToFloor = height / Mathf.Sin(rad);

        float minX = camX - halfWidth - (side > 0f ? 0f : reach) - diagonalSpacing;
        float maxX = camX + halfWidth + (side > 0f ? reach : 0f) + diagonalSpacing;
        float firstX = minX + Mathf.Repeat(offset - minX, diagonalSpacing);

        for (float groundX = firstX; groundX <= maxX; groundX += diagonalSpacing)
        {
            Vector2 floorPoint = new Vector2(groundX, floorY);
            Vector2 start = floorPoint - dir * lengthToFloor;

            GameObject spearObject = Instantiate(sweepSpearPrefab, start, Quaternion.identity);
            if (spearObject.TryGetComponent(out WardenSweepSpear spear))
            {
                // Beam brightest at the floor end, where the player is.
                spear.LaunchAlong(dir, start, lengthToFloor, diagonalWarningTime, true, diagonalSpeed);
            }
        }
    }

    // =========================
    // DEATH
    // =========================

    // EnemyHealth plays the death itself (DieTrigger -> EnemyDeath state,
    // WardenDeath.anim: the helmetless body dissolving into flames) and
    // destroys the Warden when the clip ends. Here: stop attacking, the
    // victory music, and fade the body out over the end of the clip.
    private void OnDied()
    {
        StopAllCoroutines();
        currentState = WardenState.Inactive;
        if (weakPoint != null) weakPoint.SetVulnerable(false);

        WardenIntro intro = FindAnyObjectByType<WardenIntro>();
        if (intro != null) intro.ChangeMusic(victoryMusic, 0.6f, false);

        // The ending: the fallen helmet offers the "Escape" once the Warden
        // has faded away. (If it somehow never lost it, the helmet drops now.)
        if (fallenHelmet == null && fallenHelmetPrefab != null)
        {
            Vector2 helmetPosition = weakPoint != null ? weakPoint.WeakPointCenter : (Vector2)transform.position + Vector2.up * 2.3f;
            fallenHelmet = SpawnFallenHelmet(helmetPosition);
        }
        if (fallenHelmet != null && fallenHelmet.TryGetComponent(out WardenEscape escape))
        {
            escape.EnableEscape(deathFadeDelay + deathFadeTime + 0.5f);
        }

        if (animator != null && animator.TryGetComponent(out SpriteRenderer body)) StartCoroutine(FadeOut(body));
    }

    private IEnumerator FadeOut(SpriteRenderer body)
    {
        yield return new WaitForSeconds(deathFadeDelay);

        Color c = body.color;
        for (float t = 0f; t < deathFadeTime; t += Time.deltaTime)
        {
            c.a = 1f - t / deathFadeTime;
            body.color = c;
            yield return null;
        }
        c.a = 0f;
        body.color = c;
    }

    private void OnDrawGizmosSelected()
    {
        if (laneHeights == null) return;

        Gizmos.color = new Color(0.3f, 1f, 0.8f, 0.8f);
        foreach (float h in laneHeights)
        {
            Vector3 p = transform.position + Vector3.up * h;
            Gizmos.DrawLine(p + Vector3.left * 12f, p + Vector3.right * 12f);
        }
    }
}
