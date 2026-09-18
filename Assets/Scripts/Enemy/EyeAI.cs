using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Eye boss state machine. Sits Dormant until something (EyeArenaController)
// calls Activate(). Phase 1 (portal/projectile attacks, plus wandering
// between spots in the arena) and Phase 2 (platforming + light-burst
// attacks) hook into their respective states once fully designed.
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
    // Name of the dedicated Phase 2 scene (must be added to Build Settings).
    [SerializeField] private string phase2SceneName = "EyePhase2";
    // Name of the GameObject holding BossHealthUI in that scene — rebound at
    // runtime because the Eye isn't part of that scene's saved data.
    [SerializeField] private string phase2HealthBarObjectName = "BossHealthBar";
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

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip summonSound;

    // Fires once the summon animation actually finishes (fully grown), not
    // when it starts — lets EyeArenaController hold the battle music until
    // the Eye is fully "loaded" instead of starting it on the reveal cut.
    public UnityEvent OnSummonComplete;

    private bool hasEnteredPhase2 = false;
    private FlyingEnemyMovement movement;

    // =========================
    // START
    // =========================
    private void Start()
    {
        movement = GetComponent<FlyingEnemyMovement>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnHealthChanged.AddListener(HandleHealthChanged);
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged.RemoveListener(HandleHealthChanged);
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // Runs after EVERY scene load while the Eye is alive, not just the
    // Phase 2 one — the name check is what makes this a no-op the rest of
    // the time (e.g. if the player later reaches a game-over/menu scene).
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != phase2SceneName) return;

        GameObject healthBarObject = GameObject.Find(phase2HealthBarObjectName);
        BossHealthUI healthUI = healthBarObject != null ? healthBarObject.GetComponent<BossHealthUI>() : null;

        if (healthUI != null)
        {
            healthUI.Bind(health);
            healthUI.Show();
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
        if (arenaFloor != null)
        {
            yield return StartCoroutine(arenaFloor.Crumble());
        }

        // Survive the upcoming scene load carrying the current health value
        // along (same instance, same accumulated damage) — DontDestroyOnLoad
        // only works on root GameObjects, hence the detach first.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        SceneFader.FadeToScene(phase2SceneName);

        // TODO: once EyePhase2Arena has real perch points, move the Eye to
        // the first one here instead of just sitting wherever it lands.
        currentState = EyeState.Phase2;
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
