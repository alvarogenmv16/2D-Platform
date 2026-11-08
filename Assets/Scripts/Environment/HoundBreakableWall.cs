using Unity.Cinemachine;
using UnityEngine;

// A cracked wall the player's sword can't break - only a charging hound
// with HoundAI.diesOnWallImpact on can, by slamming into it (which also
// kills that hound). Deliberately NOT an IBreakable: that interface is what
// PlayerAttackHitbox breaks, and the whole point is that the player has to
// use the hound as the tool.
//
// Setup: put it on the Ground layer (HoundAI only detects walls through its
// groundLayer mask) with a solid, non-trigger Collider2D.
[RequireComponent(typeof(Collider2D))]
public class HoundBreakableWall : MonoBehaviour
{
    [SerializeField] private AudioClip breakSound;
    [SerializeField] private CinemachineImpulseSource impulseSource; // optional screen shake

    private bool isBroken = false;

    public void Break()
    {
        if (isBroken) return;
        isBroken = true;

        // PlayClipAtPoint, not an AudioSource on this object: the wall is
        // destroyed right away and would cut its own sound off.
        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        impulseSource?.GenerateImpulse();

        Destroy(gameObject);
    }
}
