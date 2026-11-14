using System;
using System.IO;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

// The save file: where the player last saved (scene + SavePoint id) and the
// run's progress - damage and jump/dash upgrades, collected souls and
// GameProgress flags (bosses beaten, upgrades picked up...). Health isn't
// stored: Continue starts at full health, like resting at a save point.
//
// One slot, a JSON file in Application.persistentDataPath.
//   Save(...)        - called by SavePoint.
//   NewGame(scene)   - main menu "New Game": forgets the current run (the
//                      file stays until the next save overwrites it).
//   Continue()       - main menu "Continue": loads the saved scene and puts
//                      the player at the saved SavePoint with their progress.
public static class SaveSystem
{
    [Serializable]
    private class SaveData
    {
        public string scene;
        public string savePointId;
        // Where the player appeared when saving - only a fallback, used if
        // that SavePoint can't be found any more (renamed id, deleted lamp).
        public bool hasPosition;
        public float x, y;
        public float damage = 1f;
        public bool jumpUpgrade;
        public float jumpMultiplier = 1f;
        public bool dashUpgrade;
        public float dashMultiplier = 1f;
        public int souls;
        public string[] collectedSouls;
        public string[] progressFlags;
    }

    private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static bool HasSave => File.Exists(FilePath);

    // Set by Continue, applied once the saved scene has loaded.
    private static SaveData pending;

    // The save point the player respawns at after dying: the last one saved
    // at during this run (or the one Continue started from). Empty after
    // New Game until the first save - dying then restarts the scene as before.
    private static string respawnScene;
    private static string respawnPointId;

    private static bool respawnHasPosition;
    private static Vector3 respawnPosition;

    public static bool TryGetRespawnPoint(out string scene, out string savePointId)
    {
        scene = respawnScene;
        savePointId = respawnPointId;
        return !string.IsNullOrEmpty(respawnScene);
    }

    // Spawn spot for the respawn save point once its scene is loaded: the
    // SavePoint with that id, or the position stored with the save.
    public static bool TryGetRespawnPosition(string savePointId, out Vector3 position)
    {
        SavePoint point = string.IsNullOrEmpty(savePointId) ? null : SavePoint.Find(savePointId);
        if (point != null)
        {
            position = point.SpawnPosition;
            return true;
        }

        position = respawnPosition;
        return respawnHasPosition;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        pending = null;
        respawnScene = null;
        respawnPointId = null;
        respawnHasPosition = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // =========================
    // SAVE
    // =========================

    public static void Save(string savePointId, Vector3 spawnPosition, GameObject player)
    {
        SaveData data = new SaveData
        {
            scene = SceneManager.GetActiveScene().name,
            savePointId = savePointId,
            souls = SoulScore.Total,
            collectedSouls = SoulScore.ExportCollected(),
            progressFlags = GameProgress.Export(),
            hasPosition = true,
            x = spawnPosition.x,
            y = spawnPosition.y,
        };

        if (player != null)
        {
            PlayerAttackHitbox attack = player.GetComponentInChildren<PlayerAttackHitbox>();
            if (attack != null) data.damage = attack.Damage;

            if (player.TryGetComponent(out PlayerMovement movement))
            {
                data.jumpUpgrade = movement.HasJumpUpgrade;
                data.jumpMultiplier = movement.JumpUpgradeMultiplier;
                data.dashUpgrade = movement.HasDashUpgrade;
                data.dashMultiplier = movement.DashUpgradeMultiplier;
            }
        }

        respawnScene = data.scene;
        respawnPointId = data.savePointId;
        respawnHasPosition = true;
        respawnPosition = spawnPosition;

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: could not write the save file ({FilePath}): {e.Message}");
        }
    }

    // =========================
    // MAIN MENU
    // =========================

    public static void NewGame(string firstScene)
    {
        ResetRun();
        SceneManager.LoadScene(firstScene);
    }

    // Pause menu "Quit to Menu": drops the run in progress (the save file
    // is untouched - Continue picks it up again from the last save).
    public static void QuitToMenu(string menuScene)
    {
        ResetRun();
        SceneManager.LoadScene(menuScene);
    }

    // Returns false (and does nothing) if there's no readable save.
    public static bool Continue()
    {
        SaveData data = Read();
        if (data == null || string.IsNullOrEmpty(data.scene)) return false;

        ResetRun();
        SoulScore.Import(data.souls, data.collectedSouls);
        GameProgress.Import(data.progressFlags);
        respawnScene = data.scene;
        respawnPointId = data.savePointId;
        respawnHasPosition = data.hasPosition;
        respawnPosition = new Vector3(data.x, data.y, 0f);

        pending = data;
        SceneManager.LoadScene(data.scene);
        return true;
    }

    private static SaveData Read()
    {
        if (!HasSave) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: could not read the save file ({FilePath}): {e.Message}");
            return null;
        }
    }

    // Forget the current run: progress, souls, and any player (or soul
    // marker) left over in DontDestroyOnLoad from a previous run.
    private static void ResetRun()
    {
        SoulScore.ResetAll();
        GameProgress.ResetAll();
        pending = null;
        respawnScene = null;
        respawnPointId = null;
        respawnHasPosition = false;

        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            UnityEngine.Object.Destroy(player);
        }
        foreach (PlayerSoul soul in UnityEngine.Object.FindObjectsByType<PlayerSoul>(FindObjectsSortMode.None))
        {
            UnityEngine.Object.Destroy(soul.gameObject);
        }
    }

    // =========================
    // CONTINUE: AFTER THE LOAD
    // =========================

    // Runs after the scene's objects are created (Awake/OnEnable) but before
    // their Start, so everything below is in place when the scene begins.
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pending == null || scene.name != pending.scene) return;

        SaveData data = pending;
        pending = null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning($"SaveSystem: no Player in '{scene.name}' to continue with.");
            return;
        }

        // Progress carried by the player itself.
        PlayerAttackHitbox attack = player.GetComponentInChildren<PlayerAttackHitbox>();
        if (attack != null && data.damage > attack.Damage) attack.IncreaseDamage(data.damage - attack.Damage);

        if (player.TryGetComponent(out PlayerMovement movement))
        {
            if (data.jumpUpgrade) movement.UpgradeJump(data.jumpMultiplier);
            if (data.dashUpgrade) movement.UpgradeDash(data.dashMultiplier);
        }

        // Where: the save point it was saved at (or, if that lamp is gone,
        // the spot stored with the save).
        if (!TryGetRespawnPosition(data.savePointId, out Vector3 to))
        {
            Debug.LogWarning($"SaveSystem: no SavePoint '{data.savePointId}' in '{scene.name}' - starting at the scene's own spawn.");
            return;
        }

        Vector3 from = player.transform.position;
        player.transform.position = to;
        if (player.TryGetComponent(out Rigidbody2D rb))
        {
            rb.position = to;
            rb.linearVelocity = Vector2.zero;
        }

        // Snap the camera there instead of panning across the whole level.
        CinemachineCamera cam = UnityEngine.Object.FindAnyObjectByType<CinemachineCamera>();
        if (cam != null) cam.OnTargetObjectWarped(player.transform, to - from);
    }
}
