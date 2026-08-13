# EnemyAI.cs

## Overview

`EnemyAI` is responsible for controlling the enemy's **high-level behavior**.

The script uses a simple state machine with three possible states:

* `Idle`: the player is outside the enemy's detection range.
* `Chasing`: the player is detected but is outside attack range.
* `Attacking`: the player is close enough to attack.

`EnemyAI` does not directly implement movement or attack mechanics. Instead, it delegates those responsibilities to the `EnemyMovement` and `EnemyAttack` components attached to the same GameObject.

The overall responsibility can therefore be summarized as:

```text
EnemyAI
  │
  ├── Decide what the enemy should do
  │
  ├── EnemyMovement → Move / Stop
  │
  └── EnemyAttack   → Attack
```

---

## State machine

The enemy behavior is controlled by the private `EnemyState` enum:

```text
Idle
Chasing
Attacking
```

The state is determined entirely by the player's distance from the enemy.

```text
                    Player outside detection range
                              │
                              ▼
                           [Idle]
                              │
                    Player enters detection range
                              │
                              ▼
                         [Chasing]
                              │
                     Player enters attack range
                              │
                              ▼
                        [Attacking]
```

When the player moves away, the same logic works in reverse:

```text
Attacking → Chasing → Idle
```

The state is recalculated every physics update.

---

# Unity lifecycle

## `Start()`

Initializes the references required by the AI system.

The method obtains the two components responsible for the enemy's movement and attacks:

```text
EnemyMovement movement
EnemyAttack   attack
```

Both are retrieved from the same GameObject using `GetComponent()`.

The method also initializes the player reference.

If `player` has already been assigned through the Inspector, that reference is kept. Otherwise, the script attempts to find a GameObject tagged `"Player"`.

The player lookup therefore follows this logic:

```text
Player assigned in Inspector?
        │
     ┌──┴──┐
    Yes    No
     │      │
     │      ▼
     │  Find "Player" tag
     │      │
     └──────┴──────► player reference
```

If no object with the `"Player"` tag exists, `player` remains `null`.

---

## `FixedUpdate()`

Runs the AI logic during Unity's physics update cycle.

If no player reference is available, the method immediately returns:

```text
player == null
     ↓
  return
```

Otherwise, three operations are performed in order:

```text
UpdateState()
     ↓
UpdateFacing()
     ↓
HandleStateBehavior()
```

This order is important because each step provides information used by the next one.

First, the enemy determines what state it should be in. Then it determines which direction it should face. Finally, it executes the behavior associated with the selected state.

---

# Functions

## `UpdateState()`

Determines the enemy's current state based on its distance from the player.

The distance is calculated using:

```csharp
Vector2.Distance(transform.position, player.position)
```

The result is compared against `attackRange` and `detectionRange`.

The priority is:

```text
Distance <= attackRange
        ↓
    Attacking
```

Otherwise:

```text
Distance <= detectionRange
        ↓
     Chasing
```

Otherwise:

```text
Distance > detectionRange
        ↓
       Idle
```

This means `attackRange` takes priority over `detectionRange`. Since the attack range is expected to be smaller than the detection range, an enemy close to the player will always enter the `Attacking` state rather than remaining in `Chasing`.

The state transition can be represented as:

```text
                  distance > detectionRange
                         ┌───────────────┐
                         ▼               │
                      [Idle]             │
                         │               │
              distance <= detectionRange│
                         ▼               │
                    [Chasing]            │
                         │               │
                 distance <= attackRange │
                         ▼               │
                   [Attacking]           │
                         │               │
              distance > attackRange ────┘
```

---

## `UpdateFacing()`

Updates the direction the enemy is facing based on the player's horizontal position.

The method does nothing while the enemy is `Idle`:

```text
currentState == Idle
        ↓
     return
```

This is intentional. When the player leaves the detection range, the enemy keeps facing the direction it was previously facing instead of automatically returning to a default direction.

When the enemy is chasing or attacking, the horizontal position of the player is compared with the enemy's position.

The resulting direction is represented by:

```text
1  → facing right
-1 → facing left
```

If the new direction differs from the current `facingDirection`, the value is updated.

The `visuals` child transform is then flipped by changing its local X scale:

```csharp
visuals.localScale = new Vector3(facingDirection, 1f, 1f)
```

Only the `visuals` object is flipped, rather than the root enemy GameObject.

This is important because the root GameObject contains physics-related components such as the `Rigidbody2D` and `Collider2D`. Flipping only the visual hierarchy prevents the visual orientation change from interfering with the enemy's physics setup.

The behavior can be summarized as:

```text
Player is left of enemy
        ↓
facingDirection = -1
        ↓
Flip visuals horizontally
```

or:

```text
Player is right of enemy
        ↓
facingDirection = 1
        ↓
Flip visuals horizontally
```

---

## `HandleStateBehavior()`

Executes the behavior associated with the current enemy state.

The method uses a `switch` statement to delegate the actual work to `EnemyMovement` and `EnemyAttack`.

### `Idle`

```csharp
movement.Stop()
```

The enemy stops moving when the player is outside the detection range.

### `Chasing`

```csharp
movement.Move(facingDirection)
```

The enemy moves toward the player using the direction calculated by `UpdateFacing()`.

`EnemyAI` therefore decides **which direction** to move, while `EnemyMovement` is responsible for actually performing the movement.

### `Attacking`

```csharp
movement.Stop()
attack.Attack()
```

The enemy stops moving and requests an attack.

This means the AI does not implement the attack itself. It simply tells the `EnemyAttack` component that an attack should occur.

The complete behavior is:

```text
currentState
     │
     ├── Idle
     │    └── movement.Stop()
     │
     ├── Chasing
     │    └── movement.Move(facingDirection)
     │
     └── Attacking
          ├── movement.Stop()
          └── attack.Attack()
```

---

# Debug

## `OnDrawGizmosSelected()`

Displays the enemy's detection and attack ranges in the Unity Editor when the enemy GameObject is selected.

Two wire spheres are drawn:

* `detectionRange`
* `attackRange`

The detection range uses:

```text
Green → enemy is currently Chasing
Yellow → enemy is not currently Chasing
```

The attack range uses:

```text
Green → enemy is currently Attacking
Red → enemy is not currently Attacking
```

These Gizmos provide a visual representation of the same ranges used by `UpdateState()`.

This makes it easier to configure the AI in the Unity Editor and understand why the enemy changes between `Idle`, `Chasing`, and `Attacking`.

The Gizmos do not affect gameplay.

---

# Main state variables

| Variable          | Purpose                                                                  |
| ----------------- | ------------------------------------------------------------------------ |
| `currentState`    | Stores the enemy's current AI state.                                     |
| `player`          | Reference to the player's `Transform`.                                   |
| `detectionRange`  | Maximum distance at which the enemy detects and chases the player.       |
| `attackRange`     | Maximum distance at which the enemy enters the attacking state.          |
| `visuals`         | Child transform containing the enemy's visual elements and weapon pivot. |
| `movement`        | Reference to the `EnemyMovement` component used to control movement.     |
| `attack`          | Reference to the `EnemyAttack` component used to trigger attacks.        |
| `facingDirection` | Stores the enemy's current horizontal facing direction.                  |

---

# Function interaction

The main execution flow is:

```text
FixedUpdate()
     │
     ▼
UpdateState()
     │
     │ Determines behavior based on player distance
     ▼
UpdateFacing()
     │
     │ Determines horizontal direction
     ▼
HandleStateBehavior()
     │
     ├── Idle
     │     └── EnemyMovement.Stop()
     │
     ├── Chasing
     │     └── EnemyMovement.Move(facingDirection)
     │
     └── Attacking
           ├── EnemyMovement.Stop()
           └── EnemyAttack.Attack()
```

The AI therefore acts as a **coordinator** rather than directly controlling the enemy's physics or attack implementation.

---

# Interaction with `EnemyMovement`

`EnemyAI` determines **when and in which direction** the enemy should move.

`EnemyMovement` is responsible for the actual movement implementation.

```text
EnemyAI
   │
   │ Chasing
   ▼
movement.Move(facingDirection)
   │
   ▼
EnemyMovement
   │
   ▼
Enemy physics / movement
```

When the enemy is `Idle` or `Attacking`, the AI calls `movement.Stop()`.

This separation keeps the AI decision-making independent from the movement implementation.

---

# Interaction with `EnemyAttack`

When the enemy enters the `Attacking` state, `EnemyAI` stops its movement and calls:

```csharp
attack.Attack()
```

The AI therefore determines **when an attack should happen**, while `EnemyAttack` determines **how the attack is performed**.

```text
Player enters attack range
          ↓
    UpdateState()
          ↓
     Attacking
          ↓
    HandleStateBehavior()
          ↓
      attack.Attack()
          ↓
     EnemyAttack
```

---

# Overall responsibility

`EnemyAI` is the high-level decision-making component for the enemy.

Its responsibilities are:

1. Find or receive a reference to the player.
2. Determine the player's distance.
3. Select the appropriate AI state.
4. Determine which direction the enemy should face.
5. Tell `EnemyMovement` whether to move or stop.
6. Tell `EnemyAttack` when to attack.

It does **not** directly implement movement physics or attack mechanics.

The resulting architecture separates the enemy into distinct responsibilities:

```text
                    EnemyAI
                       │
          ┌────────────┴────────────┐
          │                         │
          ▼                         ▼
   EnemyMovement              EnemyAttack
          │                         │
          ▼                         ▼
   Movement physics             Attack logic
```

This design makes `EnemyAI` the central coordinator of enemy behavior while keeping movement and combat logic in their respective components.
