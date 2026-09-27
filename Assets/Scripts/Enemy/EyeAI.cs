using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Eye boss state machine. Sits Dormant until something (EyeArenaController)
// calls Activate(). Phase 1 (portal/rock barrage, plus wandering between
// spots in the arena) plays out in the upper room; once its floor crumbles,
// Phase 2 (platforming + sweeping crystal ray beams, in the room below)
// takes over as the only other attack the fight has, on purpose — each
// phase reads as a distinct kind of danger instead of a reskin of the other.
[RequireComponent(typeof(FlyingEnemyMovement))]
public class EyeAI : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    private enum EyeState
    {
        Dormant,
        Summoning,
        Phase1,
        TransitioningToPhase2,
        Phase2
    }

    [SerializeField] private EyeState currentState = EyeState.Dormant;

    [SerializeField] private Animator animator;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private EyeArenaController arena;
    [SerializeField] private EyeFloorCrumble arenaFloor;
    [SerializeField] private EyePhase2FloorTrigger phase2FloorTrigger;
    // Hidden until the summon actually starts — otherwise the Animator's
    // default Idle state (holding the fully-grown sprite) is visible from
    // the moment the scene loads, well before Activate() is ever called.
    [SerializeField] private SpriteRenderer visualsRenderer;

    [Header("Phase transition")]
    // Fraction of max health (0-1) at which Phase 1 ends and Phase 2 begins.
    [SerializeField] private float phase2HealthThreshold = 0.5f;

    [Header("Phase 1 - portal barrage")]
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private float portalInterval = 0.5f; // one new portal per tick, at a random ceiling X
    [SerializeField] private float barrageDuration = 10f; // how long a single barrage keeps spawning portals
    [SerializeField] private float barrageCooldown = 1.5f; // pause between barrages, before the next one starts

    [Header("Phase 1 - wandering")]
    [SerializeField] private float wanderSpeed = 3f;
    [SerializeField] private float minWanderPause = 2f; // how long it sits still at each spot before moving again
    [SerializeField] private float maxWanderPause = 4f;
    // Keeps wander targets off the walls, same reasoning as BossAI's wallMargin.
    [SerializeField] private float wanderMarginX = 3.5f;
    // No upward attack exists yet, so it's kept low enough that a double jump
    // can still reach it — this is a rough estimate from the player's jump
    // physics (jumpForces 5/4, gravityScale 1), not a measured value. Tune
    // both after playtesting the actual double-jump apex.
    [SerializeField] private float minHeightAboveFloor = 1.5f;
    [SerializeField] private float maxHeightAboveFloor = 3.5f;

    [Header("Phase 2 - wandering")]
    // Same drift-pause-drift idea as Phase 1's wandering, just slower and
    // with longer pauses to suit the smaller Phase 2 room — and using
    // Phase2FloorY/Phase2CeilingY directly instead of an estimated height
    // band, since Phase 2 has both markers already (Phase 1 only has a floor).
    [SerializeField] private float phase2WanderSpeed = 1.5f;
    [SerializeField] private float phase2MinWanderPause = 3f;
    [SerializeField] private float phase2MaxWanderPause = 6f;
    [SerializeField] private float phase2WanderMarginX = 2f;
    [SerializeField] private float phase2WanderMarginY = 1.5f;

    [Header("Phase 2 - platforms")]
    // Kept inactive (and therefore collision-free) until the player actually
    // reaches phase2FloorTrigger — see the comment on that class for why.
    [SerializeField] private GameObject[] phase2Platforms;

    [Header("Phase 2 - crystal ray")]
    // One horizontal segment, launched to slide rightward until it exits
    // past the right wall.
    [SerializeField] private GameObject horizontalRayPrefab;
    // Hand-placed spawn points along the left wall, one per height a
    // horizontal wave can threaten — place them yourself in the scene so you
    // control exactly where each one sits and how much room is between them.
    // One is skipped at random each wave as the gap.
    [SerializeField] private Transform[] horizontalSpawnPoints;
    // One vertical segment (its sprite pre-rotated 90 degrees on the
    // prefab), launched to slide upward until it exits past the ceiling.
    [SerializeField] private GameObject verticalRayPrefab;
    // Hand-placed spawn points along the floor, one per X position a
    // vertical wave can threaten. One is skipped at random each wave as the gap.
    [SerializeField] private Transform[] verticalSpawnPoints;
    [SerializeField] private float raySpeed = 4f; // how fast each segment slides across the room
    [SerializeField] private float rayInterval = 5f; // pause between wall attacks — tune by eye against how long a wave takes to cross

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip summonSound;

    // Fires once the summon animation actually finishes (fully grown), not
    // when it starts — lets EyeArenaController hold the battle music until
    // the Eye is fully "loaded" instead of starting it on the reveal cut.
    public UnityEvent OnSummonComplete;

    private bool hasEnteredPhase2 = false;
    private bool phase2FloorReached = false;
    private FlyingEnemyMovement movement;

    // =========================
    // START
    // =========================
    private void Start()
    {
        movement = GetComponent<FlyingEnemyMovement>();
        SetPhase2PlatformsActive(false);
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnHealthChanged.AddListener(HandleHealthChanged);
            health.OnDied.AddListener(HandleDied);
        }

        if (phase2FloorTrigger != null)
        {
            phase2FloorTrigger.OnPlayerReachedFloor.AddListener(HandlePlayerReachedPhase2Floor);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged.RemoveListener(HandleHealthChanged);
            health.OnDied.RemoveListener(HandleDied);
        }

        if (phase2FloorTrigger != null)
        {
            phase2FloorTrigger.OnPlayerReachedFloor.RemoveListener(HandlePlayerReachedPhase2Floor);
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Called by EyeArenaController when the player enters the boss arena.
    public void Activate()
    {
        if (currentState != EyeState.Dormant) return;

        currentState = EyeState.Summoning;
        StartCoroutine(SummonSequence());
    }

    private IEnumerator SummonSequence()
    {
        if (visualsRenderer != null) visualsRenderer.enabled = true;

        sfxPlayer?.Play(summonSound);

        if (animator != null)
        {
            animator.SetTrigger("SummonTrigger");
            yield return WaitForAnimatorState("EyeSummon");
        }

        currentState = EyeState.Phase1;
        OnSummonComplete?.Invoke();
        StartCoroutine(Phase1Loop());
        StartCoroutine(WanderLoop());
    }

    // Drifts to a random spot in the arena, sits there a while, then picks a
    // new one — runs alongside Phase1Loop so the Eye isn't just a static
    // target sitting wherever it landed after the summon.
    private IEnumerator WanderLoop()
    {
        while (currentState == EyeState.Phase1)
        {
            Vector2 target = PickWanderTarget();

            bool arrived = false;
            while (currentState == EyeState.Phase1 && !arrived)
            {
                arrived = movement.MoveTowards(target, wanderSpeed);
                yield return new WaitForFixedUpdate();
            }

            if (currentState != EyeState.Phase1) yield break;

            yield return new WaitForSeconds(Random.Range(minWanderPause, maxWanderPause));
        }
    }

    private Vector2 PickWanderTarget()
    {
        if (arena == null) return transform.position;

        float minX = Mathf.Min(arena.LeftBoundX + wanderMarginX, arena.RightBoundX - wanderMarginX);
        float maxX = Mathf.Max(arena.RightBoundX - wanderMarginX, arena.LeftBoundX + wanderMarginX);

        float x = Random.Range(minX, maxX);
        float y = arena.FloorY + Random.Range(minHeightAboveFloor, maxHeightAboveFloor);
        return new Vector2(x, y);
    }

    // Repeats barrage -> cooldown -> barrage for as long as Phase1 stays
    // current; HandleHealthChanged switching currentState away from Phase1
    // is what ends this loop, no extra flag needed.
    private IEnumerator Phase1Loop()
    {
        while (currentState == EyeState.Phase1)
        {
            yield return StartCoroutine(PortalBarrage());

            if (currentState != EyeState.Phase1) yield break;

            yield return new WaitForSeconds(barrageCooldown);
        }
    }

    private IEnumerator PortalBarrage()
    {
        float elapsed = 0f;

        while (elapsed < barrageDuration && currentState == EyeState.Phase1)
        {
            SpawnPortal();

            yield return new WaitForSeconds(portalInterval);
            elapsed += portalInterval;
        }
    }

    private void SpawnPortal()
    {
        if (portalPrefab == null || arena == null) return;

        float x = Random.Range(arena.LeftBoundX, arena.RightBoundX);
        Vector2 spawnPosition = new Vector2(x, arena.CeilingY);

        GameObject portalObject = Instantiate(portalPrefab, spawnPosition, Quaternion.identity);
        EyePortal portal = portalObject.GetComponent<EyePortal>();

        if (portal != null)
        {
            portal.SetFloorY(arena.FloorY);
        }
    }

    // EnemyHealth disables this component on death (aiComponentsToDisable),
    // but disabling a MonoBehaviour does NOT stop coroutines already running
    // on it — Phase2CrystalRayLoop would otherwise keep spawning walls
    // forever after the Eye is dead. Stop everything explicitly instead.
    private void HandleDied()
    {
        StopAllCoroutines();
    }

    // Fires on every hit (EnemyHealth.OnHealthChanged), not just the one that
    // crosses the threshold — hasEnteredPhase2 keeps this a one-shot switch.
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        if (hasEnteredPhase2) return;
        if (currentState != EyeState.Phase1) return;
        if (currentHealth > maxHealth * phase2HealthThreshold) return;

        hasEnteredPhase2 = true;
        currentState = EyeState.TransitioningToPhase2;
        StartCoroutine(Phase2TransitionSequence());
    }

    private IEnumerator Phase2TransitionSequence()
    {
        // Phase1Loop/WanderLoop stop on their own next iteration (both check
        // currentState == Phase1, already false by the time this runs).
        // No scene change: the arena floor just drops out from under the
        // player (and, later, the Eye) into the room below, within this
        // same scene.
        if (arenaFloor != null)
        {
            yield return StartCoroutine(arenaFloor.Crumble());
        }

        // Set before waiting on the floor trigger, not after — Phase2WanderLoop
        // reads this state, so the Eye starts drifting around the room below
        // as soon as it exists, instead of sitting still until the player lands.
        currentState = EyeState.Phase2;
        StartCoroutine(Phase2WanderLoop());

        // Platforms/attacks only start once the player has actually landed on
        // the Phase 2 floor, not the moment they fall through — otherwise
        // they'd exist in time to catch the player mid-fall instead of the floor.
        if (phase2FloorTrigger != null)
        {
            yield return new WaitUntil(() => phase2FloorReached);
        }

        SetPhase2PlatformsActive(true);
        StartCoroutine(Phase2CrystalRayLoop());
    }

    private void HandlePlayerReachedPhase2Floor()
    {
        phase2FloorReached = true;
    }

    // Drifts to a random spot in the Phase 2 room, sits there a while, then
    // picks a new one — same pattern as WanderLoop, kept as its own loop
    // (rather than reused) since it runs on different speed/pause/bounds.
    private IEnumerator Phase2WanderLoop()
    {
        while (currentState == EyeState.Phase2)
        {
            Vector2 target = PickPhase2WanderTarget();

            bool arrived = false;
            while (currentState == EyeState.Phase2 && !arrived)
            {
                arrived = movement.MoveTowards(target, phase2WanderSpeed);
                yield return new WaitForFixedUpdate();
            }

            if (currentState != EyeState.Phase2) yield break;

            yield return new WaitForSeconds(Random.Range(phase2MinWanderPause, phase2MaxWanderPause));
        }
    }

    private Vector2 PickPhase2WanderTarget()
    {
        if (arena == null) return transform.position;

        float minX = Mathf.Min(arena.Phase2LeftBoundX + phase2WanderMarginX, arena.Phase2RightBoundX - phase2WanderMarginX);
        float maxX = Mathf.Max(arena.Phase2RightBoundX - phase2WanderMarginX, arena.Phase2LeftBoundX + phase2WanderMarginX);

        float minY = Mathf.Min(arena.Phase2FloorY + phase2WanderMarginY, arena.Phase2CeilingY - phase2WanderMarginY);
        float maxY = Mathf.Max(arena.Phase2CeilingY - phase2WanderMarginY, arena.Phase2FloorY + phase2WanderMarginY);

        float x = Random.Range(minX, maxX);
        float y = Random.Range(minY, maxY);
        return new Vector2(x, y);
    }

    private void SetPhase2PlatformsActive(bool active)
    {
        foreach (GameObject platform in phase2Platforms)
        {
            if (platform != null)
            {
                platform.SetActive(active);
            }
        }
    }

    // Repeats for as long as Phase2 stays current, same "state check ends the
    // loop" idea as Phase1Loop.
    private IEnumerator Phase2CrystalRayLoop()
    {
        while (currentState == EyeState.Phase2)
        {
            yield return new WaitForSeconds(rayInterval);

            if (currentState != EyeState.Phase2) yield break;

            SpawnCrystalWall();
        }
    }

    // Alternates at random between a wall of horizontal segments (blocks
    // every height, gap is a height the player reaches via the platforms)
    // and a wall of vertical segments (blocks every X position, gap is a
    // spot to run to) — never both axes on the same wall, and which spawn
    // point is left as the gap is picked fresh each time, so the safe spot
    // is never in a predictable place.
    private void SpawnCrystalWall()
    {
        bool blockHeights = Random.Range(0, 2) == 0;

        if (blockHeights)
        {
            SpawnHorizontalRowWall();
        }
        else
        {
            SpawnVerticalColumnWall();
        }
    }

    // Launches one segment from every horizontalSpawnPoint except a random
    // gap, all sliding right until they pass the right wall.
    private void SpawnHorizontalRowWall()
    {
        if (horizontalRayPrefab == null || arena == null) return;
        if (horizontalSpawnPoints.Length == 0) return;

        int gapIndex = Random.Range(0, horizontalSpawnPoints.Length);
        Quaternion rotation = horizontalRayPrefab.transform.rotation;

        for (int i = 0; i < horizontalSpawnPoints.Length; i++)
        {
            if (i == gapIndex) continue;
            if (horizontalSpawnPoints[i] == null) continue;

            GameObject rayObject = Instantiate(horizontalRayPrefab, horizontalSpawnPoints[i].position, rotation);
            rayObject.GetComponent<EyeCrystalRay>()?.Launch(Vector2.right, raySpeed, arena.Phase2RightBoundX);
        }
    }

    // Launches one segment from every verticalSpawnPoint except a random
    // gap, all sliding up until they pass the ceiling.
    private void SpawnVerticalColumnWall()
    {
        if (verticalRayPrefab == null || arena == null) return;
        if (verticalSpawnPoints.Length == 0) return;

        int gapIndex = Random.Range(0, verticalSpawnPoints.Length);
        Quaternion rotation = verticalRayPrefab.transform.rotation;

        for (int i = 0; i < verticalSpawnPoints.Length; i++)
        {
            if (i == gapIndex) continue;
            if (verticalSpawnPoints[i] == null) continue;

            GameObject rayObject = Instantiate(verticalRayPrefab, verticalSpawnPoints[i].position, rotation);
            rayObject.GetComponent<EyeCrystalRay>()?.Launch(Vector2.up, raySpeed, arena.Phase2CeilingY);
        }
    }

    // Same "wait for the animator to actually reach this state, then wait its
    // length" pattern used by BossAI/EnemyHealth, so summon timing always
    // matches the real clip length instead of a hardcoded duration.
    private IEnumerator WaitForAnimatorState(string stateName)
    {
        int safetyFrameLimit = 180;
        int framesWaited = 0;

        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateName) && framesWaited < safetyFrameLimit)
        {
            framesWaited++;
            yield return null;
        }

        float stateLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(stateLength);
    }
}
