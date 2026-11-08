using Unity.Cinemachine;
using UnityEngine;

// Roar of the final boss (Beast / Morthos): scream + camera shake, fired by
// an Animation Event (OnRoar) on the frame the jaws open wide. Lives on
// Visuals, next to the Animator, because Animation Events can only call
// components on the Animator's own GameObject - same relay idea as
// RelicImpactShake. The sound player and impulse source live on the root.
//
// The idle loop is much shorter than the scream, so a new roar is skipped
// while the previous one is still playing (or within minRoarInterval, if
// that's longer) - otherwise every loop would stack another scream on top.
public class BeastRoar : MonoBehaviour
{
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip roarSound;
    [SerializeField, Range(0f, 1f)] private float roarVolume = 1f;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    // 0 = just don't overlap: wait for the scream clip itself to finish.
    [SerializeField] private float minRoarInterval = 0f;

    private float nextRoarTime = float.NegativeInfinity;

    // Called via an Animation Event on BeastIdle.anim (and any other clip
    // that should roar).
    public void OnRoar()
    {
        if (Time.time < nextRoarTime) return;

        float clipLength = roarSound != null ? roarSound.length : 0f;
        nextRoarTime = Time.time + Mathf.Max(minRoarInterval, clipLength);

        // Explicit null checks instead of ?. (Unity fake-null, see KeyBlock).
        if (sfxPlayer != null) sfxPlayer.Play(roarSound, roarVolume);
        if (impulseSource != null) impulseSource.GenerateImpulse();
    }
}
