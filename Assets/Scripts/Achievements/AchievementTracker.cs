using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Every achievement's rule, in one place. The game's scripts only report
// what happened (AchievementTracker.EnemyKilled(), .BossFightStarted("Eye"),
// .SoulCollected(...)...) and this decides what that unlocks. The ids match
// Resources/Achievements.asset.
public static class AchievementTracker
{
    // =========================
    // IDS
    // =========================
    public const string HeyListen = "HeyListen";
    public const string YouDied = "YouDied";
    public const string TheCakeIsALie = "TheCakeIsALie";
    public const string HereWeGoAgain = "HereWeGoAgain";
    public const string GottaGoFast = "GottaGoFast";
    public const string PraiseTheSun = "PraiseTheSun";
    public const string NoRussian = "NoRussian";
    public const string DangerousToGoAlone = "DangerousToGoAlone";
    public const string AreYouNotEntertained = "AreYouNotEntertained";
    public const string MyPrecious = "MyPrecious";
    public const string TheForceIsStrong = "TheForceIsStrong";
    public const string YouShallNotPass = "YouShallNotPass";
    public const string FinishHim = "FinishHim";
    public const string Perfect = "Perfect";
    public const string NextLevel = "NextLevel";
    public const string GottaCatchEmAll = "GottaCatchEmAll";
    public const string ThankYouForPlaying = "ThankYouForPlaying";
    public const string WildEnemyAppeared = "WildEnemyAppeared";
    public const string MammaMia = "MammaMia";
    public const string Over9000 = "Over9000";
    public const string BackInTheFight = "BackInTheFight";

    // =========================
    // GAME DATA THE RULES DEPEND ON
    // =========================

    // Boss ids, as reported by each boss's script.
    public const string Boss1 = "Boss1";     // BossArenaScene
    public const string Eye = "Eye";         // SceneBoss2
    public const string Beast = "Beast";     // TempleScene (Morthos)
    public const string Warden = "Warden";   // TempleScene
    private static readonly string[] AllBosses = { Boss1, Eye, Beast, Warden };

    // Souls placed in each level ("My Precious"). Update if souls are added
    // or removed in those scenes.
    private static readonly Dictionary<string, int> SoulsPerLevel = new Dictionary<string, int>
    {
        { "SampleScene", 3 },
        { "Level2", 3 },
        { "Level3", 3 },
    };

    // Damage with every DamageUpgradePickup taken (1 base + Level2 + Level3).
    private const float MaxDamage = 3f;

    // =========================
    // RUN-TIME STATE (this session only)
    // =========================
    private static string currentScene;
    private static string bossInFight;          // boss being fought right now, if any
    private static bool damagedInFight;
    private static readonly HashSet<string> bossesThatKilledUs = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        currentScene = null;
        bossInFight = null;
        damagedInFight = false;
        bossesThatKilledUs.Clear();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string previous = currentScene;
        currentScene = scene.name;

        // A scene change always ends whatever fight was going on (the Eye's
        // fight never changes scene, so this only cuts fights that are over).
        bossInFight = null;

        if (previous == "TutorialScene" && scene.name == "SampleScene") Unlock(HeyListen);
        if (scene.name == "Level2") Unlock(NextLevel);
    }

    // =========================
    // PLAYER
    // =========================

    // PlayerHealth: a hit that actually landed.
    public static void PlayerDamaged()
    {
        if (bossInFight != null) damagedInFight = true;
    }

    // PlayerHealth: health reached 0.
    public static void PlayerDied()
    {
        Unlock(YouDied);

        if (bossInFight != null)
        {
            bossesThatKilledUs.Add(bossInFight);
            bossInFight = null;
        }
    }

    // HazardDamage: the hit from spikes was the lethal one.
    public static void KilledByHazard() => Unlock(MammaMia);

    // PlayerPower.ApplyHeal: a full heal landed.
    public static void Healed() => Unlock(BackInTheFight);

    // =========================
    // ENEMIES & BOSSES
    // =========================

    // EnemyHealth: any enemy died (bosses included).
    public static void EnemyKilled() => Unlock(WildEnemyAppeared);

    // HoundAI: a hound slammed into a breakable wall and died with it.
    public static void HoundBrokeWall() => Unlock(YouShallNotPass);

    // Each boss's script, the moment its fight actually starts.
    public static void BossFightStarted(string boss)
    {
        // Coming back for more after this boss killed us.
        if (bossesThatKilledUs.Contains(boss)) Unlock(HereWeGoAgain);

        bossInFight = boss;
        damagedInFight = false;
    }

    // Each boss's script, when it dies.
    public static void BossDefeated(string boss)
    {
        if (boss == Eye) Unlock(AreYouNotEntertained);
        if (boss == Beast) Unlock(FinishHim);

        bool flawless = bossInFight == boss && !damagedInFight;
        if (bossInFight == boss) bossInFight = null;
        if (!flawless) return;

        Unlock(Perfect);
        Achievements.MarkProgress("Flawless:" + boss);

        foreach (string b in AllBosses)
        {
            if (!Achievements.HasProgress("Flawless:" + b)) return;
        }
        Unlock(Over9000);
    }

    // =========================
    // WORLD
    // =========================

    // SoulCollectible, right after it's counted in SoulScore.
    public static void SoulCollected(string scene, string soulName)
    {
        if (scene == "SampleScene" && soulName == "Soul1_3") Unlock(TheCakeIsALie);
        if (scene == "Level3" && soulName == "Soul3") Unlock(PraiseTheSun);

        // Soul ids are "<scene>/<x>,<y>" (SoulCollectible).
        Dictionary<string, int> found = new Dictionary<string, int>();
        foreach (string id in SoulScore.ExportCollected())
        {
            int slash = id.IndexOf('/');
            if (slash <= 0) continue;
            string level = id.Substring(0, slash);
            found[level] = found.TryGetValue(level, out int n) ? n + 1 : 1;
        }
        foreach (KeyValuePair<string, int> level in SoulsPerLevel)
        {
            if (!found.TryGetValue(level.Key, out int n) || n < level.Value) return;
        }
        Unlock(MyPrecious);
    }

    // SavePoint: the player walked up to a lamp.
    public static void SavePointReached(string scene, string savePointName)
    {
        if (scene == "Level2" && savePointName == "SavePoint2_3") Unlock(GottaGoFast);
    }

    // DamageUpgradePickup: claimed, newDamage = the player's damage now.
    public static void DamageUpgradeClaimed(string scene, float newDamage)
    {
        if (scene == "Level2") Unlock(DangerousToGoAlone);
        if (newDamage >= MaxDamage) Unlock(TheForceIsStrong);
    }

    // CompanionInteraction: a conversation started.
    public static void CompanionTalkedTo(string scene)
    {
        if (scene == "BossArenaScene" && GameProgress.Has("Boss1Defeated")) Unlock(NoRussian);
    }

    // CompanionGathering: every companion in the temple has been talked to.
    public static void CompanionsGathered() => Unlock(GottaCatchEmAll);

    // WardenEscape: the ending starts.
    public static void GameFinished() => Unlock(ThankYouForPlaying);

    private static void Unlock(string id) => Achievements.Unlock(id);
}
