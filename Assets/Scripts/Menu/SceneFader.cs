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
            AdoptPersistentPlayer(player);

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
        }

        yield return StartCoroutine(Fade(1f, 0f));
    }

    // Makes the surviving (DontDestroyOnLoad) player the scene's one and
    // only player, right after a scene load. Shared by FadeToScene and by
    // PlayerDeathHandler's reload-after-death, so both wire it up the same.
    // Call it at least one frame after LoadScene, so the new scene's UI has
    // already run its own Start (which would otherwise bind to the local
    // placeholder player again).
    public static void AdoptPersistentPlayer(GameObject player)
    {
        PlayerDeathHandler deathHandler = player.GetComponent<PlayerDeathHandler>();

        // The destination scene may have its own Player instance placed
        // by hand (so it can still be opened and playtested on its own).
        // Now that a real player has survived the transition, that local
        // one is just a duplicate sitting wherever the scene put it -
        // remove it so only the surviving instance remains.
        foreach (GameObject taggedPlayer in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (taggedPlayer != player)
            {
                // Its scene references (death fade overlay) belong to this
                // scene - hand them over before it goes.
                if (deathHandler != null && taggedPlayer.TryGetComponent(out PlayerDeathHandler localHandler))
                {
                    deathHandler.AdoptSceneReferences(localHandler);
                }

                // Destroy is deferred to the end of the frame; untag it now
                // so nothing calling FindGameObjectWithTag this same frame
                // can still pick the duplicate.
                taggedPlayer.tag = "Untagged";
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

        // Found by type instead of a fixed GameObject name, so no naming
        // convention to remember (or get wrong) per scene. Every one in the
        // scene is rebound, not just the first found: binding only one
        // could pick a stray, empty PlayerHealthUI and leave the real mask
        // bar stuck showing the destroyed placeholder player's full health
        // instead of the surviving player's actual health.
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            foreach (PlayerHealthUI healthUI in Object.FindObjectsByType<PlayerHealthUI>(FindObjectsSortMode.None))
            {
                healthUI.Bind(playerHealth);
            }
        }

        // Same rebind, for scenes that also carry a power bar.
        PlayerPower playerPower = player.GetComponent<PlayerPower>();

        if (playerPower != null)
        {
            foreach (PlayerPowerUI powerUI in Object.FindObjectsByType<PlayerPowerUI>(FindObjectsSortMode.None))
            {
                powerUI.Bind(playerPower);
            }
        }
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
        respawnPoint.BeginRespawn();
        respawnPoint.SetMovementEnabled(false);

        yield return StartCoroutine(Fade(0f, 1f));

        respawnPoint.SnapToLastSafePosition();

        yield return StartCoroutine(Fade(1f, 0f));

        respawnPoint.SetMovementEnabled(true);
    }

    // Generic version of FlashAndTeleport: fades to black, runs atBlack while
    // the screen is fully covered (teleports, resets...), fades back in, then
    // runs onDone. Used by RisingFlameSea to restart its section. Without a
    // fader in the scene, both callbacks just run immediately.
    public static void FadeOutAndIn(System.Action atBlack, System.Action onDone = null)
    {
        if (instance == null)
        {
            atBlack?.Invoke();
            onDone?.Invoke();
            return;
        }

        instance.StartCoroutine(instance.FadeOutAndInSequence(atBlack, onDone));
    }

    private IEnumerator FadeOutAndInSequence(System.Action atBlack, System.Action onDone)
    {
        yield return StartCoroutine(Fade(0f, 1f));

        atBlack?.Invoke();

        yield return StartCoroutine(Fade(1f, 0f));

        onDone?.Invoke();
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
