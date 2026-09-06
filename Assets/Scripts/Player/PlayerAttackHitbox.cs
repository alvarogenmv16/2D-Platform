using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private Vector2 attackSize = new Vector2(2f, 2f);
    [SerializeField] private float attackOffsetX = 1.75f;
    [SerializeField] private float attackOffsetY = 1f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private LayerMask enemyLayer;

    private SpriteRenderer spriteRenderer;

    // =========================
    // START
    // =========================
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // =========================
    // FUNCTIONS
    // =========================

    // Called via an Animation Event placed on the Player_Attack clip,
    // at the exact frame the hit should register — not on the frame the
    // attack begins, so the damage is tied to the visual impact.
    public void OnAttackHit()
    {
        Vector2 attackPointPosition = GetAttackPointPosition();
        // Box instead of circle: a circle centered on the attack point misses
        // enemies standing very close (just outside its near edge) — a box
        // covers the whole front arc uniformly, closer to the actual swing shape.
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackPointPosition, attackSize, 0f, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage, attackPointPosition);
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
    }
}