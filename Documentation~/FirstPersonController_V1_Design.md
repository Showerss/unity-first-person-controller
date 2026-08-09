  # First Person Controller Plugin - V1 Design Document

## Overview
A standalone, easy-to-use First Person Controller plugin for Unity. Designed for solid foundational movement with an emphasis on utilizing built-in Unity systems rather than reinventing the wheel.

## Core Architecture
- **Physics & Collisions:** `CharacterController`
  - Used for snappy, responsive movement without the unpredictability of Rigidbody physics.
- **Input System:** New Unity Input System
  - **Implementation Strategy:** Utilizing the standard `PlayerInput` component. This allows for drag-and-drop integration and easy visual wiring in the Unity Inspector.

## Camera System
- **Look Handling:** 
  - **Horizontal (Yaw):** Rotates the root Player character globally on the Y-axis.
  - **Vertical (Pitch):** Rotates the Camera local X-axis. Strictly clamped between -90° (down) and 90° (up).
- **Soft Tracking / Smoothing:** `Cinemachine`
  - Using Unity's Cinemachine package to handle camera smoothing and interpolation naturally.

## Movement Mechanics (V1 Scope)
1. **Basic Movement:** WASD mapping mapped directly to the CharacterController's `Move()` function.
2. **Gravity:** Custom mathematical gravity applied each frame since CharacterController lacks native physics gravity.
3. **Jumping:** Simple arc calculator based on a set jump height.
4. **Sprinting:** Run speed multiplier activated via input (e.g., Left Shift).

## Plugin Structure Requirements
Everything needed to drop the controller into a new project should live in an isolated directory structure once organized:
- **Target Folder:** `Assets/FirstPersonControllerPlugin`
- **Contents:**
  - `Scripts/`: Contains the main `FirstPersonController.cs`.
  - `Input/`: The `.inputactions` configuration file.
  - `Prefabs/`: A ready-to-use Player GameObject prefab completely set up (PlayerInput, CharacterController, Cinemachine Virtual Camera, and configured scripts).

## Future Sandbox (Post-V1 Ideas)
- Head bobbing
- Motion blur
- FOV zooming when sprinting
- Leaning mechanics
- Sliding
