# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-10-16

### Added
- `Teleport(position, yaw)` on `FirstPersonController`.
- `PlayerLock`: freeze or free the player (controller + interaction), and the feet-to-pivot height for teleports.
- `LightSwitch`: press-E lamp, candle or brazier, with lit visuals and a toggle sound.
- `LevelTransitionBox`: doorway volume that loads a scene (Boot stays loaded) or the next build scene, or teleports within the scene; optional lock.
- `ScreenFader`: full-screen black overlay with unscaled-time fades.
- `InteractionPromptHUD`: shows the current interaction prompt with the live key binding.
- World bounds: `WorldBoundsSettings`, `WorldBoundsWalls`, `PathBreadcrumbs` and `WorldBoundsWarden` (darkens the screen toward the edge and returns the player to the road). An optional `IWorldBoundsWarning` next to the warden is shown on the black screen.
- `GameBootstrap` (persistent Boot root that additively loads the first gameplay scene) and the editor-only `BootstrapPlayModeLoader`, which loads Boot when you press Play from another scene. It only acts when the first Build Settings scene holds a `GameBootstrap`.

## [1.0.0] - 2026-08-09

### Added
- Initial release as a standalone UPM package.
- Bundled `Runtime/Input/InputSystem_Actions.inputactions`, so the package is
  self-contained and needs no input setup from the consumer. Moved from the
  sandbox project with its GUID preserved, so existing scene references to it
  keep resolving.
- WASD movement with acceleration/deceleration and air control.
- Manual gravity, jumping, coyote time and jump buffering.
- Sprinting (toggle or hold) with stamina, and sprint FOV widening.
- Crouching and sliding: slope acceleration, mid-slide steering, slide-jump
  momentum, camera roll, and collider height change.
- Head bob and strafe camera tilt.
- Ladder climbing and moving-platform attachment.
- Hold-to-interact system (`IInteractable`) with arm animation, plus `Door`,
  `Ladder`, `MovingPlatform`, `PlatformAttachment`, `HazardZone`, `ZoneTrigger`
  and `SandboxHUD` implementations.
- Exposed state for animation/HUD consumers: `IsGrounded`, `IsSprinting`,
  `IsCrouching`, `IsSliding`, `IsClimbing`, `IsInteracting`.
