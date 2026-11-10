using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField, Range(0f, 1f)] private float upInputThreshold = 0.5f;

    private bool attackPressedThisFrame;
    private bool attackUpBuffered;
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
            // Read whichever direction is held at the moment of the press,
            // not at FixedUpdate time, so a quick up+attack tap isn't missed.
            attackUpBuffered = playerMovement.InputActions.Player.Move.ReadValue<Vector2>().y > upInputThreshold;
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
                animator.SetTrigger(attackUpBuffered ? "AttackUpTrigger" : "AttackTrigger");
            }

            attackCooldownTimer = attackCooldown;
        }

        // Consume the buffered attack press after processing it
        attackPressedThisFrame = false;
    }
}