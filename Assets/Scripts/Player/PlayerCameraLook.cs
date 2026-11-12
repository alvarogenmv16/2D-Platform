using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// Look down: holding down (S / down arrow on the keyboard, right stick down
// on a gamepad) while standing still on the ground pans the camera down a
// bit, so the player can see what's below before dropping. Letting go -
// or walking, jumping, falling - brings it back.
//
// Lives on the Player, which survives scene loads, and drives whatever
// CinemachinePositionComposer the current scene has (every gameplay scene
// has one), by shifting its TargetOffset. The scene's own offset is kept
// and restored, so per-scene camera framing isn't lost.
public class PlayerCameraLook : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;

    [SerializeField] private float lookDownDistance = 4f;
    // How long down must be held before the camera starts moving, so a quick
    // tap (or brushing the stick) doesn't shake the view.
    [SerializeField] private float holdDelay = 0.25f;
    // Units per second the offset moves (down and back up).
    [SerializeField] private float panSpeed = 8f;
    [SerializeField, Range(0.1f, 1f)] private float inputThreshold = 0.5f;

    private CinemachinePositionComposer composer;
    private Vector3 baseOffset;
    private float lookAmount;   // current extra offset, 0..lookDownDistance
    private float heldTime;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
    }

    private void LateUpdate()
    {
        // A new scene brings its own camera; the old one is gone.
        if (composer == null)
        {
            composer = FindFirstObjectByType<CinemachinePositionComposer>();
            if (composer == null) return;
            baseOffset = composer.TargetOffset;
            lookAmount = 0f;
        }

        bool wantsLook = WantsToLookDown();
        heldTime = wantsLook ? heldTime + Time.deltaTime : 0f;

        float target = heldTime >= holdDelay ? lookDownDistance : 0f;
        lookAmount = Mathf.MoveTowards(lookAmount, target, panSpeed * Time.deltaTime);

        composer.TargetOffset = baseOffset + Vector3.down * lookAmount;
    }

    private bool WantsToLookDown()
    {
        if (playerMovement == null || !playerMovement.enabled) return false;
        if (!playerMovement.IsGrounded) return false;

        InputSystem_Actions input = playerMovement.InputActions;
        if (input == null) return false;

        // Keyboard: the Move action (S / down arrow), only while not walking.
        InputAction move = input.Player.Move;
        Vector2 moveValue = move.ReadValue<Vector2>();
        bool keyboardDown = move.activeControl != null && move.activeControl.device is Keyboard
                            && moveValue.y < -inputThreshold && Mathf.Abs(moveValue.x) < 0.1f;

        // Gamepad: right stick down (the Look action), while the left stick
        // isn't walking. Look is also bound to the mouse, hence the device check.
        InputAction look = input.Player.Look;
        Vector2 lookValue = look.ReadValue<Vector2>();
        bool gamepadDown = look.activeControl != null && look.activeControl.device is Gamepad
                           && lookValue.y < -inputThreshold && Mathf.Abs(moveValue.x) < 0.1f;

        return keyboardDown || gamepadDown;
    }

    private void OnDisable()
    {
        if (composer != null) composer.TargetOffset = baseOffset;
        lookAmount = 0f;
        heldTime = 0f;
    }
}
