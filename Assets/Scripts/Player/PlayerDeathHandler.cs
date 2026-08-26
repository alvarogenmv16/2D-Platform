using System.Collections;
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
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}