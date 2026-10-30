using UnityEngine;

// A thin relay that exists purely because Animation Events can only call
// methods on components attached to the SAME GameObject as the Animator.
// MawAI lives on the root (it needs the Rigidbody2D/Collider2D), so this
// relay on Visuals forwards the call to it.
public class MawAttackRelay : MonoBehaviour
{
    [SerializeField] private MawAI mawAI;

    // Called via an Animation Event on MawBite.anim, at the exact frame the
    // bite sprite (maw_bite0) appears.
    public void OnBite()
    {
        if (mawAI != null)
        {
            mawAI.OnBite();
        }
    }
}
