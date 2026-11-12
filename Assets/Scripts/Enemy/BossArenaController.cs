using System.Collections;
using UnityEngine;

// Owns the boss fight's entry trigger, arena bounds, and the wall/health-bar
// activation that goes with it. BossAI reads LeftBoundX/RightBoundX/FloorY
// from here so the arena geometry has a single source of truth, shared
// between the flight/landing logic and the spike attack's spacing.
[RequireComponent(typeof(Collider2D))]
public class BossArenaController : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private BossAI boss;
    [SerializeField] private EnemyHealth bossHealth;
    [SerializeField] private BossHealthUI bossHealthUI;
    [SerializeField] private GameObject[] arenaWalls;

    [Header("Arena bounds (place these markers at the arena's edges/floor)")]
    [SerializeField] private Transform leftBound;
    [SerializeField] private Transform rightBound;
    [SerializeField] private Transform floorReference;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource; // one source, clip swapped per phase
    [SerializeField] private AudioClip battleMusic;
    [SerializeField] private AudioClip victoryMusic;

    [Header("Intro (title card, same as the Warden's)")]
    // Off, or with no title sprite: the fight starts right away on entry.
    [SerializeField] private bool playIntro = true;
    [SerializeField] private TitleCard.Settings intro = new TitleCard.Settings();

    private Collider2D triggerCollider;
    private bool hasActivated = false;

    // =========================
    // PROPERTIES
    // =========================

    public float LeftBoundX => leftBound != null ? leftBound.position.x : transform.position.x;
    public float RightBoundX => rightBound != null ? rightBound.position.x : transform.position.x;
    public float FloorY => floorReference != null ? floorReference.position.y : transform.position.y;

    // =========================
    // START
    // =========================
    private void Start()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;

        SetWallsActive(false);
    }

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDied.AddListener(HandleBossDied);
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDied.RemoveListener(HandleBossDied);
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasActivated) return;

        if (!other.CompareTag("Player")) return;

        hasActivated = true;

        // One-shot: never fire again once the fight has started.
        triggerCollider.enabled = false;

        // Walls close right away, so the player can't walk back out
        // while the intro plays.
        SetWallsActive(true);

        if (playIntro && intro.titleSprite != null)
        {
            StartCoroutine(IntroSequence(other.gameObject));
        }
        else
        {
            PlayBattleMusic();
            StartFight();
        }
    }

    // Same beats as the Warden's intro: player frozen, fade to black, the
    // boss' title over the black while its music starts, back to the scene
    // with the health bar up, then the fight starts.
    private IEnumerator IntroSequence(GameObject player)
    {
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

        if (movement != null) movement.enabled = false;
        if (playerRb != null) playerRb.linearVelocity = new Vector2(0f, playerRb.linearVelocity.y);

        yield return TitleCard.Play(intro, () =>
        {
            PlayBattleMusic();
            if (bossHealthUI != null) bossHealthUI.Show();
        });

        if (movement != null) movement.enabled = true;

        StartFight();
    }

    private void StartFight()
    {
        if (boss != null) boss.Activate();
        if (bossHealthUI != null) bossHealthUI.Show();
    }

    private void PlayBattleMusic()
    {
        if (musicSource != null && battleMusic != null)
        {
            musicSource.clip = battleMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    private void HandleBossDied()
    {
        SetWallsActive(false);

        if (musicSource != null)
        {
            musicSource.loop = false;
            musicSource.clip = victoryMusic;
            musicSource.Play();
        }
    }

    private void SetWallsActive(bool active)
    {
        foreach (GameObject wall in arenaWalls)
        {
            if (wall != null)
            {
                wall.SetActive(active);
            }
        }
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        if (leftBound == null || rightBound == null) return;

        Gizmos.color = Color.red;
        float floorY = FloorY;
        Vector3 left = new Vector3(LeftBoundX, floorY, 0f);
        Vector3 right = new Vector3(RightBoundX, floorY, 0f);
        Gizmos.DrawLine(left, right);
        Gizmos.DrawWireSphere(left, 0.2f);
        Gizmos.DrawWireSphere(right, 0.2f);
    }
}
