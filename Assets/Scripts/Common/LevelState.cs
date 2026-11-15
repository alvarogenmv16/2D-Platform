using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Puzzle progress inside a level (Level3's key blocks, the chalice puzzle,
// the walls the Hounds smash), tied to save points:
//   - Saving at a lamp stores the current state (SaveSystem.Save).
//   - Dying sends the player back to that lamp with the state it saved -
//     keys taken since then are back in place (PlayerDeathHandler).
//   - Continue restores it from the save file; New Game clears it.
//
// Each thing is identified by scene + where it was placed, the same way
// souls are (SoulCollectible).
public static class LevelState
{
    private static readonly HashSet<string> current = new HashSet<string>();
    private static readonly HashSet<string> saved = new HashSet<string>();

    // No domain reload when entering Play Mode in this project: start clean.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        current.Clear();
        saved.Clear();
    }

    // "Key:Level3/12.5,-4.0" - kind + scene + position.
    public static string IdFor(string kind, Component c)
    {
        Vector3 p = c.transform.position;
        return $"{kind}:{c.gameObject.scene.name}/{p.x:F1},{p.y:F1}";
    }

    public static bool Has(string id) => current.Contains(id);
    public static void Mark(string id) => current.Add(id);
    public static void Unmark(string id) => current.Remove(id);

    // How many ids of this kind are marked in a scene (e.g. keys taken).
    public static int Count(string kind, string scene)
    {
        string prefix = $"{kind}:{scene}/";
        int n = 0;
        foreach (string id in current)
        {
            if (id.StartsWith(prefix)) n++;
        }
        return n;
    }

    // =========================
    // SAVE POINTS
    // =========================

    // SaveSystem.Save: this is now the state to come back to.
    public static string[] CommitAndExport()
    {
        saved.Clear();
        saved.UnionWith(current);
        string[] all = new string[saved.Count];
        saved.CopyTo(all);
        return all;
    }

    // SaveSystem.Continue.
    public static void Import(string[] ids)
    {
        current.Clear();
        saved.Clear();
        if (ids == null) return;
        current.UnionWith(ids);
        saved.UnionWith(ids);
    }

    // PlayerDeathHandler: back to the last save.
    public static void RevertToSaved()
    {
        current.Clear();
        current.UnionWith(saved);
    }

    // New Game / back to the menu.
    public static void Clear()
    {
        current.Clear();
        saved.Clear();
    }
}
