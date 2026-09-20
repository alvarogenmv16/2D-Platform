using UnityEngine;

// A thin relay that exists purely because Animation Events can only call
// methods on components attached to the SAME GameObject as the Animator.
// FlyRelicAI lives on the root (it needs the Rigidbody2D/Collider2D), so
// this relay on Visuals forwards the call to it.
public class FlyRelicAttackRelay : MonoBehaviour
{
    [SerializeField] private FlyRelicAI flyRelicAI;

    // Called via an Animation Event on FlyRelicAttack.anim, at the exact
    // frame the beam sprite (fly_relic_attack2) appears.
    public void OnBeamFire()
    {
        if (flyRelicAI != null)
        {
            flyRelicAI.OnBeamFire();
        }
    }
}
