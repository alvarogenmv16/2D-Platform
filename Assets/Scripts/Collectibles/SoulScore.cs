using System;
using System.Collections.Generic;
using UnityEngine;

// Running total of collected souls for the whole run, plus which souls have
// already been taken. Static (not a MonoBehaviour) so it survives scene loads
// - including the reload after a death - without needing an object in every
// scene. Remembering the taken ones is what stops a soul from reappearing
// (and being counted again) when its level reloads; it's also exactly the
// data the future save system will need to store.
public static class SoulScore
{
    public static int Total { get; private set; }

    // Fired with the new total every time it changes (SoulCounterUI listens).
    public static event Action<int> OnChanged;

    private static readonly HashSet<string> collected = new HashSet<string>();

    public static bool IsCollected(string id)
    {
        return collected.Contains(id);
    }

    public static void Collect(string id, int points)
    {
        if (!collected.Add(id)) return;

        Total += points;
        OnChanged?.Invoke(Total);
    }

    // New game: forget everything.
    public static void ResetAll()
    {
        collected.Clear();
        Total = 0;
        OnChanged?.Invoke(Total);
    }

    // For SaveSystem.
    public static string[] ExportCollected()
    {
        string[] all = new string[collected.Count];
        collected.CopyTo(all);
        return all;
    }

    public static void Import(int total, string[] collectedIds)
    {
        collected.Clear();
        if (collectedIds != null) collected.UnionWith(collectedIds);
        Total = total;
        OnChanged?.Invoke(Total);
    }

    // Statics keep their values between Play sessions in the Editor when
    // Domain Reload is disabled - start every Play session from zero.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        collected.Clear();
        Total = 0;
        OnChanged = null;
    }
}
