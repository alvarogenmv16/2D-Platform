using UnityEngine;

// A thin relay that exists purely because Animation Events can only call
// methods on components attached to the SAME GameObject as the Animator.
// WardenAI lives on the root, so this relay on Visuals forwards the attack
// events to it - same as BeastAttackRelay.
public class WardenAttackRelay : MonoBehaviour
{
    [SerializeField] private WardenAI wardenAI;

    // Called via an Animation Event on WardenPunch.anim, on the frame the
    // fist goes up (Warden_Punch).
    public void OnPunch()
    {
        if (wardenAI != null)
        {
            wardenAI.OnPunch();
        }
    }

    // Called via an Animation Event on WardenSweep.anim, on the frame the
    // arm thrusts out (Warden_Spikes).
    public void OnSweep()
    {
        if (wardenAI != null)
        {
            wardenAI.OnSweep();
        }
    }
}
