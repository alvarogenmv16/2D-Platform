# PlayerHealth.cs

## Overview

`PlayerHealth` is responsible for managing the player's health and death state.

The script provides a simple health system based on a maximum health value. It initializes the player's health when the object starts, allows other scripts to apply damage through `TakeDamage()`, and triggers `Die()` when the player's health reaches zero.

The current implementation only logs damage and death events. The actual death behavior is intentionally left as a placeholder for future implementation.

---

## Unity lifecycle

### `Start()`

Initializes the player's health when the component starts.

The method sets:

```csharp
currentHealth = maxHealth
```

This means the player always begins the game with their configured maximum health.

The initial health value is therefore determined by the Inspector-configured `maxHealth`.

---

# Functions

## `TakeDamage(float amount)`

Applies damage to the player's current health.

This is the main public method of the script and is intended to be called by other gameplay systems, such as enemy attacks, traps, projectiles, or environmental hazards.

### Damage flow

When called, the method first checks whether the player is already dead:

```text
currentHealth <= 0
        ↓
     return
```

If the player still has health, the specified `amount` is subtracted from `currentHealth`.

The resulting value is then clamped to zero using `Mathf.Max()`:

```csharp
currentHealth = Mathf.Max(currentHealth, 0f)
```

This prevents the player's health from becoming negative.

After applying the damage, the method logs the amount of damage received and the player's remaining health.

Finally, if the resulting health is zero, `Die()` is called.

The complete flow is:

```text
TakeDamage(amount)
       ↓
Already dead?
   ┌───┴───┐
  Yes      No
   ↓        ↓
 return   Subtract damage
            ↓
       Clamp to 0
            ↓
       Log health
            ↓
      Health <= 0?
        ┌───┴───┐
       No      Yes
        ↓       ↓
      Return   Die()
```

### Interaction with other scripts

Because `TakeDamage()` is `public`, other components can directly communicate damage to the player through this method.

For example, an enemy weapon could conceptually perform:

```text
EnemyWeapon
     ↓
PlayerHealth.TakeDamage(amount)
     ↓
currentHealth updated
     ↓
Die() if health reaches zero
```

This keeps the responsibility for calculating and storing player health inside `PlayerHealth`, while other systems only need to communicate how much damage should be applied.

---

## `Die()`

Handles the player's death event.

Currently, this method only writes a message to the Unity Console:

```text
Player died!
```

The method is `private`, meaning that other scripts cannot directly call `Die()`. Instead, death is triggered internally by `TakeDamage()` when `currentHealth` reaches zero.

The method is intentionally structured as a separate function so that future death behavior can be added without changing the damage calculation logic.

Potential future responsibilities include:

* Playing a death animation.
* Disabling player input.
* Disabling movement.
* Triggering a respawn.
* Showing a game-over screen.
* Notifying other game systems that the player has died.

---

# Main state variables

| Variable        | Purpose                                                                              |
| --------------- | ------------------------------------------------------------------------------------ |
| `maxHealth`     | Maximum amount of health the player can have. Configurable from the Unity Inspector. |
| `currentHealth` | Current health of the player during gameplay.                                        |
| `amount`        | Amount of damage received by the player during a `TakeDamage()` call.                |

---

# Function interaction

The script has a simple execution flow:

```text
Start()
  ↓
currentHealth = maxHealth
  ↓
Gameplay
  ↓
TakeDamage(amount)
  ↓
currentHealth -= amount
  ↓
Clamp health to 0
  ↓
Health reaches 0?
  ├── No → Continue playing
  └── Yes
       ↓
      Die()
```

`Start()` establishes the initial state, while `TakeDamage()` is the main entry point used by other gameplay systems.

`Die()` is only reached through `TakeDamage()` when the player's health reaches zero.

---

# Overall responsibility

`PlayerHealth` acts as the central component responsible for the player's **health state and death detection**.

It deliberately does not handle how damage is produced. Enemy attacks, weapons, hazards, or other gameplay systems are responsible for deciding when damage should occur and how much damage to apply. `PlayerHealth` only manages the consequences of that damage.

This separation allows other scripts, such as `EnemyWeapon`, to interact with the player's health without needing to know how the health system internally works.

The current architecture can therefore be represented as:

```text
Damage Source
     │
     │ TakeDamage(amount)
     ▼
PlayerHealth
     │
     ├── Update currentHealth
     │
     ├── Log damage
     │
     └── Health <= 0
              │
              ▼
             Die()
```

At its current stage, `Die()` is a placeholder, so reaching zero health does **not yet disable or remove the player, trigger a respawn, or end the game**.
