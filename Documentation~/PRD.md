# Product Requirements Document (PRD)
## Premium First-Person Controller (FPC) for Unity

### 1. Executive Summary & Design Pillars
The goal of this project is to implement an industry-standard, high-fidelity first-person controller in Unity. The controller must balance absolute responsive precision (competitive play) with organic physical feedback (immersion).

#### Core Pillars:
1. **Kinetic Weight:** The player must feel like they have mass, momentum, and physical constraints.
2. **Juice & Subliminal Feedback:** Camera motion, FOV changes, and audio must work in unison to sell the velocity of the character.
3. **Flawless Input & Accessibility:** Inputs must feel responsive, forgiving (using "forgiveness windows"), and highly customizable.

---

### 2. Implementation Roadmap

```
+---------------------------------------------------------------------------------+
|                                 IMPLEMENTATION ROADMAP                          |
+---------------------------------------------------------------------------------+
|                                                                                 |
|  [STEP 1: PHYSICS UPGRADE]                                                      |
|  * SphereCast Ground Checker                                                    |
|  * Smooth Inertia (Acceleration / Deceleration)                                 |
|  * Slope projection (ProjectOnPlane)                                            |
|  * Steep slope sliding & reduced slide-control                                  |
|  * Realistic Air Control                                                        |
|                                                                                 |
|                        |                                                        |
|                        v                                                        |
|                                                                                 |
|  [STEP 2: PLATFORMING POLISH]                                                   |
|  * Coyote Time (forgiving ledge jumps)                                          |
|  * Jump Buffering (fluid chain jumping)                                         |
|                                                                                 |
|                        |                                                        |
|                        v                                                        |
|                                                                                 |
|  [STEP 3: CAMERA POLISH]                                                        |
|  * Dual-Sine Head Bobbing (scales with velocity)                                |
|  * Strafe Lean / Turn Roll (adds physical rotation)                             |
|  * Sprint FOV Breathing (widen FOV on speed)                                    |
|                                                                                 |
|                        |                                                        |
|                        v                                                        |
|                                                                                 |
|  [STEP 4: AUDIO SYSTEM]                                                         |
|  * Material-dependent footsteps (Raycast + PhysMaterial)                        |
|  * Exponential landing impact thuds & knee-bend camera dip                      |
|                                                                                 |
+---------------------------------------------------------------------------------+
```

---

### 3. Detailed Specifications

#### 3.1 Locomotion & Physics (Step 1)
*   **Inertial Locomotion:** Ground movement must accelerate and decelerate smoothly using adjustable acceleration and friction curves instead of instant velocity changes.
*   **Projected Slope Alignment:** Movement vectors must be projected onto the plane of the current standing slope using surface normals. This prevents camera jitter when going up or down inclines.
*   **Steep Slope Sliding:** If standing on a slope exceeding the `Slope Limit` (e.g., $>45^\circ$), the player must slide downwards and lose upward jump capabilities.
*   **Air Control:** Reduced control coefficient (e.g., `20%` of ground control) to steer momentum rather than instantly resetting horizontal speed in mid-air.

#### 3.2 Forgiveness Windows (Step 2)
*   **Coyote Time:** Allow the player to jump up to `0.15 seconds` after their capsule has completely left a ledge.
*   **Jump Buffering:** Store jump input up to `0.10 seconds` before hitting the ground, and execute the jump the frame the player lands.

#### 3.3 Camera & Look Mechanics (Step 3)
*   **Independent Axis Sensitivities:** Horizontal (Yaw) and Vertical (Pitch) sensitivities must be separate variables.
*   **Camera Head Bobbing:** Dual-sine wave camera offsets applied to vertical and horizontal local positions based on player velocity.
*   **Dynamic FOV Breathing:** Dynamic Field of View scaling. FOV should slightly widen during sprints (e.g., $+5$ to $+10$ degrees) and snap back on deceleration.
*   **Turn Lean & Strafe Roll:** Tilt the camera slightly on the Z-axis (e.g., $1.5^\circ$) when strafing left/right or during fast turns.

#### 3.4 Audio & Sensory Feedback (Step 4)
*   **Adaptive Footstep System:** Raycast downward on footstep events to play distinct clips for `Dirt`, `Concrete`, `Metal`, `Wood`, or `Water`.
*   **Impact Audio:** Falling from a height triggers a "land" sound with volume scaling exponentially based on terminal velocity, paired with a brief camera dip.
