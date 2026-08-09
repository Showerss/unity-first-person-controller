# First Person Controller

A modular, drop-in first-person controller for Unity 6, built on `CharacterController` and the new Input System.

Movement is deliberately **not** Rigidbody-based — everything runs through `CharacterController.Move()` with manually applied gravity, which keeps it snappy and predictable rather than physics-spongy.

## Features

- WASD movement with acceleration/deceleration and tunable air control
- Manual gravity, jumping, **coyote time** and **jump buffering**
- Sprinting (toggle or hold) with stamina
- Crouching, and **sliding** — slope acceleration, mid-slide steering, slide-jump momentum, camera roll, and a collider height change
- Head bob, strafe camera tilt, and FOV widening on sprint (widened further mid-slide)
- Ladder climbing and moving-platform attachment
- Hold-to-interact (doors, hazard zones, triggers) with arm animation

## Requirements

| | |
|---|---|
| Unity | 6000.0 or newer |
| Input System | 1.19.0+ (`com.unity.inputsystem`) |
| UGUI / TextMeshPro | 2.0.0+ (`com.unity.ugui`) — used by the sample HUD |

The legacy `Input` class must be disabled — set **Project Settings → Player → Active Input Handling** to *Input System Package (New)* or *Both*.

## Installation

In Unity: **Window → Package Manager → + → Install package from git URL…**

```
https://github.com/Showerss/unity-first-person-controller.git
```

To pin a specific version, append a tag:

```
https://github.com/Showerss/unity-first-person-controller.git#v1.0.0
```

Or add it to `Packages/manifest.json` directly:

```json
{
  "dependencies": {
    "com.firstperson.controller": "https://github.com/Showerss/unity-first-person-controller.git#v1.0.0"
  }
}
```

## Setup

1. Create an empty GameObject, add a **CharacterController** and the **FirstPersonController** component.
2. Parent a **Camera** under it and assign it to the controller's `Camera Transform` field.
3. Add a **PlayerInput** component on the same GameObject as the controller.
4. Set its **Behavior** to **Send Messages** and assign an input actions asset (see below).

### Input actions

**This package ships its own actions asset** — no setup required. Assign it to your `PlayerInput` component:

```
Packages/com.firstperson.controller/Runtime/Input/InputSystem_Actions.inputactions
```

In the Project window it appears under **Packages → First Person Controller → Runtime → Input**.

`PlayerInput` is used in **Send Messages** mode — the controller receives `OnMove`, `OnLook`, `OnJump`, `OnSprint`, `OnCrouch`, and `OnInteract`. There are no manual `InputAction` subscriptions to wire up.

The bundled asset provides a `Player` action map with these actions:

| Action | Type | Keyboard & Mouse | Gamepad |
|---|---|---|---|
| Move | Value (Vector2) | WASD / arrows | Left stick |
| Look | Value (Vector2) | Mouse delta | Right stick |
| Jump | Button | Space | Button South (A) |
| Sprint | Button | Left Shift | Left stick press |
| Crouch | Button | C | Button East (B) |
| Interact | Button (Hold) | E | Button North (Y) |

**Customizing the bindings.** A package installed from a Git URL is immutable — Unity will not let you edit the asset in place. To change bindings, copy it into your own `Assets/` folder, edit the copy, and assign that to `PlayerInput` instead. The controller only cares about the action *names*, so any asset matching the table above works.

## Exposed state

For driving animators, HUDs, audio, or camera effects:

```csharp
bool IsGrounded;
bool IsSprinting;
bool IsCrouching;
bool IsSliding;
bool IsClimbing;
bool IsInteracting;
```

## Interaction system

Implement `IInteractable` on any object to make it respond to the hold-to-interact input:

```csharp
public class MyDevice : MonoBehaviour, IInteractable
{
    public void Interact(GameObject interactor) { /* ... */ }
}
```

Ready-made implementations are included: `Door`, `Ladder`, `MovingPlatform`, `PlatformAttachment`, `HazardZone`, `ZoneTrigger`.

## Not yet implemented

Leaning, motion blur.

## License

MIT
