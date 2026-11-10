using UnityEngine;
using UnityEngine.Events;

// Marks the floor of the room below the arena, where the player lands after
// EyeFloorCrumble drops them through. EyeAI waits for this to fire before
// spawning Phase 2's platforms — doing it any earlier (e.g. right when the
// crumble finishes) would mean the platforms already exist while the player
// is still mid-fall, so they'd land on one instead of reaching the floor.
[RequireComponent(typeof(Collider2D))]
public class EyePhase2FloorTrigger : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    public UnityEvent OnPlayerReachedFloor;

    private bool hasTriggered = false;

    // =========================
    // START
    // =========================
    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // =========================
    // FUNCTIONS
    // =========================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;
        OnPlayerReachedFloor?.Invoke();
    }
}
