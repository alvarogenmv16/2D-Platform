using UnityEngine;

// Keeps an enemy's player reference pointing at the player that's actually
// in play. The player survives scene changes and deaths (SceneFader /
// PlayerDeathHandler keep it with DontDestroyOnLoad) and replaces the
// scene's own copy, which is untagged and destroyed a frame after the load.
//
// During that first frame BOTH are tagged "Player", and the scene's copy
// stands at the scene's start - so an enemy near the start, holding that
// copy (assigned in the Inspector, or found in Start), would "see" a player
// that's about to vanish: walkers lose their target, and enemies whose
// sound latches on at first detection (Flyer_Tutorial, Walker_Tutorial)
// keep playing it forever. So the persistent player (in DontDestroyOnLoad)
// always wins, and any other reference is swapped for it.
//
// Called every physics step by the enemy AIs: the current player is cached,
// so it only searches again once that one is gone or untagged.
public static class PlayerLocator
{
    private static Transform current;

    public static Transform Current
    {
        get
        {
            if (current == null || !current.CompareTag("Player")) current = Find();
            return current;
        }
    }

    public static bool Refresh(ref Transform player)
    {
        Transform real = Current;
        if (real != null) player = real;
        return player != null;
    }

    private static Transform Find()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return null;

        // Persistent one first: DontDestroyOnLoad objects live in their own
        // scene, which has no build index.
        foreach (GameObject p in players)
        {
            if (p.scene.buildIndex == -1) return p.transform;
        }
        return players[0].transform;
    }
}
