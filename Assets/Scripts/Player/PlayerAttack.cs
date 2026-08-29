using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;
    [SerializeField] private float attackCooldown = 0.4f;

    private bool attackPressedThisFrame;
    private float attackCooldownTimer = 0f;

    // =========================
    // UPDATE (input reading only)
    // =========================
    private void Update()
    {
        // Reuse PlayerMovement's InputSystem_Actions instance instead of
        // creating a second one, to avoid duplicating input handling.
        if (playerMovement.InputActions.Player.Attack.WasPressedThisFrame())
        {
            attackPressedThisFrame = true;
        }
    }

    // =========================
    // FIXED UPDATE (physics-aligned timing)
    // =========================
    private void FixedUpdate()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.fixedDeltaTime;
        }

        if (attackPressedThisFrame && attackCooldownTimer <= 0f)
        {
            if (animator != null)
            {
                animator.SetTrigger("AttackTrigger");
            }

            attackCooldownTimer = attackCooldown;
        }

        // Consume the buffered attack press after processing it
        attackPressedThisFrame = false;
    }
}