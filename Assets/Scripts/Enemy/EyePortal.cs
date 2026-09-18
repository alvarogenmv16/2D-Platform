using UnityEngine;

// Spawned by EyeAI during the Phase 1 rock barrage, at a random point along
// the arena ceiling. Its own Animator plays a one-shot open/close flipbook
// (no parameters or transitions needed — it's the only state, so it just
// runs from the moment this GameObject is instantiated); an Animation Event
// mid-clip spawns the falling EyeRock. Self-destructs once its own clip ends.
public class EyePortal : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private Animator animator;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private float minRockFallSpeed = 7f;
    [SerializeField] private float maxRockFallSpeed = 14f;
    [SerializeField] private float lifetimeSeconds = 2f; // safety net if the clip length can't be read

    [Header("Sound")]
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip openSound;

    private float floorY;

    // =========================
    // START
    // =========================
    private void Start()
    {
        sfxPlayer?.Play(openSound);

        float clipLength = animator != null ? animator.GetCurrentAnimatorStateInfo(0).length : 0f;
        Destroy(gameObject, clipLength > 0f ? clipLength : lifetimeSeconds);
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Called by EyeAI right after instantiating this portal.
    public void SetFloorY(float y)
    {
        floorY = y;
    }

    // Called via an Animation Event at the frame the portal is fully open.
    public void OnPortalRelease()
    {
        if (rockPrefab == null) return;

        GameObject rockObject = Instantiate(rockPrefab, transform.position, Quaternion.identity);
        EyeRock rock = rockObject.GetComponent<EyeRock>();

        if (rock != null)
        {
            rock.Launch(Random.Range(minRockFallSpeed, maxRockFallSpeed), floorY);
        }
    }
}
