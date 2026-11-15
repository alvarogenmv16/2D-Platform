using System;
using UnityEngine;

// The list of every achievement in the game (Resources/Achievements.asset).
// Each one is unlocked from code with Achievements.Unlock("its id").
[CreateAssetMenu(menuName = "Game/Achievement Database", fileName = "Achievements")]
public class AchievementDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;            // what the code passes to Achievements.Unlock
        public string title;
        [TextArea] public string description;
        // Shown as "???" in the list until it's unlocked (for secrets).
        public bool hidden;
    }

    public Entry[] achievements = new Entry[0];
}
