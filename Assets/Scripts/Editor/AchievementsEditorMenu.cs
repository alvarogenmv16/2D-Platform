using UnityEditor;
using UnityEngine;

// Testing helpers: Tools > Achievements.
public static class AchievementsEditorMenu
{
    [MenuItem("Tools/Achievements/Reset All")]
    private static void ResetAll()
    {
        Achievements.ResetAll();
        PlayerPrefs.DeleteKey("Achievements.LastShownPercent");
        Debug.Log("Achievements: all locked again.");
    }

    [MenuItem("Tools/Achievements/Unlock All (Play Mode)")]
    private static void UnlockAll()
    {
        foreach (AchievementDatabase.Entry entry in Achievements.All) Achievements.Unlock(entry.id);
    }

    [MenuItem("Tools/Achievements/Unlock All (Play Mode)", true)]
    private static bool UnlockAllValidate() => Application.isPlaying;
}
