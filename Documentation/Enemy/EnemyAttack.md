# EnemyAttack.cs

## Overview

`EnemyAttack` is responsible for controlling **when an enemy can perform an attack**.

The script acts as an intermediate layer between the enemy's high-level AI and its weapon.

`EnemyAI` decides when the enemy should be in the `Attacking` state and calls `EnemyAttack.Attack()`. `EnemyAttack` then handles the attack cooldown and, when an attack is allowed, asks `EnemyWeapon` to attempt to hit the player.

The overall responsibility can be summarized as:

```text
EnemyAI
   │
   │ Attack()
   ▼
EnemyAttack
   │
   ├── Check cooldown
   │
   └── EnemyWeapon.TryHitPlayer()
```

This separation allows the AI to repeatedly request attacks without having to manage attack timing itself.

---

# Unity lifecycle

## `FixedUpdate()`

Updates the attack cooldown timer during Unity's physics update cycle.

If `attackCooldownTimer` is greater than zero, the timer is reduced using `Time.fixedDeltaTime`.

```text id="2b4qj1"
attackCooldownTimer > 0
        ↓
attackCooldownTimer -= Time.fixedDeltaTime
```

Once the timer reaches zero or below, the enemy is allowed to attack again.

Using `FixedUpdate()` means the cooldown is updated consistently with the physics-based gameplay systems.

The timer is not allowed to prevent attacks permanently because `Attack()` only blocks execution while the timer is greater than zero.

---

# Functions

## `Attack()`

Attempts to perform an enemy attack.

This is the main public method of the component and is intended to be called by `EnemyAI` when the enemy enters or remains in the `Attacking` state.

### Cooldown check

The first operation is checking whether the attack is currently on cooldown.

```text
attackCooldownTimer > 0
        ↓
     return
```

If the timer is still active, the method exits immediately.

This is important because `EnemyAI` can call `Attack()` repeatedly during `FixedUpdate()` while the enemy remains within attack range. The cooldown prevents those repeated calls from producing an attack every physics frame.

### Performing the attack

If the cooldown has expired, the method logs the attack event:

```text
Enemy attacks!
```

It then checks whether an `EnemyWeapon` reference has been assigned.

If a weapon exists, it calls:

```csharp
weapon.TryHitPlayer()
```

The actual hit detection and damage logic are therefore delegated to `EnemyWeapon`.

### Starting the cooldown

After an attack attempt, the cooldown timer is reset:

```csharp
attackCooldownTimer = attackCooldown
```

This prevents another attack from being performed until the configured cooldown has elapsed.

The complete flow is:

```text
Attack()
   │
   ▼
Cooldown active?
   │
 ┌─┴───────┐
Yes       No
 │         │
 ▼         ▼
Return   Log attack
           │
           ▼
      Weapon assigned?
        │       │
       No      Yes
        │       │
        │       ▼
        │ TryHitPlayer()
        │       │
        └───┬───┘
            ▼
      Reset cooldown
```

---

# Main state variables

| Variable              | Purpose                                                                  |
| --------------------- | ------------------------------------------------------------------------ |
| `attackCooldown`      | Minimum time between successful attack attempts.                         |
| `weapon`              | Reference to the `EnemyWeapon` responsible for hit detection and damage. |
| `attackCooldownTimer` | Tracks the remaining time before another attack can be performed.        |

---

# Function interaction

The attack system works together with `EnemyAI` and `EnemyWeapon`.

```text
EnemyAI
   │
   │ Enemy enters Attacking state
   ▼
EnemyAttack.Attack()
   │
   ├── Cooldown active?
   │       │
   │       └── Yes → Return
   │
   └── No
        │
        ▼
   EnemyWeapon.TryHitPlayer()
        │
        ▼
   Hit detection / damage
```

`EnemyAttack` therefore sits between the AI and the weapon:

```text
EnemyAI
  │
  │ Decides when to attack
  ▼
EnemyAttack
  │
  │ Controls attack timing
  ▼
EnemyWeapon
  │
  │ Performs hit detection
  ▼
PlayerHealth
  │
  │ Receives damage
  ▼
Player
```

The final stages of this chain depend on the implementation of `EnemyWeapon`.

---

# Interaction with `EnemyAI`

`EnemyAI` calls `Attack()` whenever its current state is `Attacking`.

Because `EnemyAI` can remain in that state for multiple physics frames, `EnemyAttack` may receive repeated calls:

```text
FixedUpdate
   ↓
EnemyAI
   ↓
Attacking
   ↓
EnemyAttack.Attack()
```

On subsequent physics frames:

```text
EnemyAI
   ↓
Attacking
   ↓
EnemyAttack.Attack()
   ↓
Cooldown active
   ↓
Return
```

Once the cooldown expires, the next call to `Attack()` can trigger another attack.

This means the attack rate is controlled entirely by `attackCooldown`, while the decision to attack is controlled by `EnemyAI`.

---

# Interaction with `EnemyWeapon`

`EnemyAttack` does not directly detect the player or apply damage.

Instead, it calls:

```csharp
weapon.TryHitPlayer()
```

This creates a clear separation of responsibilities:

| Component      | Responsibility                             |
| -------------- | ------------------------------------------ |
| `EnemyAI`      | Decides when the enemy should attack.      |
| `EnemyAttack`  | Controls attack timing and cooldown.       |
| `EnemyWeapon`  | Attempts to detect and hit the player.     |
| `PlayerHealth` | Handles the damage received by the player. |

If `weapon` has not been assigned, the attack still starts its cooldown, but no hit attempt is performed.

This behavior allows the component to avoid a null-reference error when no weapon is configured.

---

# Attack timing

The attack system uses two separate concepts:

```text
EnemyAI state
     ↓
"Should the enemy attack?"
     ↓
EnemyAttack
     ↓
"Is the attack available yet?"
     ↓
EnemyWeapon
     ↓
"Did the attack hit the player?"
```

For example, with:

```csharp
attackCooldown = 1 second
```

an enemy that remains in attack range can request an attack every physics frame, but only one attack can actually be initiated approximately every second.

The cooldown begins **after an attack attempt is processed**, regardless of whether a weapon is assigned.

---

# Overall responsibility

`EnemyAttack` acts as the **attack timing controller** for the enemy.

Its responsibilities are:

1. Maintain the attack cooldown.
2. Determine whether an attack can currently be performed.
3. Trigger the configured `EnemyWeapon`.
4. Prevent repeated attacks while the cooldown is active.

It does not determine whether the player is within attack range, and it does not directly handle hit detection or player health.

The overall enemy attack architecture is therefore:

```text
                   EnemyAI
                      │
             Player in attack range
                      │
                      ▼
                EnemyAttack
                      │
              Cooldown available?
                  ┌───┴───┐
                 No      Yes
                  │        │
                  ▼        ▼
                Return   EnemyWeapon
                            │
                            ▼
                     TryHitPlayer()
                            │
                            ▼
                       PlayerHealth
```
