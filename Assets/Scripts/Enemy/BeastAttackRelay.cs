using UnityEngine;

// A thin relay that exists purely because Animation Events can only call
// methods on components attached to the SAME GameObject as the Animator.
// BeastAI lives on the root (it needs the Rigidbody2D/Collider2D), so this
// relay on Visuals forwards the attack events to it - same as MawAttackRelay.
public class BeastAttackRelay : MonoBehaviour
{
    [SerializeField] private BeastAI beastAI;

    // Called via an Animation Event on BeastBite.anim, on the frame the
    // jaws snap shut (beast_bite_2).
    public void OnBite()
    {
        if (beastAI != null)
        {
            beastAI.OnBite();
        }
    }
}
