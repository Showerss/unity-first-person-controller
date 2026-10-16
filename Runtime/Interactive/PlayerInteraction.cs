using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstPerson.Interaction
{
    /// <summary>
    /// Finds the IInteractable the player is looking at and interacts with it when the Interact action fires.
    /// Trigger colliders count, so a doorway volume needs no solid collider; any solid collider
    /// without an interactable blocks the view. Disable this component to stop interaction (e.g. during dialogue).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        const string InteractActionName = "Interact";
        const int MaxHits = 16;

        [Tooltip("Camera the look ray starts from. Defaults to the first Camera under this object.")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("How far the player can reach, in metres.")]
        [SerializeField] private float _interactRange = 2.5f;

        [Tooltip("Radius of the fallback sphere cast, in metres, so small targets like lamps are easy to aim at. 0 disables it.")]
        [SerializeField] private float _aimAssistRadius = 0.15f;

        [Tooltip("Layers the look ray can hit.")]
        [SerializeField] private LayerMask _interactLayers = ~0;

        readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
        PlayerInput _playerInput;
        Collider _bestCollider;
        Collider _currentCollider;
        Transform _currentAnchor;

        /// <summary>The interactable the player is looking at, or null.</summary>
        public IInteractable Current { get; private set; }

        public InteractionPrompt CurrentPrompt { get; private set; }

        /// <summary>World point where the focused target's prompt is drawn; see <see cref="InteractionMarkerAnchor"/>.</summary>
        public Vector3 CurrentAnchor => InteractionMarkerAnchor.Position(_currentAnchor, _currentCollider, Current as Component);

        /// <summary>The look ray's starting point, shared with <see cref="InteractableScanner"/>.</summary>
        public Transform CameraTransform => _cameraTransform;

        /// <summary>Layers the look ray can hit.</summary>
        public LayerMask InteractLayers => _interactLayers;

        /// <summary>The Interact binding in the active control scheme, e.g. "E".</summary>
        public string InteractKeyLabel { get; private set; } = "E";

        public event Action<IInteractable> FocusChanged;

        /// <summary>Raised when the prompt or the key label changes; the HUD redraws from this alone.</summary>
        public event Action<InteractionPrompt> PromptChanged;

        public event Action<IInteractable> Interacted;

        void Awake()
        {
            if (_cameraTransform == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) _cameraTransform = cam.transform;
                else Debug.LogError($"[PlayerInteraction] No camera assigned or found under '{name}'.", this);
            }

            _playerInput = GetComponent<PlayerInput>();
        }

        void OnEnable()
        {
            if (_playerInput != null) _playerInput.onControlsChanged += HandleControlsChanged;
            RefreshKeyLabel();
        }

        void OnDisable()
        {
            if (_playerInput != null) _playerInput.onControlsChanged -= HandleControlsChanged;
            SetFocus(null, InteractionPrompt.None);
        }

        void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked || _cameraTransform == null)
            {
                SetFocus(null, InteractionPrompt.None);
                return;
            }

            IInteractable target = FindInteractable(out InteractionPrompt prompt);
            _currentCollider = target != null ? _bestCollider : null;
            SetFocus(target, prompt);
        }

        // Called by PlayerInput (Send Messages) when the Interact action is performed.
        void OnInteract(InputValue v)
        {
            // SendMessage reaches disabled components too, so check explicitly.
            if (!isActiveAndEnabled || !v.isPressed || Current == null) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;

            IInteractable target = Current;
            target.Interact(this);
            Interacted?.Invoke(target);
        }

        IInteractable FindInteractable(out InteractionPrompt prompt)
        {
            Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);

            int count = Physics.RaycastNonAlloc(ray, _hits, _interactRange, _interactLayers, QueryTriggerInteraction.Collide);
            IInteractable target = PickNearest(count, false, out prompt, out float blockDistance);
            if (target != null || _aimAssistRadius <= 0f) return target;

            // Aim assist: a fatter cast, but never past whatever solid thing the centre ray hit.
            count = Physics.SphereCastNonAlloc(ray, _aimAssistRadius, _hits, blockDistance, _interactLayers, QueryTriggerInteraction.Collide);
            return PickNearest(count, true, out prompt, out _);
        }

        /// <summary>
        /// Nearest hit with a non-empty prompt that isn't behind a solid blocker.
        /// Sphere casts ignore blockers (the ray already bounded the distance) and initial overlaps.
        /// </summary>
        IInteractable PickNearest(int count, bool isSphere, out InteractionPrompt prompt, out float blockDistance)
        {
            IInteractable best = null;
            InteractionPrompt bestPrompt = InteractionPrompt.None;
            float bestDistance = float.MaxValue;
            blockDistance = _interactRange;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (isSphere && hit.distance <= 0f) continue;

                IInteractable candidate = hit.collider.GetComponentInParent<IInteractable>();
                InteractionPrompt candidatePrompt = candidate != null ? candidate.GetPrompt(this) : InteractionPrompt.None;

                if (!candidatePrompt.IsEmpty)
                {
                    if (hit.distance < bestDistance)
                    {
                        best = candidate;
                        bestPrompt = candidatePrompt;
                        bestDistance = hit.distance;
                        _bestCollider = hit.collider;
                    }
                }
                else if (!isSphere && !hit.collider.isTrigger && hit.distance < blockDistance)
                {
                    blockDistance = hit.distance;
                }
            }

            if (best != null && bestDistance <= blockDistance)
            {
                prompt = bestPrompt;
                return best;
            }

            prompt = InteractionPrompt.None;
            return null;
        }

        void SetFocus(IInteractable target, InteractionPrompt prompt)
        {
            if (!ReferenceEquals(target, Current))
            {
                Current = target;
                _currentAnchor = InteractionMarkerAnchor.Find(target as Component);
                if (target == null) _currentCollider = null;
                FocusChanged?.Invoke(target);
            }

            if (prompt.Equals(CurrentPrompt)) return;
            CurrentPrompt = prompt;
            PromptChanged?.Invoke(prompt);
        }

        void HandleControlsChanged(PlayerInput _) => RefreshKeyLabel();

        void RefreshKeyLabel()
        {
            InputAction action = _playerInput != null && _playerInput.actions != null
                ? _playerInput.actions.FindAction(InteractActionName)
                : null;
            if (action == null) return;

            string scheme = _playerInput.currentControlScheme;
            string label = string.IsNullOrEmpty(scheme)
                ? action.GetBindingDisplayString()
                : action.GetBindingDisplayString(InputBinding.MaskByGroup(scheme));

            if (string.IsNullOrEmpty(label) || label == InteractKeyLabel) return;
            InteractKeyLabel = label;
            if (!CurrentPrompt.IsEmpty) PromptChanged?.Invoke(CurrentPrompt);
        }
    }
}
