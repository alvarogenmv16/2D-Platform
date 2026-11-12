using System.Collections.Generic;
using UnityEngine;

// One-off things that have already happened in this run ("Boss1Defeated"...),
// so a scene that gets loaded again - walking back into it from the next
// level, or reloading after a death - doesn't replay them. Static, like
// SoulScore, so it survives scene loads without an object in every scene;
// it's also what the future save system will store.
public static class GameProgress
{
    private static readonly HashSet<string> flags = new HashSet<string>();

    public static bool Has(string flag)
    {
        return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
    }

    public static void Set(string flag)
    {
        if (!string.IsNullOrEmpty(flag)) flags.Add(flag);
    }

    // New game: forget everything.
    public static void ResetAll()
    {
        flags.Clear();
    }

    // Statics keep their values between Play sessions in the Editor when
    // Domain Reload is disabled - start every Play session clean.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        flags.Clear();
    }
}
