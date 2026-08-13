# EnemyMovement.cs

## Overview

`EnemyMovement` is responsible for the enemy's **horizontal physics-based movement**.

The script provides two public operations:

* `Move(float direction)`: moves the enemy horizontally in the specified direction.
* `Stop()`: stops horizontal movement while preserving vertical velocity.

The script does not decide **when** the enemy should move or **which direction** it should choose. That responsibility belongs to `EnemyAI`.

`EnemyMovement` therefore acts as the low-level movement component used by the AI.

```text
EnemyAI
   │
   ├── Move(direction)
   │
   └── Stop()
        │
        ▼
EnemyMovement
        │
        ▼
Rigidbody2D
```

The script requires a `Rigidbody2D` through `[RequireComponent(typeof(Rigidbody2D))]`.

---

# Unity lifecycle

## `Start()`

Initializes the `Rigidbody2D` reference used by the movement system.

The component retrieves the `Rigidbody2D` attached to the same GameObject:

```csharp
rb = GetComponent<Rigidbody2D>();
```

This reference is then used by both `Move()` and `Stop()` to modify the enemy's velocity.

Because of `[RequireComponent(typeof(Rigidbody2D))]`, Unity ensures that the GameObject has a `Rigidbody2D` component when this script is added.

---

# Functions

## `Move(float direction)`

Moves the enemy horizontally according to the supplied direction.

The horizontal velocity is calculated as:

```csharp
direction * moveSpeed
```

The vertical velocity is preserved:

```csharp
rb.linearVelocity = new Vector2(
    direction * moveSpeed,
    rb.linearVelocity.y
);
```

This means the function only controls the X component of the `Rigidbody2D` velocity.

For the intended usage, `direction` should be one of:

```text
-1 → move left
 0 → stop horizontal movement
 1 → move right
```

Passing fractional values is technically possible, but it will proportionally reduce the horizontal speed. For example, passing `0.5` results in half of `moveSpeed`.

### Example

If:

```csharp
moveSpeed = 3
```

then:

```text
direction = -1 → horizontal velocity = -3
direction =  0 → horizontal velocity =  0
direction =  1 → horizontal velocity =  3
```

The function does not modify the enemy's vertical velocity, allowing gravity, jumping, falling, or other vertical physics to continue independently.

---

## `Stop()`

Stops the enemy's horizontal movement.

The function sets the X component of `linearVelocity` to zero while preserving the current Y velocity:

```csharp
rb.linearVelocity = new Vector2(
    0f,
    rb.linearVelocity.y
);
```

This means calling `Stop()` does not cause the enemy to stop falling or otherwise interfere with its vertical physics.

For example:

```text
Before:
X = 3
Y = -5

After Stop():
X = 0
Y = -5
```

This is particularly important when `EnemyAI` tells the enemy to stop while it is falling or affected by other vertical forces.

---

# Main state variables

| Variable    | Purpose                                                                              |
| ----------- | ------------------------------------------------------------------------------------ |
| `moveSpeed` | Controls the enemy's horizontal movement speed.                                      |
| `rb`        | Reference to the enemy's `Rigidbody2D`.                                              |
| `direction` | Determines the horizontal movement direction. Intended values are `-1`, `0`, or `1`. |

---

# Function interaction

`EnemyMovement` is primarily controlled by `EnemyAI`.

The AI decides what the enemy should do based on its current state and then calls the appropriate movement method.

```text
EnemyAI
   │
   ├── Idle
   │     └── movement.Stop()
   │
   ├── Chasing
   │     └── movement.Move(facingDirection)
   │
   └── Attacking
         └── movement.Stop()
```

`EnemyMovement` then translates that decision into a change to the `Rigidbody2D` velocity.

---

# Interaction with `EnemyAI`

`EnemyAI` is responsible for deciding **when** movement should occur.

When the enemy is `Chasing`, the AI passes its current `facingDirection` to `Move()`:

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
Rigidbody2D.linearVelocity
```

When the enemy is `Idle` or `Attacking`, the AI calls:

```csharp
movement.Stop()
```

This keeps movement decisions centralized in `EnemyAI` while keeping the actual physics implementation inside `EnemyMovement`.

---

# Horizontal movement and vertical physics

A key characteristic of this component is that it only modifies horizontal velocity.

Both `Move()` and `Stop()` preserve:

```csharp
rb.linearVelocity.y
```

Therefore:

```text
Horizontal movement → EnemyMovement
Vertical movement  → Rigidbody2D / other physics systems
```

This separation prevents the enemy movement system from accidentally overriding gravity or other vertical forces.

For example, while chasing the player:

```text
Move(1)
   ↓
X velocity = moveSpeed
Y velocity = unchanged
```

While falling:

```text
Move(-1)
   ↓
X velocity = -moveSpeed
Y velocity = falling velocity
```

---

# Overall responsibility

`EnemyMovement` is the low-level **horizontal movement controller** for the enemy.

Its responsibilities are:

1. Obtain the enemy's `Rigidbody2D`.
2. Apply horizontal movement at the configured speed.
3. Stop horizontal movement when requested.
4. Preserve vertical velocity during both operations.

It does not contain any AI logic, target detection, state management, or attack behavior.

The resulting architecture is:

```text
                 EnemyAI
                    │
          ┌─────────┴─────────┐
          │                   │
       Chasing          Idle / Attacking
          │                   │
          ▼                   ▼
   Move(direction)           Stop()
          │                   │
          └─────────┬─────────┘
                    ▼
              EnemyMovement
                    │
                    ▼
               Rigidbody2D
                    │
                    ▼
             Enemy physics
```

This separation allows `EnemyAI` to focus on **decision-making**, while `EnemyMovement` focuses exclusively on **executing horizontal movement through the physics system**.
