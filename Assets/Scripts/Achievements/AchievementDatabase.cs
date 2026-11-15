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
        // Its card (Sprites/PlayerSprite/Achivements.png). Only shown once
        // it's unlocked.
        public Sprite icon;
    }

    [Header("Unlock pop-up")]
    public AudioClip unlockSound;
    [Range(0f, 1f)] public float unlockVolume = 1f;

    public Entry[] achievements = new Entry[0];
}
