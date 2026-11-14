using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeathHandler : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject soulPrefab;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip deathSound;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;
    private Rigidbody2D rb;

    // =========================
    // START
    // =========================

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        // Subscribe in OnEnable/unsubscribe in OnDisable, same pattern
        // used for the health UI, to avoid duplicate listeners.
        if (playerHealth != null)
        {
            playerHealth.OnDied.AddListener(HandleDeath);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDied.RemoveListener(HandleDeath);
        }
    }

    // =========================
    // FUNCTIONS
    // =========================

    private void HandleDeath()
    {
        sfxPlayer?.Play(deathSound, deathVolume);
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Stop the player from moving or jumping while the death
        // animation plays.
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Freeze physics so the corpse doesn't keep sliding or get
        // pushed around by anything still colliding with it.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (animator != null)
        {
            animator.SetTrigger("DieTrigger");

            // Wait one frame so the Animator actually processes the
            // Any State -> PlayerDeath transition before we read info
            // about the current state.
            yield return null;

            // Read the exact length of whatever clip is now playing,
            // instead of hardcoding a duration that could drift out of
            // sync if the clip is edited later.
            float deathAnimationLength = animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(deathAnimationLength);
        }

        SpawnSoul();
        // Fade to black slowly before resetting, instead of cutting instantly
        if (fadeOverlay != null)
        {
            yield return StartCoroutine(FadeToBlack());
        }
        ResetGame();
    }

    private void SpawnSoul()
    {
        if (soulPrefab == null) return;

        Instantiate(soulPrefab, transform.position, Quaternion.identity);

        // Hide the player's sprite now that the soul object is taking its
        // place visually. The last frame of Player_Death already shows the
        // soul pose, so this swap is seamless — no visible pop or flicker.
        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.enabled = false;
        }
    }

    private IEnumerator FadeToBlack()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        fadeOverlay.alpha = 1f;
    }
    private void ResetGame()
    {
        // This same player always comes back - never the scene's own fresh
        // copy, which wouldn't have the damage/jump/dash upgrades. So it's
        // kept alive through the load (DontDestroyOnLoad, like SceneFader
        // does for scene changes) and revived in place of that copy.
        transform.SetParent(null); // DontDestroyOnLoad only works on root objects
        DontDestroyOnLoad(gameObject);

        // Back to the last save point of this run (it may be in another
        // scene); before the first save, to the start of this scene.
        string sceneToLoad = SceneManager.GetActiveScene().name;
        string savePointId = null;
        if (SaveSystem.TryGetRespawnPoint(out string savedScene, out string savedId))
        {
            sceneToLoad = savedScene;
            savePointId = savedId;
        }

        StartCoroutine(ReloadAndRevive(sceneToLoad, savePointId));
    }

    private IEnumerator ReloadAndRevive(string sceneName, string savePointId)
    {
        SceneManager.LoadScene(sceneName);
        yield return null; // let the new scene run Awake/Start first (same as SceneFader)

        Vector3 from = transform.position;

        SavePoint savePoint = string.IsNullOrEmpty(savePointId) ? null : SavePoint.Find(savePointId);
        if (savePoint != null)
        {
            transform.position = savePoint.SpawnPosition;
        }
        else
        {
            // No save yet (or it's missing): land where the scene's own
            // player starts.
            foreach (GameObject taggedPlayer in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (taggedPlayer != gameObject)
                {
                    transform.position = taggedPlayer.transform.position;
                    break;
                }
            }
        }

        SceneFader.AdoptPersistentPlayer(gameObject);
        Revive();

        // Snap the camera to the respawn spot instead of panning there.
        CinemachineCamera cam = FindAnyObjectByType<CinemachineCamera>();
        if (cam != null) cam.OnTargetObjectWarped(transform, transform.position - from);
    }

    private void Revive()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = transform.position;
            rb.simulated = true;
        }

        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.enabled = true;
        }

        if (animator != null)
        {
            // Back to the default state with every parameter cleared
            // (DieTrigger, IsHit...).
            animator.Rebind();
            animator.Update(0f);
        }

        playerHealth?.Revive();

        if (TryGetComponent(out PlayerRespawnPoint respawnPoint))
        {
            respawnPoint.ResetSafePosition(transform.position);
            respawnPoint.SetMovementEnabled(true);
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }

    // Called by SceneFader.AdoptPersistentPlayer with the scene's local
    // player about to be destroyed: its references point at this scene's
    // objects, the ones this survivor carried point at the old scene's.
    public void AdoptSceneReferences(PlayerDeathHandler local)
    {
        if (local.fadeOverlay != null)
        {
            fadeOverlay = local.fadeOverlay;
        }
    }
}