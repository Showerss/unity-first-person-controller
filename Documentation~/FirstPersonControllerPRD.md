
Your current implementation is <b>beautifully structured</b>. You are practicing excellent code-design principles that are rare to see in standard tutorials:
*   <b>Narrow Interface:</b> Your public-facing API is minimal. The caller (or the `PlayerInput` component) only interacts with simple inputs, leaving the script to handle its own state.
*   <b>Clear Class Roles (Orchestrator vs. Deep Implementation):</b> Your comments (`ORCHESTRATOR`, `DEEP IMPLEMENTATION`) show a strong understanding of architecture. Keeping `Update` as a sequencer that delegates to isolated methods (`ApplyLook`, `ApplyGravity`, `ApplyMovement`) makes the code incredibly readable.
*   <b>Kinematic Integrity:</b> Your jump calculation uses the correct physics formula ($v = \sqrt{2gh}$, implemented as `Mathf.Sqrt(_jumpHeight * -2f * _gravity)`), which guarantees your jump height is exactly what you set, regardless of gravity.
*   <b>Slope Contact:</b> Your small negative gravity reset (`_verticalVelocity = -2f` when grounded) is a professional trick to prevent the controller from briefly losing contact when moving down slight inclines.

However, to move from a <b>functional tutorial script</b> to a <b>fantastic, professional-grade first-person controller (FPC)</b>, there are major improvements needed in <b>kinematics, game feel, and input robustness</b>.

Below is a detailed <b>technical critique</b> of your current code, followed by a <b>Product Requirements Document (PRD)</b> detailing the design of an industry-standard first-person controller.

---

### Part 1: Technical Critique of Your Current Code

#### 1. Instant Momentum Transfer (Robotic Movement)
*   <b>The Issue:</b> Your movement speed instantly snaps to `_walkSpeed` or `_sprintSpeed` and drops to `0` the moment input stops.
*   <b>The Impact:</b> This creates a very "robotic" and jerky feel. Human movement has inertia—it takes a moment to accelerate, decelerate, and change direction.
*   <b>The Fix:</b> Implement linear interpolation (`Mathf.MoveTowards` or `Mathf.Lerp`) with separate acceleration, deceleration, and friction coefficients to give the player a sense of physical mass.

#### 2. Frame-Rate Dependent and Gamepad-Unfriendly Rotation
*   <b>The Issue:</b> In `ApplyLook()`, you rotate by `_lookInput.x * _sensitivity` without multiplying by `Time.deltaTime`.
*   <b>The Impact:</b> While Mouse Delta input in Unity's New Input System is frame-rate independent by default (as it measures pixel deltas per frame), <b>Gamepad or Keyboard turn inputs</b> (which report continuous axis values between `-1` and `1`) will spin incredibly fast on high frame rates and slowly on low frame rates.
*   <b>The Fix:</b> Check the device source or normalize input. For analog inputs, multiply by `Time.deltaTime` and separate horizontal (`X`) and vertical (`Y`) sensitivities—a setting mandatory in modern FPS games.

#### 3. Over-Powered Air Control
*   <b>The Issue:</b> When in mid-air (`!_cc.isGrounded`), `ApplyMovement()` executes exactly the same as on the ground.
*   <b>The Impact:</b> A player can instantly reverse direction or change angles mid-jump with full velocity. This removes all tactical weight from jumping.
*   <b>The Fix:</b> Track current horizontal velocity, preserve momentum when leaving the ground, and apply a reduced `AirControl` coefficient (e.g., `15%` of ground control) to steer in mid-air.

#### 4. The Unreliability of `CharacterController.isGrounded`
*   <b>The Issue:</b> You rely solely on `_cc.isGrounded` for jumping and gravity resets.
*   <b>The Impact:</b> Unity’s built-in `isGrounded` is notoriously buggy. It can flicker `false` when walking down steps, slopes, or over small bumps. If it flickers `false` even for a frame, your gravity resets, vertical velocity builds up, and jumping becomes unresponsive.
*   <b>The Fix:</b> Implement a custom ground check using a <b>SphereCast</b> or a array of downward <b>Raycasts</b> from the base of the capsule.

#### 5. Slope Movement Speed Distortion
*   <b>The Issue:</b> Your movement vector is strictly horizontal (`transform.right` and `transform.forward`).
*   <b>The Impact:</b> When walking up a $30^\circ$ slope, your character moves horizontally at full speed, forcing the `CharacterController` to violently step up, which can cause jitter. When moving down, they step off the slope, float for a microsecond, and slam back down.
*   <b>The Fix:</b> Raycast down to find the slope's surface normal, project your movement vector onto that slope plane using `Vector3.ProjectOnPlane`, and adjust speed based on the angle (slower going up, sliding down steep angles).

---

### Part 2: Product Requirements Document (PRD)
#### Project: Industry-Standard First-Person Controller (FPC)

---

### 1. Executive Summary & Design Pillars
The goal of this document is to define the technical and gameplay specifications for a premium, AAA-feeling first-person controller in Unity. The controller must balance absolute responsive precision (competitive play) with organic physical feedback (immersion).

#### Core Pillars:
1.  <b>Kinetic Weight:</b> The player must feel like they have mass, momentum, and physical constraints.
2.  <b>Juice & Subliminal Feedback:</b> Camera motion, FOV changes, and audio must work in unison to sell the velocity of the character.
3.  <b>Flawless Input & Accessibility:</b> Inputs must feel responsive, forgiving (using "forgiveness windows"), and highly customizable.

---

### 2. Functional Requirements

#### 2.1 Locomotion & Physics

| ID | Feature | Specification | Game Feel Impact |
| :--- | :--- | :--- | :--- |
| <b>F-01</b> | <b>Inertial Locomotion</b> | Ground movement must accelerate and decelerate smoothly using adjustable acceleration and friction curves instead of instant velocity changes. | Simulates human weight; removes jarring transitions. |
| <b>F-02</b> | <b>Projected Slope Alignment</b> | Movement vectors must be projected onto the plane of the current standing slope using surface normals. | Eliminates jitter/shudder when traversing inclines. |
| <b>F-03</b> | <b>Steep Slope Sliding</b> | If standing on a slope exceeding the `Slope Limit` (e.g., $>45^\circ$), the player must slide downwards, losing upward jump capabilities. | Prevents cheese-climbing steep mountains. |
| <b>F-04</b> | <b>Momentum-Preserved Jumping</b> | Jumping must capture the player's horizontal velocity at the moment of lift-off and carry it as momentum. | Encourages skill-based speed retention (strafe jumping/bunnies). |
| <b>F-05</b> | <b>Limited Air Control</b> | Air control must be an additive, low-influence vector that steers momentum rather than instantly resetting horizontal speed. | Makes committing to a jump a meaningful choice. |
| <b>F-06</b> | <b>Step-Down Snapping</b> | When walking down stairs or slopes, a down-raycast must snap the controller to the ground if the distance is within the step limit. | Prevents the player from awkwardly "flying" off stairs. |

#### 2.2 Forgiveness Windows (Platforming Polish)

| ID | Feature | Specification | Why It’s Critical |
| :--- | :--- | :--- | :--- |
| <b>P-01</b> | <b>Coyote Time</b> | Allow the player to jump up to `0.15 seconds` (adjustable) after their capsule has completely left a ledge. | Prevents players from feeling cheated when they press jump a millisecond too late. |
| <b>P-02</b> | <b>Jump Buffering</b> | If the player presses the jump key up to `0.10 seconds` before hitting the ground, store the input and execute the jump the exact frame they land. | Ensures fluid chain-jumping without requiring frame-perfect timing. |

#### 2.3 Camera & Look Mechanics

| ID | Feature | Specification | Game Feel Impact |
| :--- | :--- | :--- | :--- |
| <b>C-01</b> | <b>Independent Axis Sensitivities</b> | Horizontal (Yaw) and Vertical (Pitch) sensitivities must be separate variables. | Standard competitive requirement for mouse/gamepad comfort. |
| <b>C-02</b> | <b>Camera Head Bobbing</b> | Dual-sine wave camera offsets applied to vertical and horizontal local positions based on player velocity. | Simulates natural stride weight. Bob frequency and amplitude must scale dynamically with speed (walking vs. sprinting). |
| <b>C-03</b> | <b>Dynamic FOV Breathing</b> | Dynamic Field of View scaling. FOV should slightly widen during sprints (e.g., $+5$ to $+10$ degrees) and snap back on deceleration. | Sells the psychological illusion of extreme speed. |
| <b>C-04</b> | <b>Turn Lean & Strafe Roll</b> | Tilt the camera slightly on the Z-axis (e.g., $1.5^\circ$) when strafing left/right or during fast turns. | Makes horizontal evasive maneuvers feel physical and dynamic. |

---

### 3. Audio & Sensory Feedback

*   <b>Adaptive Footstep System:</b>
    *   The controller must perform a downward raycast on a footstep event (triggered by the head bob peak) to detect the physics material or tag of the ground.
    *   Audio must play distinct clips for `Dirt`, `Concrete`, `Metal`, `Wood`, or `Water`.
*   <b>Impact Audio:</b>
    *   Falling from a height must trigger a "land" sound, with volume scaling exponentially based on terminal velocity.
    *   Landing must trigger a brief camera dip (vertical translation) to represent knee-flexion.

---

### 4. Architectural & Code Requirements

1.  <b>State Machine Pattern:</b>
    *   Locomotion must be governed by a lightweight state machine: `Grounded`, `Sprinting`, `Airborne`, `Sliding`, `Crouching`. This prevents "spaghetti code" where physics states conflict (e.g., sprinting while crouched, or jumping while sliding).
2.  <b>Input Abstraction:</b>
    *   The controller must not read `PlayerInput` directly in movement methods. It must read from a clean, decoupled `InputReader` structure or custom interface (`IInputProvider`). This allows easy swapping between keyboard, gamepad, mobile, or AI-bot inputs.
3.  <b>Fixed vs. Update Split:</b>
    *   Input polling must occur in `Update()` to ensure no button presses (like jumps) are missed.
    *   Movement computation and `CharacterController.Move()` must be orchestrated in `Update()` (or in `FixedUpdate` if rigidbodies are heavily integrated), but always normalized correctly using either `Time.deltaTime` or `Time.fixedDeltaTime` to avoid physics stuttering.

---