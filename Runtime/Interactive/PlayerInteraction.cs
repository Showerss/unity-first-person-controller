using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float _interactRange = 2.5f;
    [SerializeField] private LayerMask _interactLayers = ~0; // Look at all layers by default

    private Transform _cameraTransform;
    private IInteractable _currentInteractable;

    public string CurrentPrompt { get; private set; } = string.Empty;

    void Awake()
    {
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            _cameraTransform = cam.transform;
        }
        else if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            _currentInteractable = null;
            CurrentPrompt = string.Empty;
            return;
        }

        FindInteractable();
    }

    private void FindInteractable()
    {
        if (_cameraTransform == null) return;

        Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, _interactRange, _interactLayers, QueryTriggerInteraction.Ignore))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                interactable = hit.collider.GetComponent<IInteractable>();
            }

            if (interactable != null)
            {
                _currentInteractable = interactable;
                CurrentPrompt = interactable.GetPromptText();
                return;
            }
        }

        _currentInteractable = null;
        CurrentPrompt = string.Empty;
    }

    // Called automatically by PlayerInput SendMessages behavior when "Interact" action is performed
    void OnInteract(InputValue v)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        if (v.isPressed && _currentInteractable != null)
        {
            _currentInteractable.Interact(gameObject);
        }
    }
}
