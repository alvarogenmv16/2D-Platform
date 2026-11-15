using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Statistics of the current run (one "partida"), shown at the end of the
// game: play time, deaths (total, per level, by hazards), the enemy that
// killed the player most, and the achievements earned during this run.
//
// Its own file (persistentDataPath/runstats.json), written on every death,
// at save points and when leaving to the menu, so nothing is lost between
// saves. New Game starts it from zero; Continue loads it back.
public static class RunStats
{
    [Serializable]
    private class Counter
    {
        public string key;
        public int count;
    }

    [Serializable]
    private class Data
    {
        public float playTime;
        public int deaths;
        public int hazardDeaths;
        public int enemiesDefeated;
        public int heals;
        public int hitsTaken;
        public List<Counter> deathsPerLevel = new List<Counter>();
        public List<Counter> killers = new List<Counter>();
        public List<string> achievements = new List<string>();
    }

    private static Data data = new Data();

    private static string FilePath => Path.Combine(Application.persistentDataPath, "runstats.json");

    // Scenes that are not gameplay: play time doesn't run there.
    private static readonly HashSet<string> MenuScenes = new HashSet<string>
    {
        "MainMenu", "SettingsScene", "AchievementsScene",
    };

    // Which level each scene belongs to, for "deaths per level".
    private static readonly Dictionary<string, string> LevelOfScene = new Dictionary<string, string>
    {
        { "TutorialScene", "Tutorial" },
        { "SampleScene", "Level 1" },
        { "BossArenaScene", "Level 1" },
        { "Level2", "Level 2" },
        { "SceneBoss2", "Level 2" },
        { "Level3", "Level 3" },
        { "TempleScene", "Level 3" },
    };
    public static readonly string[] Levels = { "Tutorial", "Level 1", "Level 2", "Level 3" };

    // Names shown for each enemy (GameObject / prefab name -> display name).
    private static readonly Dictionary<string, string> EnemyNames = new Dictionary<string, string>
    {
        { "Boss", "The Crimson Mourner" },
        { "Boss1", "The Crimson Mourner" },
        { "Eye", "Divine Guardian" },
        { "Beast", "Morthos" },
        { "Warden", "Warden of Lost Souls" },
        { "Walker", "Walker" },
        { "Walker_Tutorial", "Walker" },
        { "FlyingEnemy", "Flyer" },
        { "Flyer_Tutorial", "Flyer" },
        { "Hound", "Hound" },
        { "Hound1", "Hound" },
        { "Hound_Breakable", "Hound" },
        { "Maw", "Maw" },
        { "Relic", "Relic" },
        { "FlyRelic", "Fly Relic" },
    };

    // =========================
    // RUN LIFETIME
    // =========================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        data = new Data();
        Application.quitting -= Save;
        Application.quitting += Save;
    }

    // Counts play time - one hidden object for the whole game.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateClock()
    {
        GameObject clock = new GameObject("RunStatsClock");
        clock.hideFlags = HideFlags.HideInHierarchy;
        UnityEngine.Object.DontDestroyOnLoad(clock);
        clock.AddComponent<Clock>();
    }

    private class Clock : MonoBehaviour
    {
        private void Update()
        {
            // Scaled time: the pause menu (timeScale 0) doesn't count.
            if (!MenuScenes.Contains(SceneManager.GetActiveScene().name)) data.playTime += Time.deltaTime;
        }
    }

    // SaveSystem.NewGame.
    public static void StartNewRun()
    {
        data = new Data();
        Save();
    }

    // SaveSystem.Continue.
    public static void Load()
    {
        data = new Data();
        if (!File.Exists(FilePath)) return;

        try
        {
            data = JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)) ?? new Data();
        }
        catch (Exception e)
        {
            Debug.LogError($"RunStats: could not read {FilePath}: {e.Message}");
            data = new Data();
        }
    }

    public static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"RunStats: could not write {FilePath}: {e.Message}");
        }
    }

    // =========================
    // EVENTS
    // =========================

    // PlayerHealth, on death. killer = raw name of what dealt the last hit
    // (null if unknown); hazard = it was the environment (spikes, flames...).
    public static void PlayerDied(string killer, bool hazard)
    {
        data.deaths++;
        if (hazard) data.hazardDeaths++;

        string scene = SceneManager.GetActiveScene().name;
        if (LevelOfScene.TryGetValue(scene, out string level)) Increment(data.deathsPerLevel, level);

        if (!hazard && !string.IsNullOrEmpty(killer)) Increment(data.killers, EnemyName(killer));

        Save();
    }

    // EnemyHealth, when any enemy dies (bosses included).
    public static void EnemyDefeated() => data.enemiesDefeated++;

    // PlayerPower.ApplyHeal, when a heal lands.
    public static void Healed() => data.heals++;

    // PlayerHealth, every hit that actually lands (enemies and hazards).
    public static void HitTaken() => data.hitsTaken++;

    // Achievements.Unlock - counted even if it was already unlocked in an
    // earlier run: what matters here is that it was earned in this one.
    public static void AchievementEarned(string id)
    {
        if (!data.achievements.Contains(id)) data.achievements.Add(id);
    }

    // =========================
    // READING
    // =========================

    public static float PlayTime => data.playTime;
    public static int Deaths => data.deaths;
    public static int HazardDeaths => data.hazardDeaths;
    public static int AchievementsEarned => data.achievements.Count;
    public static int EnemiesDefeated => data.enemiesDefeated;
    public static int Heals => data.heals;
    public static int HitsTaken => data.hitsTaken;
    // Souls taken this run (SoulScore lives with the run, like these stats).
    public static int SoulsCollected => SoulScore.ExportCollected().Length;

    public static int DeathsIn(string level)
    {
        Counter c = data.deathsPerLevel.Find(x => x.key == level);
        return c != null ? c.count : 0;
    }

    // Null if no enemy has killed the player.
    public static string TopKiller(out int kills)
    {
        kills = 0;
        string top = null;
        foreach (Counter c in data.killers)
        {
            if (c.count > kills)
            {
                kills = c.count;
                top = c.key;
            }
        }
        return top;
    }

    // "Hound (3)", "Maw(Clone)" -> the display name of that kind of enemy.
    public static string EnemyName(string rawName)
    {
        string name = rawName.Replace("(Clone)", "");
        int paren = name.IndexOf(" (", StringComparison.Ordinal);
        if (paren > 0) name = name.Substring(0, paren);
        // Numbered copies placed in the scenes: "Walker_2", "Flyer_Tutorial3".
        name = name.Trim().TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9').TrimEnd('_', ' ');
        return EnemyNames.TryGetValue(name, out string display) ? display : name;
    }

    public static string FormatTime(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes:00}:{t.Seconds:00}";
    }

    private static void Increment(List<Counter> list, string key)
    {
        Counter c = list.Find(x => x.key == key);
        if (c == null) list.Add(new Counter { key = key, count = 1 });
        else c.count++;
    }
}
