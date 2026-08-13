# Player Movement

## Overview

`PlayerMovement` is responsible for controlling the player's movement and aerial mobility.

The script uses Unity's **Input System** to read player input and a `Rigidbody2D` to apply physics-based movement. It handles:

* Horizontal movement.
* Player facing direction.
* Ground detection.
* Multiple jumps.
* Variable jump height while the jump button is held.
* Dash movement with duration and cooldown.
* Input buffering between `Update` and `FixedUpdate`.
* Visualization of the ground detection area in the Unity Editor.

The script requires a `Rigidbody2D` component through `[RequireComponent(typeof(Rigidbody2D))]`.

---

## Unity lifecycle

### `Start()`

Initializes the components required by the movement system.

The method:

1. Retrieves the `Rigidbody2D` attached to the player.
2. Creates an instance of `InputSystem_Actions`.
3. Enables the input actions.

The input system is initialized here rather than in `Awake`, so all movement-related input is ready before the first `Update` call.

---

### `Update()`

Reads player input and stores it for use by the physics system.

`Update` is intentionally limited to **input reading** rather than directly modifying the `Rigidbody2D`.

The method:

* Reads the player's movement input and stores it in `moveInput`.
* Detects when the jump button was pressed during the current frame.
* Detects when the dash button was pressed during the current frame.

The jump and dash presses are stored in `jumpPressedThisFrame` and `dashPressedThisFrame`. This acts as a small input buffer so a button press is not lost when the render frame and physics frame do not occur at exactly the same time.

The actual movement and physics operations are performed later by `FixedUpdate()`.

---

### `FixedUpdate()`

Controls the main physics update loop of the player.

The functions are executed in the following order:

```text
CheckGrounded()
      ↓
HandleMovement()
      ↓
HandleJump()
      ↓
HandleDash()
```

After these operations, the buffered jump and dash inputs are consumed by resetting:

```csharp
jumpPressedThisFrame = false
dashPressedThisFrame = false
```

Using `FixedUpdate` for these operations ensures that changes to the `Rigidbody2D` are synchronized with Unity's physics system.

---

# Functions

## `CheckGrounded()`

Determines whether the player is currently standing on a valid ground surface.

The method uses `Physics2D.OverlapCircle()` at the position defined by `groundCheck`. Only colliders belonging to `groundLayer` are considered ground.

```text
groundCheck position
       ↓
OverlapCircle
       ↓
isGrounded
```

The method also manages the jump counter.

`jumpCount` is reset **only when the player transitions from airborne to grounded**:

```text
Airborne → Grounded
        ↓
jumpCount = 0
```

It does not reset the counter every physics frame while the player remains grounded. This is important because the player may still be detected as grounded for a short time immediately after initiating a jump.

`wasGroundedLastFrame` stores the previous grounded state and allows the method to detect this transition.

---

## `HandleMovement()`

Handles horizontal player movement.

If the player is currently dashing, normal movement is ignored:

```text
isDashing == true
        ↓
No normal movement
```

Otherwise, the method directly sets the horizontal component of `Rigidbody2D.linearVelocity` using the movement input and `moveSpeed`.

The vertical velocity is preserved, allowing gravity and jumping to continue affecting the player independently of horizontal movement.

The method also updates `facingDirection`:

```csharp
moveInput.x > 0  → facingDirection =  1
moveInput.x < 0  → facingDirection = -1
```

This direction is later used by `HandleDash()` to determine the direction of the dash.

---

## `HandleDash()`

Controls the player's dash state, duration, and cooldown.

### Cooldown

The cooldown timer is reduced using `Time.fixedDeltaTime`.

While `dashCooldownTimer` is greater than zero, another dash cannot be started.

### Starting the dash

A dash starts when all of the following conditions are met:

```csharp
dashPressedThisFrame
        AND
!isDashing
        AND
dashCooldownTimer <= 0
```

When the dash begins:

* `isDashing` becomes `true`.
* `dashTimer` is reset.
* The cooldown timer is initialized.
* The player's horizontal velocity is set according to `facingDirection` and `dashSpeed`.

The vertical velocity is preserved.

### Ending the dash

While `isDashing` is active, `dashTimer` increases every physics step.

Once:

```csharp
dashTimer >= dashDuration
```

the dash ends and `isDashing` becomes `false`.

The dash therefore has a fixed duration rather than continuing until the player releases a button.

---

## `HandleJump()`

Controls both the initial jump impulse and the additional force used to create variable jump height.

### Starting a jump

A new jump can begin when:

```csharp
jumpPressedThisFrame
AND
jumpCount < maxJumps
```

Before applying the new jump impulse, the vertical velocity is reset to zero.

This prevents the player's existing vertical velocity from stacking with the new jump impulse and producing inconsistent jump heights.

The jump force is selected from the `jumpForces` array according to the current `jumpCount`.

For example:

```csharp
jumpCount = 0 → first jump
jumpCount = 1 → second jump
```

If `maxJumps` is greater than the number of entries in `jumpForces`, the last available force is reused.

The selected force is applied as an impulse using:

```csharp
ForceMode2D.Impulse
```

After the jump starts:

* `jumpHoldTime` is reset.
* `isJumpHeld` becomes `true`.
* `jumpCount` is increased.

---

### Variable jump height

After the initial jump, the script can continue applying upward force while the jump button remains pressed.

This only happens when:

```csharp
isJumpHeld
AND
Jump.IsPressed()
AND
jumpHoldTime < maxJumpHoldTime
```

The additional force is applied using:

```csharp
ForceMode2D.Force
```

This creates a variable jump height:

```text
Short button press → shorter jump
Longer button hold → higher jump
```

The additional force can only be applied during the configured `maxJumpHoldTime`.

If the player releases the button or the maximum hold time is reached, `isJumpHeld` becomes `false` and the current jump can no longer be extended.

This state is reset for every new jump, preventing a held jump button from continuously applying force when the player is not actually starting a jump.

---

## `OnDestroy()`

Cleans up the input system when the player object is destroyed.

The method:

1. Disables the input actions.
2. Disposes of the `InputSystem_Actions` instance.

This prevents the input action object from remaining active after the `PlayerMovement` component has been destroyed.

---

## `OnDrawGizmosSelected()`

Provides a visual debugging aid inside the Unity Editor.

When the player GameObject is selected, the method draws a wire sphere at the `groundCheck` position using `groundCheckRadius`.

The color indicates the current grounded state:

```text
Green → player detected as grounded
Red   → player not detected as grounded
```

If `groundCheck` has not been assigned, the method exits without drawing anything.

This function has no effect on the actual gameplay physics; it is only used to visualize the ground detection area.

---

# Main state variables

| Variable               | Purpose                                                            |
| ---------------------- | ------------------------------------------------------------------ |
| `moveSpeed`            | Controls normal horizontal movement speed.                         |
| `facingDirection`      | Stores the direction the player is facing and is used for dashing. |
| `dashSpeed`            | Horizontal speed applied during a dash.                            |
| `dashDuration`         | Maximum duration of a dash.                                        |
| `dashCooldown`         | Time that must pass before another dash can start.                 |
| `maxJumps`             | Maximum number of jumps allowed before landing.                    |
| `jumpForces`           | Initial impulse used for each jump in the jump chain.              |
| `jumpHoldForce`        | Additional upward force while the jump button is held.             |
| `maxJumpHoldTime`      | Maximum duration for which the current jump can be extended.       |
| `groundCheck`          | Transform used as the center of the ground detection check.        |
| `groundCheckRadius`    | Radius of the ground detection area.                               |
| `groundLayer`          | Layers considered valid ground.                                    |
| `moveInput`            | Cached movement input read during `Update`.                        |
| `jumpCount`            | Number of jumps already performed since the last landing.          |
| `isGrounded`           | Indicates whether the player is currently detected on the ground.  |
| `isDashing`            | Indicates whether the dash is currently active.                    |
| `jumpPressedThisFrame` | Buffered jump input waiting to be processed by physics.            |
| `dashPressedThisFrame` | Buffered dash input waiting to be processed by physics.            |
| `wasGroundedLastFrame` | Previous grounded state, used to detect landing.                   |

---

# Function interaction

The main interaction between the functions can be summarized as follows:

```text
Update()
  │
  ├── Read movement input
  ├── Buffer jump input
  └── Buffer dash input
          │
          ▼
FixedUpdate()
  │
  ├── CheckGrounded()
  │     └── Updates isGrounded / jumpCount
  │
  ├── HandleMovement()
  │     └── Updates horizontal velocity / facingDirection
  │
  ├── HandleJump()
  │     └── Applies jump impulse and jump-hold force
  │
  └── HandleDash()
        └── Applies dash velocity and manages dash state
```

The most important dependency is the relationship between `Update()` and `FixedUpdate()`. `Update()` captures player input, while `FixedUpdate()` consumes that input and performs the corresponding physics operations.

The movement system therefore follows the general flow:

```text
Player Input
     ↓
Update()
     ↓
Input Buffer
     ↓
FixedUpdate()
     ↓
Movement / Jump / Dash
     ↓
Rigidbody2D
     ↓
Player Physics
```

## Movement and dash relationship

`HandleMovement()` and `HandleDash()` share the `isDashing` state.

While `isDashing` is `true`, `HandleMovement()` immediately returns. This prevents normal horizontal movement from overwriting the velocity established by the dash.

```text
Normal movement
      │
      ├── isDashing = false → movement applied
      │
      └── isDashing = true  → movement ignored
```

`facingDirection`, meanwhile, is updated by normal movement and subsequently determines the horizontal direction of the dash.

## Jump and ground relationship

`CheckGrounded()` is responsible for resetting `jumpCount` after a landing.

This creates the following jump cycle:

```text
Grounded
   ↓
jumpCount = 0
   ↓
First jump
   ↓
jumpCount = 1
   ↓
Second jump
   ↓
jumpCount = 2
   ↓
Land
   ↓
jumpCount = 0
```

This allows the script to support double jumps or any other number of jumps defined by `maxJumps`.

## Overall responsibility

`PlayerMovement` acts as the central controller for the player's **locomotion and aerial movement**. It does not appear to manage health, damage, animation, or combat directly. Those responsibilities can therefore be handled by other components while `PlayerMovement` focuses on translating player input into physics-based movement.
