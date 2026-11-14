using UnityEngine;

// Keeps an enemy's player reference pointing at the player that's actually
// alive. The player survives scene changes (SceneFader / PlayerDeathHandler
// keep it with DontDestroyOnLoad) and replaces the scene's own copy, which is
// untagged and destroyed - so a reference assigned in the Inspector, or found
// in Start before that swap, ends up pointing at a destroyed object. Called
// every physics step: it costs nothing while the reference is valid and only
// searches again (by tag) once it's lost.
public static class PlayerLocator
{
    public static bool Refresh(ref Transform player)
    {
        if (player != null) return true;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
        return player != null;
    }
}
