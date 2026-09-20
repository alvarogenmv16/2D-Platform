using UnityEngine;
using Unity.Cinemachine;

// A thin relay that exists purely because Animation Events can only call
// methods on components attached to the SAME GameObject as the Animator.
// Triggers the screen shake on the exact frame the impact sprite appears -
// kept separate from RelicAI on purpose, so it doesn't touch the fall
// timing, damage or state machine at all.
public class RelicImpactShake : MonoBehaviour
{
    [SerializeField] private CinemachineImpulseSource impulseSource;

    // Called via an Animation Event on RelicAttack.anim.
    public void OnImpactShake()
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }
    }
}
