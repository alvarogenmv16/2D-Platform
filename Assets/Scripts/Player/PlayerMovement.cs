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
    private float facingDirection = 1f; // 1 for right, -1 for left
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private Animator animator;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private bool dashPressedThisFrame;
    private float dashCooldownTimer = 0f;
    private int jumpCount = 0;

    // Jump settings
    // One entry per jump in the chain: index 0 = first (grounded) jump,
    // index 1 = second (air) jump, etc. Size this array to match maxJumps.
    [SerializeField] private float[] jumpForces = { 5f, 4f };
    [SerializeField] private float jumpHoldForce = 0.5f;
    [SerializeField] private float maxJumpHoldTime = 0.25f;

    // Ground detection settings
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component

    // Components
    private Rigidbody2D rb;
    private InputSystem_Actions inputActions;

    // Player state
    private Vector2 moveInput;
    private bool jumpPressedThisFrame;
    private float jumpHoldTime = 0f;
    private bool isJumpHeld = false; // true only while extending the CURRENT jump
    private bool isGrounded = false;
    private bool wasGroundedLastFrame = true;

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
        UpdateAnimatorParameters();

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

        // Only reset the jump counter on the frame the player actually LANDS
        // (transition from airborne to grounded), not on every grounded frame.
        // This avoids resetting the counter mid-takeoff, before physics has
        // moved the Rigidbody out of the ground check radius.
        if (isGrounded && !wasGroundedLastFrame)
        {
            jumpCount = 0;
        }

        wasGroundedLastFrame = isGrounded;
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

        // Flip the sprite based on facing direction. Done once here,
        // outside the if/else, so it also applies correctly when
        // moveInput.x == 0 (keeps the last known facing).
        spriteRenderer.flipX = facingDirection < 0;
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
        // Apply the initial jump impulse only if jumps remain
        if (jumpPressedThisFrame && jumpCount < maxJumps)
        {
            // Cancel any existing vertical velocity before applying the new
            // impulse. Without this, jumping again while still moving upward
            // from the previous jump STACKS velocity, causing wildly
            // inconsistent jump heights depending on timing.
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            // Pick the force for this jump in the chain (first jump, second
            // jump, etc). Falls back to the last defined value if the array
            // is shorter than maxJumps, so a missing entry never breaks.
            float currentJumpForce = jumpForces[Mathf.Min(jumpCount, jumpForces.Length - 1)];

            rb.AddForce(
                Vector2.up * currentJumpForce,
                ForceMode2D.Impulse
            );

            // Fire the correct Animator trigger for THIS jump in the chain,
            // exactly once, at the moment the impulse is applied. Using a
            // Trigger (instead of the persistent JumpCount condition) avoids
            // it staying "true" for the rest of the airtime and causing the
            // Animator to bounce between DoubleJump and Fall repeatedly.
            if (animator != null)
            {
                animator.SetTrigger(jumpCount == 0 ? "JumpTrigger" : "DoubleJumpTrigger");
            }

            // Start a fresh hold window for THIS jump
            jumpHoldTime = 0f;
            isJumpHeld = true;

            // Increase jump counter
            jumpCount++;
        }

        // Apply additional force while the jump button is held, but only
        // while we're inside an active jump's hold window. This prevents
        // hold force from leaking in when the button happens to be held
        // outside of a real jump (e.g. holding it before ever jumping).
        if (isJumpHeld && inputActions.Player.Jump.IsPressed() && jumpHoldTime < maxJumpHoldTime)
        {
            rb.AddForce(
                Vector2.up * jumpHoldForce,
                ForceMode2D.Force
            );

            // Track how long the jump button has been held
            jumpHoldTime += Time.fixedDeltaTime;
        }
        else
        {
            // Button released or hold window expired: stop extending this jump
            isJumpHeld = false;
        }
    }

    // Pushes the current physics/jump state to the Animator so its state
    // machine can decide which clip to play (Idle/Jump/DoubleJump/Fall).
    // Called at the end of FixedUpdate, after HandleJump has updated
    // jumpCount and physics has updated rb.linearVelocity for this step.
    private void UpdateAnimatorParameters()
    {
        if (animator == null) return;

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetInteger("JumpCount", jumpCount);
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        animator.SetBool("IsDashing", isDashing);
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