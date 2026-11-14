using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Global achievements: they belong to the player, not to a run, so they're
// stored in their own file (persistentDataPath/achievements.json) and New
// Game never clears them.
//
//   Achievements.Unlock("DefeatMorthos");   // from anywhere
//   Achievements.Percent                    // 0..1, for the menu bar
//
// Unlocking shows the "Achievement unlocked" pop-up (AchievementToast).
public static class Achievements
{
    [Serializable]
    private class SaveData
    {
        public string[] unlocked;
    }

    private static AchievementDatabase database;
    private static HashSet<string> unlocked;

    public static event Action<AchievementDatabase.Entry> OnUnlocked;

    private static string FilePath => Path.Combine(Application.persistentDataPath, "achievements.json");

    // No domain reload when entering Play Mode in this project: start clean.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        database = null;
        unlocked = null;
        OnUnlocked = null;
    }

    public static AchievementDatabase.Entry[] All
    {
        get
        {
            if (database == null) database = Resources.Load<AchievementDatabase>("Achievements");
            return database != null && database.achievements != null ? database.achievements : new AchievementDatabase.Entry[0];
        }
    }

    public static int Total => All.Length;

    public static int UnlockedCount
    {
        get
        {
            int count = 0;
            foreach (AchievementDatabase.Entry entry in All)
            {
                if (IsUnlocked(entry.id)) count++;
            }
            return count;
        }
    }

    // Completion of the game, 0..1.
    public static float Percent => Total > 0 ? (float)UnlockedCount / Total : 0f;

    public static bool IsUnlocked(string id)
    {
        Load();
        return !string.IsNullOrEmpty(id) && unlocked.Contains(id);
    }

    // Returns true only the first time; later calls do nothing.
    public static bool Unlock(string id)
    {
        AchievementDatabase.Entry entry = Find(id);
        if (entry == null)
        {
            Debug.LogWarning($"Achievements: there's no achievement with id '{id}' in Resources/Achievements.");
            return false;
        }

        Load();
        if (!unlocked.Add(id)) return false;

        Save();
        AchievementToast.Show(entry);
        OnUnlocked?.Invoke(entry);
        return true;
    }

    public static AchievementDatabase.Entry Find(string id)
    {
        foreach (AchievementDatabase.Entry entry in All)
        {
            if (entry.id == id) return entry;
        }
        return null;
    }

#if UNITY_EDITOR
    // Tools > Achievements > Reset (see AchievementsEditorMenu) - for testing.
    public static void ResetAll()
    {
        unlocked = new HashSet<string>();
        Save();
    }
#endif

    private static void Load()
    {
        if (unlocked != null) return;

        unlocked = new HashSet<string>();
        if (!File.Exists(FilePath)) return;

        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            if (data?.unlocked != null) unlocked.UnionWith(data.unlocked);
        }
        catch (Exception e)
        {
            Debug.LogError($"Achievements: could not read {FilePath}: {e.Message}");
        }
    }

    private static void Save()
    {
        try
        {
            SaveData data = new SaveData { unlocked = new List<string>(unlocked).ToArray() };
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"Achievements: could not write {FilePath}: {e.Message}");
        }
    }
}
