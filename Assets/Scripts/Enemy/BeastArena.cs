using UnityEngine;

// TempleScene: seals off the area where Morthos is fought. The walls start
// hidden; they appear the moment the Beast starts waking up (its roar) so the
// player can't leave the fight, and disappear again when it dies.
//
// Walls are plain scene objects (sprite + solid BoxCollider2D) dragged into
// arenaWalls. The Beast is found in the scene if left empty.
public class BeastArena : MonoBehaviour
{
    [SerializeField] private BeastAI beast;
    [SerializeField] private GameObject[] arenaWalls;

    private EnemyHealth beastHealth;

    private void Awake()
    {
        if (beast == null) beast = FindFirstObjectByType<BeastAI>(FindObjectsInactive.Include);
        if (beast != null) beastHealth = beast.GetComponent<EnemyHealth>();

        SetWallsActive(false);
    }

    private void OnEnable()
    {
        if (beast != null) beast.OnAwakened.AddListener(CloseArena);
        if (beastHealth != null) beastHealth.OnDied.AddListener(OpenArena);
    }

    private void OnDisable()
    {
        if (beast != null) beast.OnAwakened.RemoveListener(CloseArena);
        if (beastHealth != null) beastHealth.OnDied.RemoveListener(OpenArena);
    }

    private void CloseArena() => SetWallsActive(true);

    private void OpenArena() => SetWallsActive(false);

    private void SetWallsActive(bool active)
    {
        foreach (GameObject wall in arenaWalls)
        {
            if (wall != null) wall.SetActive(active);
        }
    }
}
