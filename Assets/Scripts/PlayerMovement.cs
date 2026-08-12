using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    // =========================
    // VARIABLES
    // =========================
    // Movement settings
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float facingDirection = 1f; // 1 for right, -1 for left
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private bool dashPressedThisFrame;
    private float dashCooldownTimer = 0f;


    // Jump settings
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpHoldForce = 0.5f;
    [SerializeField] private float maxJumpHoldTime = 0.25f;

    // Ground detection settings
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    // Components
    private Rigidbody2D rb;
    private InputSystem_Actions inputActions;

    // Player state
    private Vector2 moveInput;
    private bool jumpPressedThisFrame;
    private float jumpHoldTime = 0f;
    private bool isGrounded = false;

    // =========================
    // START
    // =========================
    private void Start()
    {
        // Get the Rigidbody2D attached to the player
        rb = GetComponent<Rigidbody2D>();

        // Create the input actions instance
        inputActions = new InputSystem_Actions();

        // Enable the input actions
        inputActions.Enable();
    }

    // =========================
    // UPDATE (input reading only)
    // =========================
    private void Update()
    {
        // Cache movement input for use in FixedUpdate
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // Latch the jump press so it isn't missed between physics steps
        if (inputActions.Player.Jump.WasPressedThisFrame())
        {
            jumpPressedThisFrame = true;
        }

        // Dash movement
        if (inputActions.Player.Dash.WasPressedThisFrame())
        {
            dashPressedThisFrame = true;
        }
    }

    // =========================
    // FIXED UPDATE (physics only)
    // =========================
    private void FixedUpdate()
    {
        CheckGrounded();
        HandleMovement();
        HandleJump();
        HandleDash();

        // Consume the buffered jump press after processing it
        jumpPressedThisFrame = false;
        // Consume the buffered dash after processing it
        dashPressedThisFrame = false;
    }

    // =========================
    // FUNCTIONS
    // =========================
    private void CheckGrounded()
    {
        // Check for ground overlap at the groundCheck position
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void HandleMovement()
    {
        if (isDashing)
        {
            return;
        }
        // Set horizontal velocity while keeping vertical velocity
        rb.linearVelocity = new Vector2(
            moveInput.x * moveSpeed,
            rb.linearVelocity.y
        );
        // Update facing direction based on movement input
        if (moveInput.x > 0)
        {
            facingDirection = 1f;
        }
        else if (moveInput.x < 0)
        {
            facingDirection = -1f;
        }
        
    }

    private void HandleDash()
    {
        // Reduce cooldown timer
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.fixedDeltaTime;
        }

        // Start dash
        if (dashPressedThisFrame && !isDashing && dashCooldownTimer <= 0f)
        {
            isDashing = true;
            dashTimer = 0f;
            dashCooldownTimer = dashCooldown;

            rb.linearVelocity = new Vector2(
                facingDirection * dashSpeed,
                rb.linearVelocity.y
            );
        }

        // Track dash duration
        if (isDashing)
        {
            dashTimer += Time.fixedDeltaTime;

            if (dashTimer >= dashDuration)
            {
                isDashing = false;
            }
        }
    }

    private void HandleJump()
    {
        // Apply the initial jump impulse only if the player is grounded
        if (jumpPressedThisFrame && isGrounded)
        {
            rb.AddForce(
                Vector2.up * jumpForce,
                ForceMode2D.Impulse
            );

            // Reset the jump hold timer for the new jump
            jumpHoldTime = 0f;
        }

        // Apply additional force while the jump button is held
        if (inputActions.Player.Jump.IsPressed() &&
            jumpHoldTime < maxJumpHoldTime)
        {
            rb.AddForce(
                Vector2.up * jumpHoldForce,
                ForceMode2D.Force
            );

            // Track how long the jump button has been held
            jumpHoldTime += Time.fixedDeltaTime;
        }
    }

    private void OnDestroy()
    {
        // Clean up the input actions when this object is destroyed
        inputActions.Disable();
        inputActions.Dispose();
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        // Visualize the ground check radius in the editor
        if (groundCheck == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}