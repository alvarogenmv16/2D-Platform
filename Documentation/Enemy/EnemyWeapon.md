# EnemyWeapon.cs

## Overview

`EnemyWeapon` is responsible for **detecting whether the player is within the enemy's attack point at the moment an attack lands and applying damage if the player is found**.

The script represents a simple weapon or attack point attached to the enemy. It does not decide when the enemy should attack or manage the attack cooldown. Those responsibilities belong to `EnemyAI` and `EnemyAttack`.

The overall attack flow is:

```text
EnemyAI
   │
   │ Enemy is in Attacking state
   ▼
EnemyAttack
   │
   │ Attack cooldown available
   ▼
EnemyWeapon.TryHitPlayer()
   │
   │ Check attack radius
   ▼
Player detected?
   │
   ├── No → No damage
   │
   └── Yes
        ↓
   PlayerHealth.TakeDamage()
```

---

# Properties

## `AttackPointPosition`

Provides the world position of the weapon's attack point.

```csharp
public Vector2 AttackPointPosition => transform.position;
```

The property simply returns the current position of the `EnemyWeapon` GameObject.

This position is used as the center of the attack detection area in `TryHitPlayer()`.

Because the weapon can be placed on a child GameObject, the attack point can move together with the enemy's visual hierarchy.

The property also avoids duplicating the position calculation in the attack logic.

---

# Functions

## `TryHitPlayer()`

Attempts to detect and damage the player at the moment the attack lands.

This is the main public method of the component and is called by `EnemyAttack`.

The method performs three main operations:

1. Detect a player inside the attack radius.
2. Obtain its `PlayerHealth` component.
3. Apply the configured damage.

---

### Player detection

The method uses:

```csharp
Physics2D.OverlapCircle(
    AttackPointPosition,
    attackRadius,
    playerLayer
)
```

This creates a circular detection area centered on `AttackPointPosition`.

Only colliders belonging to `playerLayer` are considered.

The result is stored in `hit`.

If no collider is detected:

```text
hit == null
    ↓
 return
```

No damage is applied.

This means the weapon does not maintain a persistent detection state. Instead, it performs a **single instantaneous check** when `TryHitPlayer()` is called.

---

### Retrieving player health

If a collider is detected, the script attempts to retrieve a `PlayerHealth` component from the same GameObject:

```csharp
PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
```

If the component does not exist, the method does nothing further.

This prevents the weapon from attempting to call `TakeDamage()` on an object that does not contain the expected health component.

---

### Applying damage

If a valid `PlayerHealth` component is found, the weapon calls:

```csharp
playerHealth.TakeDamage(damage);
```

The amount of damage is determined by the serialized `damage` variable.

The responsibility for actually reducing the player's health remains inside `PlayerHealth`.

The weapon therefore only determines:

```text
Did the attack hit?
        ↓
How much damage should be sent?
```

While `PlayerHealth` determines:

```text
How is that damage applied?
```

---

# Debug

## `OnDrawGizmosSelected()`

Displays the weapon's attack radius in the Unity Editor when the weapon GameObject is selected.

A magenta wire sphere is drawn around the weapon's transform:

```text
transform.position
       │
       ▼
  Attack radius
       │
       ▼
Magenta Gizmo
```

The radius shown in the editor corresponds to the `attackRadius` value used by `TryHitPlayer()`.

This makes it possible to visually configure the weapon's effective attack area.

The Gizmo is only a debugging and editor visualization tool and has no effect on gameplay.

---

# Main variables and properties

| Variable / Property   | Purpose                                                         |
| --------------------- | --------------------------------------------------------------- |
| `damage`              | Amount of damage applied to the player when the attack hits.    |
| `attackRadius`        | Radius of the circular hit detection area.                      |
| `playerLayer`         | Layer mask used to identify valid player colliders.             |
| `AttackPointPosition` | World position used as the center of the attack detection area. |

---

# Function interaction

`EnemyWeapon` is the final component in the enemy attack chain.

The complete interaction between the enemy systems is:

```text
EnemyAI
   │
   │ Determines that the enemy should attack
   ▼
EnemyAttack.Attack()
   │
   │ Checks attack cooldown
   ▼
EnemyWeapon.TryHitPlayer()
   │
   │ Physics2D.OverlapCircle()
   ▼
Player inside attack radius?
   │
   ├── No
   │    └── Return
   │
   └── Yes
        ↓
   GetComponent<PlayerHealth>()
        │
        ▼
   PlayerHealth.TakeDamage(damage)
```

This creates a clear separation of responsibilities:

| Component      | Responsibility                                        |
| -------------- | ----------------------------------------------------- |
| `EnemyAI`      | Determines when the enemy should attack.              |
| `EnemyAttack`  | Controls the attack cooldown and triggers the attack. |
| `EnemyWeapon`  | Performs the hit detection and sends the damage.      |
| `PlayerHealth` | Receives and processes the damage.                    |

---

# Attack detection behavior

The weapon uses an **instantaneous overlap check**, rather than a persistent trigger collider.

This means the player only gets hit if their collider is inside the attack radius **at the exact moment `TryHitPlayer()` is called**.

For example:

```text
EnemyAttack
     │
     │ Attack()
     ▼
EnemyWeapon
     │
     │ TryHitPlayer()
     ▼
OverlapCircle()
     │
     ├── Player outside radius → Miss
     │
     └── Player inside radius  → Hit
```

This makes the weapon suitable for a simple melee attack system where `EnemyAttack` determines the exact moment at which the attack connects.

---

# Relationship with the weapon transform

`EnemyWeapon` is expected to represent an attack point attached to the enemy.

Its `transform.position` determines where the attack occurs.

Because `EnemyAI` flips the `visuals` hierarchy rather than the enemy's root GameObject, the weapon can be positioned within that visual hierarchy so that it follows the enemy's facing direction.

The intended structure can therefore be represented as:

```text
Enemy
├── Rigidbody2D
├── Collider2D
├── EnemyAI
├── EnemyMovement
├── EnemyAttack
└── Visuals
    ├── Sprite
    └── Weapon
         └── EnemyWeapon
```

When `Visuals` is flipped by `EnemyAI`, the weapon's position can move to the appropriate side of the enemy while its `AttackPointPosition` continues to use its current world position.

---

# Overall responsibility

`EnemyWeapon` is the low-level **hit detection and damage delivery component** of the enemy attack system.

Its responsibilities are:

1. Define the attack point.
2. Define the attack radius.
3. Detect the player at the moment of impact.
4. Retrieve the player's `PlayerHealth` component.
5. Send the configured damage to `PlayerHealth`.

It does not manage:

* When the enemy attacks.
* Attack cooldowns.
* Enemy AI states.
* Enemy movement.
* The player's health calculation.

The complete architecture is:

```text
                    EnemyAI
                       │
                Attack decision
                       │
                       ▼
                 EnemyAttack
                       │
                Attack cooldown
                       │
                       ▼
                 EnemyWeapon
                       │
                 Hit detection
                       │
                       ▼
                 PlayerHealth
                       │
                 Damage processing
                       │
                       ▼
                    Player
```

This separation keeps the enemy's **decision-making, attack timing, hit detection, and damage processing** independent from one another, making each component easier to modify or extend.
