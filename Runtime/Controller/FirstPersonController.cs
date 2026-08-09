using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    // NARROW INTERFACE — the only surface a user of this class ever touches
    [Header("Movement Speed")]
    [SerializeField] float _walkSpeed = 5f;
    [SerializeField] float _sprintSpeed = 9f;
    [SerializeField] float _acceleration = 12f;
    [SerializeField] float _deceleration = 10f;
    [SerializeField] float _airControl = 0.2f;
    [SerializeField] bool _sprintIsToggle = true;

    [Header("Jumping & Gravity")]
    [SerializeField] float _jumpHeight = 1.8f;
    [SerializeField] float _gravity = -20f;
    [SerializeField] float _slopeLimit = 45f;
    [SerializeField] float _slideControl = 2f;

    [Header("Look")]
    [SerializeField] float _sensitivity = 0.15f;
    [SerializeField] float _pitchClamp = 89f;
    [SerializeField] Transform _cameraTransform;

    [Header("Ground Check")]
    [SerializeField] LayerMask _groundLayers = ~0; // Default to everything
    [SerializeField] float _groundCheckDistance = 0.15f;

    [Header("Crouch Settings")]
    [SerializeField] float _crouchHeight = 1.2f;
    [SerializeField] float _crouchSpeed = 2.5f;
    [SerializeField] float _crouchTransitionSpeed = 10f;

    [Header("Slide Settings")]
    [SerializeField] float _minSlideSpeed = 5.5f;
    [SerializeField] float _slideInitialBoost = 11f;
    [SerializeField] float _slideFriction = 6f;
    [SerializeField] float _slideSteerControl = 2f;
    [SerializeField] float _slideHeight = 1.0f;
    [SerializeField] float _minEndSlideSpeed = 3f;
    [SerializeField] float _slideCooldown = 0.4f;
    [SerializeField] float _slideCameraTilt = -3f;

    [Header("Platforming Polish & Juice")]
    [SerializeField] float _coyoteTime = 0.15f;
    [SerializeField] float _jumpBufferTime = 0.12f;
    [SerializeField] float _strafeCameraTilt = 2.0f;
    [SerializeField] float _minImpactVelocity = 3.0f;
    [SerializeField] float _landingDipMultiplier = 0.025f;
    [SerializeField] float _maxLandingDip = 0.25f;
    [SerializeField] float _stepDownDistance = 0.3f;

    [Header("Ladder Settings")]
    [SerializeField] float _ladderClimbSpeed = 3.5f;

    [Header("Push Settings")]
    [SerializeField] float _pushForce = 2.0f;

    [Header("Interaction Settings")]
    [SerializeField] float _interactionDuration = 0.2f;
    [SerializeField] Transform _armTransform;
    [SerializeField] Vector3 _armIdlePosition = new Vector3(0.4f, -0.4f, 0.5f);
    [SerializeField] Vector3 _armInteractPosition = new Vector3(0.2f, -0.2f, 0.8f);

    [Header("Sprint Camera Effects")]
    [SerializeField] float _fovSprintMultiplier = 1.15f;
    [SerializeField] float _fovTransitionSpeed = 8f;
    [SerializeField] bool _enableSprintHeadBob = true;
    [SerializeField] float _bobFrequency = 14f;
    [SerializeField] float _bobAmount = 0.05f;

    [Header("Stamina Settings")]
    [SerializeField] float _maxStamina = 100f;
    [SerializeField] float _staminaDepletionRate = 20f;
    [SerializeField] float _staminaRegenRate = 15f;
    [SerializeField] float _staminaRegenDelay = 1f;

    // HIDDEN STATE — private; complexity lives here, not in the caller
    CharacterController _cc;
    Vector3 _velocity; // Total physical velocity including vertical
    float _pitch;
    Vector2 _moveInput;
    Vector2 _lookInput;
    bool _jumpPressed;
    bool _isSprinting;

    // Interaction & I-Frame State
    float _iframeTimer;
    bool _isInteracting;
    float _interactionTimer;

    // Crouch & Head Bob state
    float _defaultHeight = 2f;
    float _defaultCameraY;
    float _currentCameraY;
    float _defaultFov;
    float _bobTimer;
    float _currentBobOffsetX;
    float _currentBobOffsetY;
    bool _wantsToCrouch;
    bool _isCrouching;

    // Slide state
    bool _isSliding;
    Vector3 _slideDirection;
    float _slideSpeed;
    float _slideCooldownTimer;
    float _currentCameraRoll;

    // Polish & Juice State
    float _coyoteTimer;
    float _jumpBufferTimer;
    bool _wasGrounded;
    float _previousVelocityY;
    float _landingDipOffset;

    // Stamina state
    float _stamina;
    float _staminaDelayTimer;

    // Public State API for HUD
    public float Stamina => _stamina;
    public float MaxStamina => _maxStamina;
    public float StaminaPercent => _maxStamina > 0 ? _stamina / _maxStamina : 0f;

    // Ladder climbing state
    bool _isClimbing;
    Ladder _currentLadder;

    // Advanced Physics State
    bool _isGrounded;
    Vector3 _groundNormal = Vector3.up;
    bool _isOnSteepSlope;
    Vector3 _slopeSlideDirection;

    // Public State API for HUD
    public bool IsGrounded => _isGrounded;
    public bool IsSprinting => _isSprinting && !_isSliding && _cc.velocity.magnitude > 0.1f && !_isCrouching && !_isClimbing;
    public bool IsCrouching => _isCrouching || _isSliding || (_wantsToCrouch && !_isClimbing);
    public bool IsSliding => _isSliding;
    public bool IsClimbing => _isClimbing;
    public bool IsInteracting => _isInteracting;
    public bool HasIFrame => _iframeTimer > 0f;

    Camera _camera;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        if (_cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                _cameraTransform = cam.transform;
            }
            else
            {
                Debug.LogError($"_cameraTransform is not assigned on {gameObject.name} and no Camera was found in children!", this);
            }
        }

        _defaultHeight = _cc.height;
        if (_cameraTransform != null)
        {
            _defaultCameraY = _cameraTransform.localPosition.y;
            _currentCameraY = _defaultCameraY;
            _camera = _cameraTransform.GetComponent<Camera>();
            if (_camera != null)
            {
                _defaultFov = _camera.fieldOfView;
            }
        }

        if (_armTransform != null)
        {
            _armTransform.localPosition = _armIdlePosition;
        }

        _stamina = _maxStamina;
    }

    // ORCHESTRATOR — sequences the implementation each frame; caller says what, not how
    void Update()
    {
        UpdateTimers();
        UpdateStamina();
        ApplyLook();
        
        if (_isClimbing)
        {
            ApplyLadderClimbing();
        }
        else
        {
            ApplyCrouch();
            ApplyHeadBob();
            CheckGroundStatus();
            ApplyGravityAndJumping();
            ApplyMovement();
        }

        ApplyCameraFov();
        ApplyArmAnimation();
    }

    void UpdateTimers()
    {
        if (_iframeTimer > 0f) _iframeTimer -= Time.deltaTime;
        if (_slideCooldownTimer > 0f) _slideCooldownTimer -= Time.deltaTime;
        if (_coyoteTimer > 0f) _coyoteTimer -= Time.deltaTime;
        if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;
        
        if (_isInteracting)
        {
            _interactionTimer -= Time.deltaTime;
            if (_interactionTimer <= 0f)
            {
                _isInteracting = false;
            }
        }
    }

    public void StartDoorInteraction()
    {
        _isInteracting = true;
        _interactionTimer = _interactionDuration;
        _iframeTimer = _interactionDuration;
    }

    void ApplyArmAnimation()
    {
        if (_armTransform == null) return;

        Vector3 targetPos = _isInteracting ? _armInteractPosition : _armIdlePosition;
        _armTransform.localPosition = Vector3.Lerp(_armTransform.localPosition, targetPos, 15f * Time.deltaTime);
    }

    void UpdateStamina()
    {
        if (IsSprinting)
        {
            _stamina -= _staminaDepletionRate * Time.deltaTime;
            if (_stamina <= 0f)
            {
                _stamina = 0f;
                _isSprinting = false; // Stop sprinting
            }
            _staminaDelayTimer = _staminaRegenDelay;
        }
        else
        {
            if (_staminaDelayTimer > 0f)
            {
                _staminaDelayTimer -= Time.deltaTime;
            }
            else
            {
                _stamina += _staminaRegenRate * Time.deltaTime;
                if (_stamina > _maxStamina)
                {
                    _stamina = _maxStamina;
                }
            }
        }
    }

    void ApplyCameraFov()
    {
        if (_camera == null) return;

        float targetFov = _defaultFov;
        if (_isSliding)
        {
            targetFov = _defaultFov * _fovSprintMultiplier * 1.05f;
        }
        else if (IsSprinting)
        {
            targetFov = _defaultFov * _fovSprintMultiplier;
        }

        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, _fovTransitionSpeed * Time.deltaTime);
    }

    void ApplyHeadBob()
    {
        if (_cameraTransform == null) return;

        float targetBobOffsetX = 0f;
        float targetBobOffsetY = 0f;

        if (IsSprinting && _enableSprintHeadBob)
        {
            _bobTimer += Time.deltaTime * _bobFrequency;
            targetBobOffsetY = Mathf.Sin(_bobTimer) * _bobAmount;
            targetBobOffsetX = Mathf.Cos(_bobTimer * 0.5f) * _bobAmount * 0.5f;
        }
        else
        {
            _bobTimer = 0f;
        }

        _currentBobOffsetX = Mathf.Lerp(_currentBobOffsetX, targetBobOffsetX, 10f * Time.deltaTime);
        _currentBobOffsetY = Mathf.Lerp(_currentBobOffsetY, targetBobOffsetY, 10f * Time.deltaTime);

        // Smoothly decay landing dip offset
        _landingDipOffset = Mathf.Lerp(_landingDipOffset, 0f, 10f * Time.deltaTime);

        float finalCameraY = _currentCameraY + _currentBobOffsetY - _landingDipOffset;
        _cameraTransform.localPosition = new Vector3(_currentBobOffsetX, finalCameraY, _cameraTransform.localPosition.z);
    }

    // NARROW INTERFACE — PlayerInput "Send Messages" callbacks; one line each, no logic
    void OnMove(InputValue v)   => _moveInput   = v.Get<Vector2>();
    void OnLook(InputValue v)   => _lookInput   = v.Get<Vector2>();
    void OnJump(InputValue v)   
    {
        if (v.isPressed)
        {
            _jumpPressed = true;
            _jumpBufferTimer = _jumpBufferTime;
        }
    }
    void OnSprint(InputValue v)
    {
        if (v.isPressed)
        {
            if (_wantsToCrouch || _isCrouching)
            {
                _wantsToCrouch = false;
                if (_stamina > 5f)
                {
                    _isSprinting = true;
                }
            }
            else
            {
                if (_sprintIsToggle)
                {
                    if (_isSprinting)
                    {
                        _isSprinting = false;
                    }
                    else if (_stamina > 5f)
                    {
                        _isSprinting = true;
                    }
                }
                else
                {
                    if (_stamina > 5f)
                    {
                        _isSprinting = true;
                    }
                }
            }
        }
        else
        {
            if (!_sprintIsToggle)
            {
                _isSprinting = false;
            }
        }
    }
    void OnCrouch(InputValue v)
    {
        if (v.isPressed)
        {
            Vector3 horizontalVel = new Vector3(_velocity.x, 0f, _velocity.z);
            float currentHorizontalSpeed = horizontalVel.magnitude;

            if (_isGrounded && !_isSliding && _slideCooldownTimer <= 0f && (IsSprinting || currentHorizontalSpeed >= _minSlideSpeed) && _moveInput.y > -0.1f)
            {
                StartSlide(currentHorizontalSpeed);
            }
            else if (_isSliding)
            {
                StopSlide();
            }
            else
            {
                _wantsToCrouch = !_wantsToCrouch;
                if (_wantsToCrouch)
                {
                    _isSprinting = false; // Crouching cancels any active sprint toggle/state
                }
            }
        }
    }

    void StartSlide(float currentSpeed)
    {
        _isSliding = true;
        _isSprinting = false;
        _wantsToCrouch = true;

        Vector3 moveDir = (transform.right * _moveInput.x + transform.forward * _moveInput.y).normalized;
        if (moveDir.sqrMagnitude < 0.01f)
        {
            moveDir = transform.forward;
        }

        _slideDirection = moveDir;
        _slideSpeed = Mathf.Max(currentSpeed * 1.15f, _slideInitialBoost);
    }

    void StopSlide()
    {
        _isSliding = false;
        _slideCooldownTimer = _slideCooldown;
    }

    // DEEP IMPLEMENTATION — non-obvious decisions buried here so callers never need to know
    void CheckGroundStatus()
    {
        float radius = _cc.radius;
        // Center of the bottom hemisphere of the capsule
        Vector3 bottomSphereCenter = transform.position + _cc.center + Vector3.down * (_cc.height * 0.5f - radius);
        
        // Start the spherecast slightly above the bottom sphere center to ensure it detects the ground
        float offset = 0.1f;
        Vector3 origin = bottomSphereCenter + Vector3.up * offset;
        float castDistance = offset + _groundCheckDistance;

        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, castDistance, _groundLayers, QueryTriggerInteraction.Ignore))
        {
            _isGrounded = true;
            _groundNormal = hit.normal;

            // Check if slope is too steep
            float slopeAngle = Vector3.Angle(Vector3.up, _groundNormal);
            if (slopeAngle > _slopeLimit)
            {
                _isOnSteepSlope = true;
                // Calculate slide direction down the slope
                _slopeSlideDirection = Vector3.ProjectOnPlane(Vector3.down, _groundNormal).normalized;
            }
            else
            {
                _isOnSteepSlope = false;
                _slopeSlideDirection = Vector3.zero;
            }
        }
        else
        {
            _isGrounded = false;
            _groundNormal = Vector3.up;
            _isOnSteepSlope = false;
            _slopeSlideDirection = Vector3.zero;
        }
    }

    void ApplyGravityAndJumping()
    {
        if (_isGrounded)
        {
            // Reset vertical velocity when grounded; small negative forces down onto slopes
            if (_velocity.y < 0f)
            {
                _velocity.y = -2f;
            }

            // Regular jump is only allowed on walkable slopes
            if (_jumpPressed && !_isOnSteepSlope)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity); // kinematics: v = sqrt(h * -2g)
            }
        }
        else
        {
            // Apply gravity over time
            _velocity.y += _gravity * Time.deltaTime;
        }

        _jumpPressed = false; // consumed and cleared internally; caller never manages this
    }

    void ApplyMovement()
    {
        if (_isSliding)
        {
            ApplySlideMovement();
            return;
        }

        if (_sprintIsToggle && _isSprinting && _moveInput.sqrMagnitude < 0.01f)
        {
            _isSprinting = false;
        }

        // Calculate Target Ground Direction
        Vector3 rawDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        Vector3 targetDirection = rawDirection.normalized;

        // Project movement onto the slope plane so there's no upward bounce or speed loss
        if (_isGrounded)
        {
            targetDirection = Vector3.ProjectOnPlane(targetDirection, _groundNormal).normalized;
        }

        // Determine target speed
        float speed = _isSprinting ? _sprintSpeed : _walkSpeed;
        if (_isCrouching || _cc.height < _defaultHeight - 0.05f)
        {
            speed = _crouchSpeed;
        }
        Vector3 targetVelocity = targetDirection * speed;

        // Apply Acceleration / Deceleration / Air Control
        float accelRate;
        if (_isGrounded)
        {
            // Accelerate if input is active, decelerate if input is idle
            accelRate = (rawDirection.magnitude > 0.01f) ? _acceleration : _deceleration;
        }
        else
        {
            // Reduced air control in airborne state
            accelRate = _acceleration * _airControl;
        }

        // Extract horizontal components of velocity to apply momentum smoothly
        Vector3 horizontalVelocity = new Vector3(_velocity.x, 0f, _velocity.z);

        if (_isGrounded && _isOnSteepSlope)
        {
            // Slide down steep slope
            Vector3 slideAccel = _slopeSlideDirection * (Mathf.Abs(_gravity) * Time.deltaTime);
            horizontalVelocity += slideAccel;

            // Give player limited control to steer away/around while sliding
            Vector3 steerVel = targetDirection * (_walkSpeed * _slideControl);
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, steerVel, Time.deltaTime);
        }
        else
        {
            // Standard smooth movement
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, accelRate * Time.deltaTime);
        }

        // Recombine horizontal and vertical velocity
        _velocity.x = horizontalVelocity.x;
        _velocity.z = horizontalVelocity.z;

        // Move the CharacterController
        _cc.Move(_velocity * Time.deltaTime);
    }

    void ApplySlideMovement()
    {
        // 1. If airborne (slid off a ledge)
        if (!_isGrounded)
        {
            _velocity.x = _slideDirection.x * _slideSpeed;
            _velocity.z = _slideDirection.z * _slideSpeed;
            StopSlide();
            return;
        }

        // 2. Slide Jump (jump out of slide with preserved boosted momentum)
        if (_jumpPressed)
        {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            _velocity.x = _slideDirection.x * _slideSpeed;
            _velocity.z = _slideDirection.z * _slideSpeed;
            _jumpPressed = false;
            _wantsToCrouch = false;
            StopSlide();
            _cc.Move(_velocity * Time.deltaTime);
            return;
        }

        // 3. Slope assistance & flat surface friction
        if (_isOnSteepSlope || _slopeSlideDirection.sqrMagnitude > 0.01f)
        {
            float slopeDot = Vector3.Dot(_slideDirection, _slopeSlideDirection);
            if (slopeDot > 0f)
            {
                // Sliding downhill accelerates slide speed
                _slideSpeed += slopeDot * Mathf.Abs(_gravity) * 0.8f * Time.deltaTime;
            }
            else
            {
                // Sliding uphill decelerates fast
                _slideSpeed -= _slideFriction * 1.5f * Time.deltaTime;
            }
        }
        else
        {
            // Flat surface friction
            _slideSpeed -= _slideFriction * Time.deltaTime;
        }

        // 4. Steering control
        if (_moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 steerDir = (transform.right * _moveInput.x + transform.forward * _moveInput.y).normalized;
            _slideDirection = Vector3.Slerp(_slideDirection, steerDir, _slideSteerControl * Time.deltaTime).normalized;
        }

        // 5. Minimum speed threshold / un-crouch exit check
        if (_slideSpeed <= _minEndSlideSpeed || !_wantsToCrouch)
        {
            StopSlide();
            return;
        }

        // 6. Execute slide move projected on ground normal
        Vector3 finalSlideVel = Vector3.ProjectOnPlane(_slideDirection, _groundNormal).normalized * _slideSpeed;
        _velocity.x = finalSlideVel.x;
        _velocity.z = finalSlideVel.z;

        _cc.Move(_velocity * Time.deltaTime);
    }

    void ApplyLook()
    {
        // Rotate body around Y-axis (Yaw)
        transform.Rotate(Vector3.up, _lookInput.x * _sensitivity);

        // Clamp head rotation around X-axis (Pitch)
        _pitch = Mathf.Clamp(_pitch - _lookInput.y * _sensitivity, -_pitchClamp, _pitchClamp);

        // Dynamic camera roll/tilt when sliding or strafing
        float targetRoll = _isSliding ? _slideCameraTilt : -_moveInput.x * _strafeCameraTilt;
        _currentCameraRoll = Mathf.Lerp(_currentCameraRoll, targetRoll, 10f * Time.deltaTime);

        _cameraTransform.localEulerAngles = new Vector3(_pitch, 0f, _currentCameraRoll);
    }

    void ApplyCrouch()
    {
        if (_wantsToCrouch)
        {
            _isCrouching = true;
        }
        else if (_isCrouching)
        {
            // Perform ceiling check
            Vector3 start = transform.position + _cc.center;
            float castDistance = _defaultHeight - _cc.height;
            if (castDistance > 0.01f)
            {
                float radius = _cc.radius - 0.05f;
                // Ray origin at the current top of the crouch capsule
                Vector3 origin = transform.position + Vector3.up * (_crouchHeight - radius);
                if (Physics.SphereCast(origin, radius, Vector3.up, out RaycastHit hit, castDistance + 0.1f, _groundLayers, QueryTriggerInteraction.Ignore))
                {
                    // Blocked! Remain crouching.
                    _isCrouching = true;
                }
                else
                {
                    _isCrouching = false;
                }
            }
            else
            {
                _isCrouching = false;
            }
        }

        float targetHeight = _isSliding ? _slideHeight : (_isCrouching ? _crouchHeight : _defaultHeight);
        float speed = _crouchTransitionSpeed * Time.deltaTime;
        
        _cc.height = Mathf.MoveTowards(_cc.height, targetHeight, speed);
        float targetCenterY = (_cc.height - _defaultHeight) * 0.5f;
        _cc.center = new Vector3(0f, targetCenterY, 0f);

        if (_cameraTransform != null)
        {
            float effectiveCrouchHeight = _isSliding ? _slideHeight : _crouchHeight;
            float targetCamY = (_isCrouching || _isSliding) ? (_defaultCameraY - (_defaultHeight - effectiveCrouchHeight) * 0.5f) : _defaultCameraY;
            _currentCameraY = Mathf.MoveTowards(_currentCameraY, targetCamY, speed);
        }
    }

    void ApplyLadderClimbing()
    {
        // Smoothly climbing up/down on Y axis
        float climbSpeed = (_currentLadder != null && _currentLadder.ClimbSpeedOverride > 0f) ? _currentLadder.ClimbSpeedOverride : _ladderClimbSpeed;
        _velocity.y = _moveInput.y * climbSpeed;
        
        // Minor horizontal adjustments
        Vector3 rawDirection = transform.right * _moveInput.x;
        _velocity.x = rawDirection.x * climbSpeed * 0.5f;
        _velocity.z = rawDirection.z * climbSpeed * 0.5f;

        // Check if player jumps to dismount ladder
        if (_jumpPressed)
        {
            _isClimbing = false;
            _currentLadder = null;
            
            // Jump away from ladder
            Vector3 pushDirection = -transform.forward; // Push off backward
            pushDirection.y = 1f; // up
            _velocity = pushDirection.normalized * Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            _jumpPressed = false;
            
            _cc.Move(_velocity * Time.deltaTime);
            return;
        }

        _cc.Move(_velocity * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ladder") || other.GetComponent<Ladder>() != null)
        {
            _isClimbing = true;
            _currentLadder = other.GetComponent<Ladder>();
            _velocity = Vector3.zero; // reset movement velocity
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (_isClimbing && _currentLadder != null && other.gameObject == _currentLadder.gameObject)
        {
            _isClimbing = false;
            _currentLadder = null;
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        // Don't push if no rigidbody or kinematic
        if (body == null || body.isKinematic)
            return;

        // Don't push objects below us
        if (hit.moveDirection.y < -0.3f)
            return;

        // Calculate push direction from move direction, horizontal only
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);

        // Apply push force relative to velocity
        body.AddForceAtPosition(pushDir * _pushForce, hit.point, ForceMode.Impulse);
    }
}
