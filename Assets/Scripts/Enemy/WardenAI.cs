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
public class WardenAI : MonoBehaviour
{
    private enum WardenState
    {
        Inactive,
        Idle,
        Attacking,
    }

    private enum WardenAttack
    {
        Punch,
        Sweep,
    }

    [SerializeField] private WardenState currentState = WardenState.Inactive; // debug
    [SerializeField] private Animator animator;

    [Header("Timing")]
    [SerializeField] private float firstAttackDelay = 1.5f;
    [FormerlySerializedAs("punchCooldown")]
    [SerializeField] private float attackCooldown = 3f;
    // Safety net: back to Idle even if the attack clip's end is never seen.
    [FormerlySerializedAs("maxPunchDuration")]
    [SerializeField] private float maxAttackDuration = 3.5f;

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

    [Header("Sound (optional)")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip punchSound;
    [SerializeField] private AudioClip sweepSound;

    private Transform player;
    private EnemyHealth health;
    private float stateTimer;
    private WardenAttack lastAttack;
    private int sameAttackStreak;
    private int lastSafeLane = -1;
    private ContactFilter2D solidGroundFilter;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
    private readonly Collider2D[] wallHits = new Collider2D[4];

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        if (health != null) health.OnDied.AddListener(OnDied);

        // "Queries hit triggers" is on in this project: only real ground.
        solidGroundFilter = new ContactFilter2D();
        solidGroundFilter.SetLayerMask(groundLayer);
        solidGroundFilter.useTriggers = false;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDied.RemoveListener(OnDied);
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
        }
    }

    private void HandleIdle()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer < attackCooldown || player == null) return;

        StartAttack(PickAttack());
    }

    private WardenAttack PickAttack()
    {
        WardenAttack attack = Random.value < 0.5f ? WardenAttack.Punch : WardenAttack.Sweep;

        // Never the same attack 3 times in a row.
        if (sameAttackStreak >= 2 && attack == lastAttack)
        {
            attack = attack == WardenAttack.Punch ? WardenAttack.Sweep : WardenAttack.Punch;
        }

        sameAttackStreak = attack == lastAttack ? sameAttackStreak + 1 : 1;
        lastAttack = attack;
        return attack;
    }

    private void StartAttack(WardenAttack attack)
    {
        stateTimer = 0f;
        currentState = WardenState.Attacking;
        if (animator != null) animator.SetTrigger(attack == WardenAttack.Punch ? "PunchTrigger" : "SweepTrigger");
    }

    private void HandleAttacking()
    {
        stateTimer += Time.deltaTime;

        // The clip goes back to Idle on its own (exit time); once it's there
        // after the attack, the cooldown starts.
        bool backToIdle = stateTimer > 0.2f && animator != null
            && animator.GetCurrentAnimatorStateInfo(0).IsName("WardenIdle")
            && !animator.IsInTransition(0);

        if (backToIdle || stateTimer >= maxAttackDuration)
        {
            stateTimer = 0f;
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

        if (sfxPlayer != null) sfxPlayer.Play(punchSound);
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

        if (sfxPlayer != null) sfxPlayer.Play(sweepSound);
        if (sweepSpearPrefab != null) StartCoroutine(SweepVolleys());
    }

    private IEnumerator SweepVolleys()
    {
        for (int v = 0; v < sweepVolleys; v++)
        {
            LaunchVolley();
            if (v < sweepVolleys - 1) yield return new WaitForSeconds(volleyInterval);
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
                spear.Launch(direction, leftX, rightX, sweepWarningTime);
            }
        }
    }

    private void OnDied()
    {
        StopAllCoroutines();
        currentState = WardenState.Inactive;
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
