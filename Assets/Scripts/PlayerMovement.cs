using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // =========================
    // Movement Settings
    // =========================

    [SerializeField] private float moveSpeed = 5f;

    // =========================
    // Jump Settings
    // =========================

    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpHoldForce = 0.5f;
    [SerializeField] private float maxJumpHoldTime = 0.25f;

    // =========================
    // Components & References
    // =========================

    private Rigidbody2D circle;
    private InputSystem_Actions inputActions;

    // =========================
    // Runtime State
    // =========================

    private float jumpHoldTime = 0f;
    private bool isGrounded = false;


    // =========================
    // Unity Methods
    // =========================

    private void Start()
    {
        // Get the Rigidbody2D attached to the player
        circle = GetComponent<Rigidbody2D>();

        // Create the input actions instance
        inputActions = new InputSystem_Actions();

        // Enable the input actions so they can receive player input
        inputActions.Enable();
    }

    private void Update()
    {
        HandleMovement();
        HandleJump();
    }


    // =========================
    // Movement
    // =========================

    private void HandleMovement()
    {
        // Read the current movement input
        Vector2 movement = inputActions.Player.Move.ReadValue<Vector2>();

        // Create a horizontal movement vector
        Vector3 movementVector = new Vector3(
            movement.x * moveSpeed,
            0,
            0
        );

        // Apply horizontal movement
        transform.Translate(movementVector * Time.deltaTime);
    }


    // =========================
    // Jump
    // =========================

    private void HandleJump()
    {
        // Check if the jump button was pressed this frame
        bool jumpPressed = inputActions.Player.Jump.WasPressedThisFrame();

        // Apply the initial jump impulse only when the player is grounded
        if (jumpPressed && isGrounded)
        {
            circle.AddForce(
                Vector2.up * jumpForce,
                ForceMode2D.Impulse
            );

            // Reset the jump hold timer for the new jump
            jumpHoldTime = 0f;
        }

        // Apply additional force while the jump button is held
        if (inputActions.Player.Jump.IsPressed() && jumpHoldTime < maxJumpHoldTime)
        {
            circle.AddForce(
                Vector2.up * jumpHoldForce,
                ForceMode2D.Force
            );

            // Keep track of how long the button has been held
            jumpHoldTime += Time.deltaTime;
        }
    }


    // =========================
    // Ground Detection
    // =========================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // The player has started touching another collider
        isGrounded = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // The player has stopped touching another collider
        isGrounded = false;
    }
}