using UnityEngine;

// Animation Events can only call methods on components on the same GameObject
// as the Animator (Visuals). BossAI (state machine, root) and the scythe's
// EnemyWeapon (WeaponPivot) both live elsewhere, so this relay forwards to
// them — same reasoning as EnemyAttackHitboxRelay, extended to the boss's
// second attack type.
public class BossAttackRelay : MonoBehaviour
{
    [SerializeField] private BossAI bossAI;
    [SerializeField] private EnemyWeapon scytheWeapon;

    // Called via an Animation Event on the BossScytheAttack clip, at the impact frame.
    public void OnScytheHit()
    {
        if (scytheWeapon != null)
        {
            scytheWeapon.TryHitPlayer();
        }
    }

    // Called via an Animation Event on the BossSpikeAttack clip, at the release frame.
    public void OnSpikeAttackRelease()
    {
        if (bossAI != null)
        {
            bossAI.SpawnSpikeRow();
        }
    }
}
