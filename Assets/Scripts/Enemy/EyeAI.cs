using System.Collections;
using UnityEngine;

// Eye boss state machine. Sits Dormant until something (EyeArenaController)
// calls Activate(). Phase 1 (portal/projectile attacks) and Phase 2
// (platforming + light-burst attacks) hook into their respective states once
// designed — this only covers the summon and the health-driven phase switch.
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

    [Header("Phase transition")]
    // Fraction of max health (0-1) at which Phase 1 ends and Phase 2 begins.
    [SerializeField] private float phase2HealthThreshold = 0.5f;

    [Header("Phase 1 - portal barrage")]
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private float portalInterval = 0.5f; // one new portal per tick, at a random ceiling X
    [SerializeField] private float barrageDuration = 10f; // how long a single barrage keeps spawning portals
    [SerializeField] private float barrageCooldown = 1.5f; // pause between barrages, before the next one starts

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip summonSound;

    private bool hasEnteredPhase2 = false;

    // =========================
    // START
    // =========================
    private void OnEnable()
    {
        if (health != null)
        {
            health.OnHealthChanged.AddListener(HandleHealthChanged);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged.RemoveListener(HandleHealthChanged);
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
        sfxPlayer?.Play(summonSound);

        if (animator != null)
        {
            animator.SetTrigger("SummonTrigger");
            yield return WaitForAnimatorState("EyeSummon");
        }

        currentState = EyeState.Phase1;
        StartCoroutine(Phase1Loop());
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
        // TODO: stop Phase 1's portal attacks and play the phase-transition
        // animation/VFX here once Phase 2's art and arena are designed.
        yield return null;

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
