using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private Vector2 attackSize = new Vector2(2f, 2f);
    [SerializeField] private float attackOffsetX = 1.75f;
    [SerializeField] private float attackOffsetY = 1f;
    [SerializeField] private Vector2 attackUpSize = new Vector2(2f, 2f);
    [SerializeField] private float attackUpOffsetY = 3f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask breakableLayer; // KeyBlocks and other one-hit breakables
    [SerializeField] private SfxPlayer sfxPlayer;
    [SerializeField] private AudioClip swingSound; // plays on every attack, connects or not
    [SerializeField, Range(0f, 1f)] private float swingVolume = 1f;
    [SerializeField] private PlayerPower playerPower;

    private SpriteRenderer spriteRenderer;

    // The damage before any upgrade - what a "normal" hit is worth in heal
    // power. Captured in Awake, before any DamageUpgradePickup can raise it.
    private float baseDamage = 1f;

    // Heal power per landed hit: 1 with the starting damage, plus this much
    // for every point of damage gained from upgrades, so upgrades also fill
    // the heal bar faster. With 0.5 and a 7-hit bar (PlayerPower):
    // damage 1 = 7 hits, damage 2 = 5 hits, damage 3 = 4 hits.
    [SerializeField] private float powerPerExtraDamage = 0.5f;

    private float PowerPerHit => 1f + (damage / baseDamage - 1f) * powerPerExtraDamage;

    // =========================
    // START
    // =========================
    private void Awake()
    {
        baseDamage = Mathf.Max(0.01f, damage);
    }

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Permanently raises the player's attack damage - called by pickups
    // like DamageUpgradePickup. Not clamped: whoever grants the upgrade is
    // responsible for only doing it once.
    public float Damage => damage;

    public void IncreaseDamage(float amount)
    {
        damage += amount;
    }

    // Called via an Animation Event placed on the Player_Attack clip,
    // at the exact frame the hit should register — not on the frame the
    // attack begins, so the damage is tied to the visual impact.
    public void OnAttackHit()
    {
        sfxPlayer?.Play(swingSound, swingVolume);
        PerformAttack(GetAttackPointPosition(), attackSize);
    }

    // Called via an Animation Event on the PlayerAttackUp clip, the same
    // way OnAttackHit is driven by PlayerAttack's clip.
    public void OnAttackUpHit()
    {
        sfxPlayer?.Play(swingSound, swingVolume);
        PerformAttack(GetAttackUpPointPosition(), attackUpSize);
    }

    // Box instead of circle: a circle centered on the attack point misses
    // enemies standing very close (just outside its near edge) — a box
    // covers the whole front arc uniformly, closer to the actual swing shape.
    private void PerformAttack(Vector2 attackPointPosition, Vector2 size)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackPointPosition, size, 0f, enemyLayer | breakableLayer);

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                if (enemyHealth.IsDead) continue;

                enemyHealth.TakeDamage(damage, attackPointPosition);
                playerPower?.AddPower(PowerPerHit);
                continue;
            }

            IBreakable breakable = hit.GetComponent<IBreakable>();
            if (breakable != null)
            {
                breakable.Break();
            }
        }
    }

    private Vector2 GetAttackPointPosition()
    {
        // The player flips using SpriteRenderer.flipX (not localScale like
        // the enemy), so a child Transform's position wouldn't move to the
        // other side automatically. We read the same flipX flag here and
        // mirror the offset manually.
        float direction = spriteRenderer.flipX ? -1f : 1f;
        return (Vector2)transform.position + new Vector2(attackOffsetX * direction, attackOffsetY);
    }

    // Straight up, so it doesn't need to mirror with flipX like the
    // horizontal attack point does.
    private Vector2 GetAttackUpPointPosition()
    {
        return (Vector2)transform.position + new Vector2(0f, attackUpOffsetY);
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(GetAttackPointPosition(), attackSize);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(GetAttackUpPointPosition(), attackUpSize);
    }
}