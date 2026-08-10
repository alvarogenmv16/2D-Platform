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
    }

    // =========================
    // FIXED UPDATE (physics only)
    // =========================
    private void FixedUpdate()
    {
        CheckGrounded();
        HandleMovement();
        HandleJump();

        // Consume the buffered jump press after processing it
        jumpPressedThisFrame = false;
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
        // Set horizontal velocity while keeping vertical velocity
        rb.linearVelocity = new Vector2(
            moveInput.x * moveSpeed,
            rb.linearVelocity.y
        );
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