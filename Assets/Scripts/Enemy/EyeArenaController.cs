using UnityEngine;

// Owns the Eye fight's entry trigger, arena bounds, and the wall/health-bar
// activation that goes with it. Mirrors BossArenaController so EyeAI (and
// later the phase 1/2 attack logic) can read LeftBoundX/RightBoundX/FloorY
// from a single source of truth, same as the existing Boss fight.
[RequireComponent(typeof(Collider2D))]
public class EyeArenaController : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private EyeAI eye;
    [SerializeField] private EnemyHealth eyeHealth;
    [SerializeField] private BossHealthUI eyeHealthUI;
    [SerializeField] private GameObject[] arenaWalls;

    [Header("Arena bounds (place these markers at the arena's edges/floor/ceiling)")]
    [SerializeField] private Transform leftBound;
    [SerializeField] private Transform rightBound;
    [SerializeField] private Transform floorReference;
    [SerializeField] private Transform ceilingReference; // where Phase 1's portals spawn along

    [Header("Music")]
    [SerializeField] private AudioSource musicSource; // one source, clip swapped per phase
    [SerializeField] private AudioClip battleMusic;
    [SerializeField] private AudioClip victoryMusic;

    private Collider2D triggerCollider;
    private bool hasActivated = false;

    // =========================
    // PROPERTIES
    // =========================

    public float LeftBoundX => leftBound != null ? leftBound.position.x : transform.position.x;
    public float RightBoundX => rightBound != null ? rightBound.position.x : transform.position.x;
    public float FloorY => floorReference != null ? floorReference.position.y : transform.position.y;
    public float CeilingY => ceilingReference != null ? ceilingReference.position.y : transform.position.y;

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
        if (eyeHealth != null)
        {
            eyeHealth.OnDied.AddListener(HandleEyeDied);
        }
    }

    private void OnDisable()
    {
        if (eyeHealth != null)
        {
            eyeHealth.OnDied.RemoveListener(HandleEyeDied);
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

        if (eye != null) eye.Activate();
        if (eyeHealthUI != null) eyeHealthUI.Show();
        SetWallsActive(true);

        if (musicSource != null && battleMusic != null)
        {
            musicSource.clip = battleMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        // One-shot: never fire again once the fight has started.
        triggerCollider.enabled = false;
    }

    private void HandleEyeDied()
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
