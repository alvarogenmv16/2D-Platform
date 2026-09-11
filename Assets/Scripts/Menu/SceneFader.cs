using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Full-screen fade used for mid-fight scene transitions (currently: the Eye
// dropping the player into its Phase 2 arena). Survives the scene load via
// DontDestroyOnLoad, same singleton-guard pattern as PlayerSoul, so exactly
// one fader exists no matter how many times a scene reloads.
public class SceneFader : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    private static SceneFader instance;

    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.6f;

    // Name of the GameObject holding PlayerHealthUI in any scene this fader
    // loads into. Rebound at runtime because the surviving Player isn't
    // part of that scene's saved data, so it can't be wired via Inspector.
    [SerializeField] private string playerHealthUIObjectName = "HealthMaskContainer";

    // =========================
    // START
    // =========================
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Fades to black, marks the player to survive the load (so its exact
    // health/state carries over into the new scene), loads sceneName, then
    // fades back in. Callers that also need to persist themselves (e.g. the
    // Eye, mid-fight) must call DontDestroyOnLoad on their own GameObject
    // before calling this.
    public static void FadeToScene(string sceneName)
    {
        if (instance == null) return;

        instance.StartCoroutine(instance.FadeSequence(sceneName));
    }

    private IEnumerator FadeSequence(string sceneName)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // DontDestroyOnLoad only works on root GameObjects.
            player.transform.SetParent(null);
            DontDestroyOnLoad(player);
        }

        yield return StartCoroutine(Fade(0f, 1f));

        SceneManager.LoadScene(sceneName);
        yield return null; // let the new scene finish loading before fading back in

        if (player != null)
        {
            GameObject healthUIObject = GameObject.Find(playerHealthUIObjectName);
            PlayerHealthUI healthUI = healthUIObject != null ? healthUIObject.GetComponent<PlayerHealthUI>() : null;
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

            if (healthUI != null && playerHealth != null)
            {
                healthUI.Bind(playerHealth);
            }
        }

        yield return StartCoroutine(Fade(1f, 0f));
    }

    // Fades to black, snaps the player back to their last safe ground
    // position, then fades back in. Same fadeCanvasGroup as FadeToScene,
    // just without any scene load - used for the hazard bump-back.
    public static void FlashAndTeleport(PlayerRespawnPoint respawnPoint)
    {
        if (instance == null || respawnPoint == null) return;

        instance.StartCoroutine(instance.FlashSequence(respawnPoint));
    }

    private IEnumerator FlashSequence(PlayerRespawnPoint respawnPoint)
    {
        respawnPoint.SetMovementEnabled(false);

        yield return StartCoroutine(Fade(0f, 1f));

        respawnPoint.SnapToLastSafePosition();

        yield return StartCoroutine(Fade(1f, 0f));

        respawnPoint.SetMovementEnabled(true);
    }

    private IEnumerator Fade(float from, float to)
    {
        fadeCanvasGroup.blocksRaycasts = to > 0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }
}
