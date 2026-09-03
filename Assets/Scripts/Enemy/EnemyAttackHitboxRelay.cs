using UnityEngine;

// A thin relay that exists purely because Animation Events can only call
// methods on components attached to the SAME GameObject as the Animator.
// EnemyWeapon lives on WeaponPivot (a child), so this relay on Visuals
// forwards the call to it.
public class EnemyAttackHitboxRelay : MonoBehaviour
{
    [SerializeField] private EnemyWeapon weapon;

    // Called via an Animation Event on the Enemy_Attack clip, at the
    // exact frame the tongue reaches full extension.
    public void OnTongueHit()
    {
        if (weapon != null)
        {
            weapon.TryHitPlayer();
        }
    }
}