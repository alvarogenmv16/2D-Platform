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

    // Name of the GameObject holding PlayerHealthUI in any scene this fader
    // loads into. Rebound at runtime because the surviving Player isn't
    // part of that scene's saved data, so it can't be wired via Inspector.
    [SerializeField] private string playerHealthUIObjectName = "HealthMaskContainer";

    // Name of the GameObject holding the level's CinemachineCamera. Rebound
    // at runtime for the same reason as playerHealthUIObjectName: its Follow
    // target is normally wired in the Inspector to that scene's own local
    // Player, which gets destroyed in favor of the surviving one below.
    [SerializeField] private string cinemachineCameraObjectName = "CinemachineCamera";

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
    // entryPointName is optional: when given, the player is moved to the
    // position of a GameObject with that name found in the newly loaded
    // scene, right before fading back in. Left null/empty, the player just
    // keeps whatever world position they carried over (the old behavior).
    public static void FadeToScene(string sceneName, string entryPointName = null)
    {
        if (instance == null) return;

        instance.StartCoroutine(instance.FadeSequence(sceneName, entryPointName));
    }

    private IEnumerator FadeSequence(string sceneName, string entryPointName)
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
            GameObject cameraObject = GameObject.Find(cinemachineCameraObjectName);
            CinemachineCamera cinemachineCamera = cameraObject != null ? cameraObject.GetComponent<CinemachineCamera>() : null;

            if (cinemachineCamera != null)
            {
                cinemachineCamera.Follow = player.transform;
            }

            if (!string.IsNullOrEmpty(entryPointName))
            {
                GameObject entryPoint = GameObject.Find(entryPointName);

                if (entryPoint != null)
                {
                    player.transform.position = entryPoint.transform.position;

                    // Clear any velocity carried over from the previous scene
                    // (e.g. still moving/falling when the trigger was hit),
                    // same reasoning as PlayerRespawnPoint.SnapToLastSafePosition.
                    if (player.TryGetComponent(out Rigidbody2D playerRb))
                    {
                        playerRb.linearVelocity = Vector2.zero;
                    }
                }
                else
                {
                    Debug.LogWarning($"SceneFader: entry point '{entryPointName}' not found in scene '{sceneName}'.");
                }
            }

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
