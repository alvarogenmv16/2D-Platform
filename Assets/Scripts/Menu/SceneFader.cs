using System.Collections;
using Unity.Cinemachine;
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
    //
    // entryPointId is optional: when given, the player is moved to whichever
    // LevelEntryPoint in the newly loaded scene has a matching Id, right
    // before fading back in. Left null/empty, the player just keeps
    // whatever world position they carried over (the old behavior).
    public static void FadeToScene(string sceneName, string entryPointId = null)
    {
        if (instance == null) return;

        instance.StartCoroutine(instance.FadeSequence(sceneName, entryPointId));
    }

    private IEnumerator FadeSequence(string sceneName, string entryPointId)
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
            // The destination scene may have its own Player instance placed
            // by hand (so it can still be opened and playtested on its own).
            // Now that a real player has survived the transition, that local
            // one is just a duplicate sitting wherever the scene put it -
            // remove it so only the surviving instance remains.
            foreach (GameObject taggedPlayer in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (taggedPlayer != player)
                {
                    Destroy(taggedPlayer);
                }
            }

            // Point the level's camera at the surviving player - its Follow
            // target was wired in the Inspector to the local Player instance
            // just destroyed above, so left alone it would track nothing.
            // Found by type rather than by name: every gameplay scene has
            // exactly one, so no naming convention is needed.
            CinemachineCamera cinemachineCamera = Object.FindFirstObjectByType<CinemachineCamera>();

            if (cinemachineCamera != null)
            {
                cinemachineCamera.Follow = player.transform;
            }

            if (!string.IsNullOrEmpty(entryPointId))
            {
                LevelEntryPoint entryPoint = FindEntryPoint(entryPointId);

                if (entryPoint != null)
                {
                    // Only the position is teleported - velocity (speed and
                    // direction) is left untouched on purpose, so momentum
                    // from the old scene (e.g. still rising from a jump when
                    // the exit trigger was hit) carries straight into the
                    // new one instead of getting cut to a dead stop.
                    player.transform.position = entryPoint.transform.position;
                }
                else
                {
                    Debug.LogWarning($"SceneFader: no LevelEntryPoint with id '{entryPointId}' found in scene '{sceneName}'.");
                }
            }

            // Found by type instead of a fixed GameObject name - every
            // gameplay scene just needs one PlayerHealthUI, no naming
            // convention to remember (or get wrong) per scene.
            PlayerHealthUI healthUI = Object.FindFirstObjectByType<PlayerHealthUI>();
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

            if (healthUI != null && playerHealth != null)
            {
                healthUI.Bind(playerHealth);
            }

            // Same rebind, for scenes that also carry a power bar.
            PlayerPowerUI powerUI = Object.FindFirstObjectByType<PlayerPowerUI>();
            PlayerPower playerPower = player.GetComponent<PlayerPower>();

            if (powerUI != null && playerPower != null)
            {
                powerUI.Bind(playerPower);
            }
        }

        yield return StartCoroutine(Fade(1f, 0f));
    }

    private static LevelEntryPoint FindEntryPoint(string id)
    {
        foreach (LevelEntryPoint candidate in Object.FindObjectsByType<LevelEntryPoint>(FindObjectsSortMode.None))
        {
            if (candidate.Id == id) return candidate;
        }

        return null;
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
