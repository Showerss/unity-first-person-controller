using UnityEngine;

namespace FirstPerson.Interaction
{
    /// <summary>
    /// A swinging or sliding door, opened by Interact or automatically when the player walks into its trigger.
    /// Moves along eased curves (a small overshoot on open, a firm settle on close), swings away from the player,
    /// and can rattle when something (e.g. a lock) refuses to open it. Only ticks while it is moving.
    /// </summary>
    public class Door : MonoBehaviour, IInteractable
    {
        public enum DoorType { Swinging, Sliding }
        public enum ActivationMode { Interactive, Automatic }

        [Header("Door Configuration")]
        [Tooltip("Swinging rotates between the two rotations; Sliding moves between the two positions.")]
        [SerializeField] private DoorType _doorType = DoorType.Swinging;

        [Tooltip("Interactive opens on the Interact key; Automatic opens while the player is inside a trigger on this object.")]
        [SerializeField] private ActivationMode _activationMode = ActivationMode.Interactive;

        [Tooltip("Prompt while closed. The HUD adds the key.")]
        [SerializeField] private string _promptText = "Open door";

        [Tooltip("Prompt while open. The HUD adds the key.")]
        [SerializeField] private string _closePromptText = "Close door";

        [Header("Motion")]
        [Tooltip("Seconds for a full open.")]
        [SerializeField] private float _openDuration = 0.6f;

        [Tooltip("Seconds for a full close.")]
        [SerializeField] private float _closeDuration = 0.5f;

        [Tooltip("Easing for opening, 0..1 over time. Values above 1 overshoot and settle back.")]
        [SerializeField] private AnimationCurve _openCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2.6f), new Keyframe(0.65f, 1.05f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f));

        [Tooltip("Easing for closing, 0..1 over time. Keep it at or below 1 so the door doesn't pass through its frame.")]
        [SerializeField] private AnimationCurve _closeCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0.6f), new Keyframe(1f, 1f, 1.8f, 0f));

        [Header("Swinging Settings")]
        [Tooltip("Local rotation when closed.")]
        [SerializeField] private Vector3 _closedRotation = Vector3.zero;

        [Tooltip("Local rotation when open.")]
        [SerializeField] private Vector3 _openRotation = new Vector3(0f, 90f, 0f);

        [Tooltip("Mirror the swing so the door always opens away from whoever opens it.")]
        [SerializeField] private bool _swingAwayFromPlayer = true;

        [Header("Sliding Settings")]
        [Tooltip("Local position when closed.")]
        [SerializeField] private Vector3 _closedPosition = Vector3.zero;

        [Tooltip("Local position when open.")]
        [SerializeField] private Vector3 _openPosition = new Vector3(0f, 3f, 0f);

        [Header("Rattle (refused)")]
        [Tooltip("Seconds the door shakes when Rattle() is called (e.g. locked).")]
        [SerializeField] private float _rattleDuration = 0.3f;

        [Tooltip("Shake size: degrees for swinging doors, centimetres for sliding doors.")]
        [SerializeField] private float _rattleAmount = 1.5f;

        [Tooltip("Shake frequency in Hz.")]
        [SerializeField] private float _rattleFrequency = 22f;

        [Header("Audio (optional)")]
        [Tooltip("Plays the clips below. Leave empty for a silent door.")]
        [SerializeField] private AudioSource _audioSource;

        [Tooltip("Played as the door starts opening.")]
        [SerializeField] private AudioClip _openSound;

        [Tooltip("Played when the door finishes closing.")]
        [SerializeField] private AudioClip _closeSound;

        [Tooltip("Played by Rattle(). Leave empty when the lock plays its own sound.")]
        [SerializeField] private AudioClip _rattleSound;

        private bool _isOpen;
        private int _triggerCount; // For automatic mode (handles multiple players)
        private InteractionPrompt _openPrompt;
        private InteractionPrompt _closePrompt;

        // Tween state: the pose is LerpUnclamped(closed, open, _value).
        private float _value;
        private float _from;
        private float _elapsed;
        private float _duration;
        private float _rattleTime;
        private float _swingSign = 1f;
        private Vector3 _leafCentre; // local-space centre of the leaf, for picking the swing side

        public bool IsOpen => _isOpen;

        void Awake()
        {
            _openPrompt = new InteractionPrompt(_promptText);
            _closePrompt = new InteractionPrompt(_closePromptText);
        }

        void Start()
        {
            ApplyPose(0f, 0f);
            _leafCentre = FindLeafCentre();

            // At rest: no Update until something moves the door. Trigger messages still arrive while disabled.
            enabled = false;
        }

        void Update()
        {
            if (_elapsed < _duration)
            {
                _elapsed = Mathf.Min(_elapsed + Time.deltaTime, _duration);
                float eased = (_isOpen ? _openCurve : _closeCurve).Evaluate(_elapsed / _duration);
                _value = Mathf.LerpUnclamped(_from, _isOpen ? 1f : 0f, eased);

                if (_elapsed >= _duration)
                {
                    _value = _isOpen ? 1f : 0f;
                    if (!_isOpen) Play(_closeSound);
                }
            }

            float shake = 0f;
            if (_rattleTime > 0f)
            {
                _rattleTime = Mathf.Max(0f, _rattleTime - Time.deltaTime);
                float fade = _rattleTime / _rattleDuration;
                shake = Mathf.Sin(_rattleTime * _rattleFrequency * Mathf.PI * 2f) * _rattleAmount * fade;
            }

            ApplyPose(_value, shake);

            if (_elapsed >= _duration && _rattleTime <= 0f) enabled = false;
        }

        /// <summary>Shake in place to show the door refuses to open (e.g. locked). Ignored while moving.</summary>
        public void Rattle()
        {
            if (_elapsed < _duration || _rattleDuration <= 0f) return;
            _rattleTime = _rattleDuration;
            Play(_rattleSound);
            enabled = true;
        }

        private void SetOpen(bool open, Vector3 openerPosition)
        {
            if (open == _isOpen) return;

            // Pick the swing side only from fully closed, so a reversal mid-swing doesn't snap across.
            if (open && _value <= 0f && _doorType == DoorType.Swinging && _swingAwayFromPlayer)
            {
                _swingSign = IsOnSwingSide(openerPosition) ? -1f : 1f;
            }

            _isOpen = open;
            _from = _value;
            _elapsed = 0f;
            float remaining = open ? 1f - Mathf.Clamp01(_value) : Mathf.Clamp01(_value);
            _duration = Mathf.Max(0.01f, (open ? _openDuration : _closeDuration) * remaining);

            if (open) Play(_openSound);
            enabled = true;
        }

        private void ApplyPose(float value, float shake)
        {
            if (_doorType == DoorType.Swinging)
            {
                Vector3 open = _closedRotation + (_openRotation - _closedRotation) * _swingSign;
                transform.localRotation = Quaternion.Euler(Vector3.LerpUnclamped(_closedRotation, open, value))
                                          * Quaternion.Euler(0f, shake, 0f);
            }
            else
            {
                Vector3 pos = Vector3.LerpUnclamped(_closedPosition, _openPosition, value);
                transform.localPosition = pos + transform.localRotation * Vector3.right * (shake * 0.01f);
            }
        }

        /// <summary>True if the position is on the side the default swing pushes the leaf towards.</summary>
        private bool IsOnSwingSide(Vector3 position)
        {
            Vector3 closedCentre = Quaternion.Euler(_closedRotation) * _leafCentre;
            Vector3 openCentre = Quaternion.Euler(_openRotation) * _leafCentre;
            Vector3 swing = openCentre - closedCentre;

            // Keep only the part of the swing across the doorway, not along the leaf.
            Vector3 along = Vector3.ProjectOnPlane(closedCentre, Vector3.up).normalized;
            Vector3 across = Vector3.ProjectOnPlane(swing - along * Vector3.Dot(swing, along), Vector3.up);

            Vector3 local = transform.parent != null ? transform.parent.InverseTransformPoint(position) : position;
            return Vector3.Dot(across, local - (transform.localPosition + closedCentre)) > 0f;
        }

        private Vector3 FindLeafCentre()
        {
            Renderer leaf = GetComponentInChildren<Renderer>();
            if (leaf != null) return transform.InverseTransformPoint(leaf.bounds.center);

            Collider col = GetComponentInChildren<Collider>();
            return col != null ? transform.InverseTransformPoint(col.bounds.center) : Vector3.zero;
        }

        private void Play(AudioClip clip)
        {
            if (_audioSource != null && clip != null) _audioSource.PlayOneShot(clip);
        }

        // --- IInteractable Implementation ---
        public InteractionPrompt GetPrompt(PlayerInteraction interactor)
        {
            if (_activationMode == ActivationMode.Automatic) return InteractionPrompt.None;
            return _isOpen ? _closePrompt : _openPrompt;
        }

        public void Interact(PlayerInteraction interactor)
        {
            if (_activationMode == ActivationMode.Interactive)
            {
                SetOpen(!_isOpen, interactor.transform.position);
            }
        }

        // --- Automatic Trigger Detection ---
        void OnTriggerEnter(Collider other)
        {
            if (_activationMode == ActivationMode.Automatic && IsPlayer(other))
            {
                _triggerCount++;
                SetOpen(true, other.transform.position);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (_activationMode == ActivationMode.Automatic && IsPlayer(other))
            {
                _triggerCount = Mathf.Max(0, _triggerCount - 1);
                if (_triggerCount == 0)
                {
                    SetOpen(false, other.transform.position);
                }
            }
        }

        private static bool IsPlayer(Collider other)
        {
            return other.GetComponent<CharacterController>() != null || other.CompareTag("Player");
        }
    }
}
