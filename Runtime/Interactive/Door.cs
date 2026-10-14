using UnityEngine;

namespace FirstPerson.Interaction
{
    /// <summary>
    /// A swinging or sliding door, opened by Interact or automatically when the player walks into its trigger.
    /// Only ticks while it is moving.
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

        [Tooltip("How quickly the door eases towards its target.")]
        [SerializeField] private float _animationSpeed = 4f;

        [Tooltip("Prompt while closed. The HUD adds the key.")]
        [SerializeField] private string _promptText = "Open door";

        [Tooltip("Prompt while open. The HUD adds the key.")]
        [SerializeField] private string _closePromptText = "Close door";

        [Header("Swinging Settings")]
        [Tooltip("Local rotation when closed.")]
        [SerializeField] private Vector3 _closedRotation = Vector3.zero;

        [Tooltip("Local rotation when open.")]
        [SerializeField] private Vector3 _openRotation = new Vector3(0f, 90f, 0f);

        [Header("Sliding Settings")]
        [Tooltip("Local position when closed.")]
        [SerializeField] private Vector3 _closedPosition = Vector3.zero;

        [Tooltip("Local position when open.")]
        [SerializeField] private Vector3 _openPosition = new Vector3(0f, 3f, 0f);

        private bool _isOpen = false;
        private int _triggerCount = 0; // For automatic mode (handles multiple players)
        private InteractionPrompt _openPrompt;
        private InteractionPrompt _closePrompt;

        void Awake()
        {
            _openPrompt = new InteractionPrompt(_promptText);
            _closePrompt = new InteractionPrompt(_closePromptText);
        }

        void Start()
        {
            // Set initial state
            if (_doorType == DoorType.Swinging)
            {
                transform.localEulerAngles = _closedRotation;
            }
            else
            {
                transform.localPosition = _closedPosition;
            }

            // At rest: no Update until something moves the door. Trigger messages still arrive while disabled.
            enabled = false;
        }

        void Update()
        {
            AnimateDoor();
        }

        private void AnimateDoor()
        {
            float speed = _animationSpeed * Time.deltaTime;

            if (_doorType == DoorType.Swinging)
            {
                Quaternion targetRot = Quaternion.Euler(_isOpen ? _openRotation : _closedRotation);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, speed);

                if (Quaternion.Angle(transform.localRotation, targetRot) < 0.1f)
                {
                    transform.localRotation = targetRot;
                    enabled = false;
                }
            }
            else
            {
                Vector3 targetPos = _isOpen ? _openPosition : _closedPosition;
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, speed);

                if ((transform.localPosition - targetPos).sqrMagnitude < 0.000001f)
                {
                    transform.localPosition = targetPos;
                    enabled = false;
                }
            }
        }

        private void SetOpen(bool open)
        {
            _isOpen = open;
            enabled = true;
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
                SetOpen(!_isOpen);
            }
        }

        // --- Automatic Trigger Detection ---
        void OnTriggerEnter(Collider other)
        {
            if (_activationMode == ActivationMode.Automatic)
            {
                if (other.GetComponent<CharacterController>() != null || other.CompareTag("Player"))
                {
                    _triggerCount++;
                    SetOpen(true);
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (_activationMode == ActivationMode.Automatic)
            {
                if (other.GetComponent<CharacterController>() != null || other.CompareTag("Player"))
                {
                    _triggerCount = Mathf.Max(0, _triggerCount - 1);
                    if (_triggerCount == 0)
                    {
                        SetOpen(false);
                    }
                }
            }
        }
    }
}
