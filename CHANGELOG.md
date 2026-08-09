# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-08-09

### Added
- Initial release as a standalone UPM package.
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
