# 2D Platformer — Unity Learning Project

A 2D platformer developed in Unity as a hands-on learning project focused on understanding Unity, C#, game development concepts, and common gameplay systems.

## 🌐 Overview

This repository contains a 2D platformer built while learning how Unity and C# work together in a real game project.

The project is designed as a practical learning experience rather than as a demonstration of complex architecture. Gameplay systems are developed incrementally, with an emphasis on understanding important Unity concepts, making sensible technical decisions, and keeping the codebase simple and maintainable.

The game explores core platforming mechanics, player control, physics-based movement, enemy behavior, combat, camera systems, and other features commonly found in 2D games.

## ⭐ Key Features

* 2D platformer built with Unity
* Player movement based on `Rigidbody2D`
* Variable jumping and multiple-jump mechanics
* Ground detection using physics queries and layers
* Dash-based player movement
* Unity's New Input System for player controls
* Cinemachine-based camera following
* Enemy AI based on simple state management
* Enemy movement and attack systems separated through component composition
* Enemy detection and attack ranges
* Physics-based attack and hit detection
* Gizmos for visualizing gameplay and debugging logic
* Modular use of Unity components and child transforms
* Code and project structure designed to support learning and future expansion

## 🛠 Architecture

The project follows a simple component-based approach, using Unity's `GameObject` and component system rather than introducing unnecessary architectural complexity.

The main project structure is organized around the following areas:

* `Assets/Scripts/Player/`: Player-related gameplay logic
* `Assets/Scripts/Enemy/`: Enemy AI, movement, and combat logic
* `Assets/Scripts/Input/`: Unity-generated Input System code
* `Assets/Scenes/`: Game scenes and test environments
* `Assets/Prefabs/`: Reusable game objects
* `Assets/Art/`: Visual assets and artwork
* `Docs/`: External project documentation and development notes

Player and enemy behavior are separated into focused components where this provides a clear benefit. For example, enemy decision-making, movement, and attacking are treated as separate responsibilities while remaining part of the same enemy object.

The project intentionally favors straightforward solutions over unnecessarily complex patterns or abstractions.

## 🎮 Gameplay Systems

The project explores several fundamental platforming and gameplay systems.

### Player

The player is controlled through Unity's New Input System and uses a `Rigidbody2D` for physics-based movement.

The movement system covers concepts such as:

* Horizontal movement
* Variable jumping
* Multiple jumps
* Ground detection
* Dash movement
* Movement timing and cooldowns
* Physics-based velocity
* Input handling and buffering

The player also uses a dedicated ground-check object to determine whether the character is standing on a valid platform.

### Enemy AI

Enemies use a simple internal state system to determine their behavior.

The main conceptual states are:

* **Idle** — The enemy is not currently engaging the player.
* **Chasing** — The enemy detects the player and moves toward them.
* **Attacking** — The enemy is close enough to attempt an attack.

The enemy continuously evaluates its relationship with the player using configurable detection and attack ranges.

This provides a simple introduction to state-based AI without requiring a large or overly abstract state-machine architecture.

### Enemy Components

Enemy behavior is divided into a small number of components with clear responsibilities:

* `EnemyAI` decides what the enemy should do.
* `EnemyMovement` handles physical movement.
* `EnemyAttack` handles attack timing and attack execution.
* `EnemyWeapon` represents the enemy's attack point and related weapon properties.

This composition allows the AI to issue simple commands such as moving, stopping, or attacking while the individual components handle the details of those actions.

### Combat

The combat system is designed around the separation between the decision to attack and the actual attack logic.

An enemy AI can request an attack, while the attack component determines whether the attack can currently be performed based on factors such as cooldowns.

Attack points and attack ranges are represented through dedicated weapon objects, allowing the system to later support concepts such as hit detection, damage, hitboxes, animations, and knockback.

## 🎥 Camera

The project uses **Cinemachine** for camera behavior.

The camera system is intended to demonstrate how gameplay objects and camera systems can be separated, while still allowing the camera to follow the player smoothly throughout a platforming environment.

## 🎛 Input System

Player controls use Unity's **New Input System** rather than the legacy `UnityEngine.Input` API.

Input actions are organized through the Unity-generated `InputSystem_Actions` class and are accessed from C# to handle gameplay actions such as:

* Movement
* Jumping
* Dashing
* Attacking
* Interaction
* Crouching
* Sprinting
* Looking
* Other player actions

`InputSystem_Actions.cs` is generated by Unity and should not be edited manually.

## 🧩 Unity Concepts

Throughout development, the project explores important Unity and C# concepts through practical implementation.

These include:

* `MonoBehaviour` components
* `Rigidbody2D`
* Physics queries
* `LayerMask`
* `Transform` and child objects
* `localPosition` and `localScale`
* Unity's Input System
* `Update` and `FixedUpdate`
* Timers and cooldowns
* Enums and state management
* Component references
* `GetComponent<T>()`
* Public and private members
* Properties
* Component composition
* Physics-based movement
* Gizmos and debugging tools

The goal is not to implement every concept in isolation, but to understand them through the systems that make up an actual game.

## 📂 Documentation

Project documentation is maintained separately from Unity's editor and stored in the repository alongside the project.

The `Docs/` directory is intended for documenting:

* Gameplay systems
* Architecture and design decisions
* Unity and C# concepts encountered during development
* Technical explanations
* Development notes
* Other useful project information

Markdown documentation can be edited directly in VS Code and viewed through GitHub without requiring Unity.

## 💻 Development Approach

Development follows an incremental and interactive learning approach.

New systems are introduced one step at a time, allowing their implementation and design decisions to be understood before moving on to the next feature.

The project prioritizes:

* Learning through implementation
* Clear and simple code
* Practical Unity patterns
* Separation of responsibilities where it provides real value
* Avoiding unnecessary abstraction
* Understanding important technical decisions
* Maintaining a codebase that is easy to modify and experiment with

The project is intentionally treated as a learning environment, so simplicity and understanding are preferred over applying complex architecture purely for the sake of following advanced patterns.

## 🚀 Getting Started

1. Clone the repository.
2. Open the project using a compatible version of Unity.
3. Open the project through Unity Hub.
4. Open the relevant scene from `Assets/Scenes/`.
5. Enter Play Mode to run the game.
6. Open the project in VS Code to work on scripts and documentation.

The exact Unity version and additional project requirements should be kept consistent with the project's Unity configuration files.

## 🧪 Development Notes

* Unity's New Input System is used for player input.
* `InputSystem_Actions.cs` is generated by Unity and should not be modified manually.
* Physics-based gameplay uses `Rigidbody2D` where appropriate.
* Physics-related movement and gameplay operations are handled with consideration for Unity's `FixedUpdate` cycle.
* Enemy behavior uses simple state-based decision making.
* Enemy functionality is divided into components when the separation provides a clear benefit.
* Visual elements can be separated from physics and gameplay objects through child transforms.
* Gizmos are used to make gameplay ranges and other debugging information easier to understand.
* The codebase favors simple, understandable solutions over unnecessary architectural complexity.

## 💡 Contribution Guidelines

* 🚀 `feat`: Add new features
* 🐛 `bugfix`: Resolve bugs
* ♻ `refactor`: Improve code structure without behavior changes
* 📚 `docs`: Update documentation
