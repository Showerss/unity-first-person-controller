using System.Collections.Generic;
using UnityEngine;

namespace FirstPerson.Interaction
{
    /// <summary>
    /// Finds every usable interactable near the player, for HUD markers (see InteractionMarkerHUD).
    /// "Usable" means the same thing as for PlayerInteraction: a non-empty prompt. Rescans on an interval;
    /// readers project the markers every frame, so moving targets are tracked smoothly.
    /// Goes quiet when PlayerInteraction is disabled (dialogue, cutscenes) or the cursor is unlocked (menus).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInteraction))]
    public sealed class InteractableScanner : MonoBehaviour
    {
        const int MaxColliders = 64;

        /// <summary>One nearby interactable and where to draw it.</summary>
        public readonly struct Marker
        {
            public Marker(IInteractable target, Component owner, Collider collider, Transform anchor)
            {
                Target = target;
                Owner = owner;
                Collider = collider;
                Anchor = anchor;
            }

            public IInteractable Target { get; }
            public Component Owner { get; }
            public Collider Collider { get; }
            public Transform Anchor { get; }

            /// <summary>World point to draw at.</summary>
            public Vector3 Position => InteractionMarkerAnchor.Position(Anchor, Collider, Owner);

            /// <summary>False once the target has been destroyed (e.g. a pickup that was taken).</summary>
            public bool IsAlive => Owner != null;
        }

        [Tooltip("How far away, in metres, interactables get a marker dot.")]
        [SerializeField] private float _markerRange = 8f;

        [Tooltip("Seconds between scans. Markers still follow their targets every frame in between.")]
        [SerializeField] private float _scanInterval = 0.1f;

        [Tooltip("Hide markers on interactables the camera can't see (behind walls).")]
        [SerializeField] private bool _requireLineOfSight = true;

        readonly Collider[] _colliders = new Collider[MaxColliders];
        readonly HashSet<IInteractable> _seen = new HashSet<IInteractable>();
        readonly List<Marker> _markers = new List<Marker>(MaxColliders);
        PlayerInteraction _interaction;
        float _nextScanTime;

        /// <summary>Interactables in range and in view, as of the last scan. Check <see cref="Marker.IsAlive"/> before use.</summary>
        public IReadOnlyList<Marker> Markers => _markers;

        void Awake()
        {
            _interaction = GetComponent<PlayerInteraction>();
        }

        void OnDisable()
        {
            _markers.Clear();
        }

        void Update()
        {
            Transform cam = _interaction.CameraTransform;
            if (!_interaction.isActiveAndEnabled || Cursor.lockState != CursorLockMode.Locked || cam == null)
            {
                _markers.Clear();
                _nextScanTime = 0f;
                return;
            }

            if (Time.unscaledTime < _nextScanTime) return;
            _nextScanTime = Time.unscaledTime + _scanInterval;
            Scan(cam);
        }

        void Scan(Transform cam)
        {
            _markers.Clear();
            _seen.Clear();

            Vector3 eye = cam.position;
            Vector3 forward = cam.forward;
            int count = Physics.OverlapSphereNonAlloc(eye, _markerRange, _colliders, _interaction.InteractLayers, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider col = _colliders[i];
                if (col.transform.IsChildOf(transform)) continue;

                IInteractable target = col.GetComponentInParent<IInteractable>();
                if (target == null || !_seen.Add(target)) continue;
                if (target.GetPrompt(_interaction).IsEmpty) continue;

                Component owner = target as Component;
                Transform anchor = InteractionMarkerAnchor.Find(owner);
                Vector3 point = InteractionMarkerAnchor.Position(anchor, col, owner);
                if (Vector3.Dot(point - eye, forward) <= 0f) continue;
                if (_requireLineOfSight && !HasLineOfSight(eye, point, owner)) continue;

                _markers.Add(new Marker(target, owner, col, anchor));
            }
        }

        /// <summary>True if nothing solid outside the target's own hierarchy stands between the eye and the point.
        /// A wall that is the target's parent still blocks, so doors and lamps inside a building stay hidden.</summary>
        bool HasLineOfSight(Vector3 eye, Vector3 point, Component owner)
        {
            if (!Physics.Linecast(eye, point, out RaycastHit hit, _interaction.InteractLayers, QueryTriggerInteraction.Ignore))
                return true;

            // The ray reached the target itself (its own collider or one of its children).
            return owner != null && hit.collider.transform.IsChildOf(owner.transform);
        }
    }
}
