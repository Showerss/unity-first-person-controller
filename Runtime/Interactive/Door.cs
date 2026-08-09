using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    public enum DoorType { Swinging, Sliding }
    public enum ActivationMode { Interactive, Automatic }

    [Header("Door Configuration")]
    [SerializeField] private DoorType _doorType = DoorType.Swinging;
    [SerializeField] private ActivationMode _activationMode = ActivationMode.Interactive;
    [SerializeField] private float _animationSpeed = 4f;
    [SerializeField] private string _promptText = "Open Door";

    [Header("Swinging Settings")]
    [SerializeField] private Vector3 _closedRotation = Vector3.zero;
    [SerializeField] private Vector3 _openRotation = new Vector3(0f, 90f, 0f);

    [Header("Sliding Settings")]
    [SerializeField] private Vector3 _closedPosition = Vector3.zero;
    [SerializeField] private Vector3 _openPosition = new Vector3(0f, 3f, 0f);

    private bool _isOpen = false;
    private int _triggerCount = 0; // For automatic mode (handles multiple players)

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
            Vector3 targetEuler = _isOpen ? _openRotation : _closedRotation;
            Quaternion targetRot = Quaternion.Euler(targetEuler);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, speed);
        }
        else
        {
            Vector3 targetPos = _isOpen ? _openPosition : _closedPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, speed);
        }
    }

    // --- IInteractable Implementation ---
    public void Interact(GameObject player)
    {
        if (_activationMode == ActivationMode.Interactive)
        {
            _isOpen = !_isOpen;

            // Notify player of interaction
            FirstPersonController controller = player.GetComponent<FirstPersonController>();
            if (controller != null)
            {
                controller.StartDoorInteraction();
            }
        }
    }

    public string GetPromptText()
    {
        if (_activationMode == ActivationMode.Automatic)
        {
            return string.Empty;
        }
        return _isOpen ? "Close Door" : _promptText;
    }

    // --- Automatic Trigger Detection ---
    void OnTriggerEnter(Collider other)
    {
        if (_activationMode == ActivationMode.Automatic)
        {
            if (other.GetComponent<CharacterController>() != null || other.CompareTag("Player"))
            {
                _triggerCount++;
                _isOpen = true;
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
                    _isOpen = false;
                }
            }
        }
    }
}
