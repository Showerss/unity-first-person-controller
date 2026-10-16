using System.Collections.Generic;
using FirstPerson.Interaction;
using TMPro;
using UnityEngine;

namespace FirstPerson.UI
{
    /// <summary>
    /// Draws a tiny dot on every usable interactable near the player, and the focused one's prompt
    /// (e.g. "[E] Open door") on the object itself. Show and hide are instant, with no bobbing or pulsing.
    /// Lives on (or under) a screen-space canvas; dots and the label are positioned inside <see cref="_markerRoot"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionMarkerHUD : MonoBehaviour
    {
        [Tooltip("The player's interaction component; supplies the focused target and its prompt.")]
        [SerializeField] private PlayerInteraction _playerInteraction;

        [Tooltip("The player's scanner; supplies the nearby interactables that get a dot.")]
        [SerializeField] private InteractableScanner _scanner;

        [Tooltip("Camera the world is drawn with. Defaults to a camera under the PlayerInteraction.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Full-screen rect the dots and label are placed in. Defaults to this object's RectTransform.")]
        [SerializeField] private RectTransform _markerRoot;

        [Tooltip("Dot to copy for each marker. Keep it disabled; it is cloned as needed.")]
        [SerializeField] private RectTransform _dotTemplate;

        [Tooltip("Box around the focused prompt; hidden while there is no prompt. Its pivot sits on the object.")]
        [SerializeField] private RectTransform _promptRoot;

        [Tooltip("Text element that displays the focused prompt.")]
        [SerializeField] private TMP_Text _promptText;

        [Tooltip("Offset of the prompt from the object's anchor point, in canvas units.")]
        [SerializeField] private Vector2 _promptOffset = new Vector2(0f, 12f);

        [Tooltip("Gap kept between the prompt and the screen edge, in canvas units.")]
        [SerializeField] private float _screenMargin = 8f;

        readonly List<RectTransform> _dots = new List<RectTransform>();
        Canvas _canvas;
        bool _hasPrompt;

        void Awake()
        {
            if (_markerRoot == null) _markerRoot = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            if (_dotTemplate != null) _dotTemplate.gameObject.SetActive(false);

            if (_camera == null && _playerInteraction != null)
                _camera = _playerInteraction.GetComponentInChildren<Camera>();
        }

        void OnEnable()
        {
            if (_playerInteraction == null || _scanner == null || _camera == null)
            {
                Debug.LogError($"[InteractionMarkerHUD] Missing PlayerInteraction, InteractableScanner or Camera on '{name}'.", this);
                enabled = false;
                return;
            }

            _playerInteraction.PromptChanged += ShowPrompt;
            ShowPrompt(_playerInteraction.CurrentPrompt);
        }

        void OnDisable()
        {
            if (_playerInteraction != null) _playerInteraction.PromptChanged -= ShowPrompt;
            HideDotsFrom(0);
            if (_promptRoot != null) _promptRoot.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            IInteractable focused = _playerInteraction.Current;
            IReadOnlyList<InteractableScanner.Marker> markers = _scanner.Markers;
            Rect screen = _markerRoot.rect;

            int used = 0;
            for (int i = 0; i < markers.Count; i++)
            {
                InteractableScanner.Marker marker = markers[i];
                if (!marker.IsAlive || ReferenceEquals(marker.Target, focused)) continue;
                if (!TryProject(marker.Position, out Vector2 local) || !screen.Contains(local)) continue;

                RectTransform dot = GetDot(used++);
                dot.localPosition = local;
            }
            HideDotsFrom(used);

            if (_hasPrompt) PlacePrompt(focused);
        }

        void ShowPrompt(InteractionPrompt prompt)
        {
            _hasPrompt = !prompt.IsEmpty;
            if (_promptText != null) _promptText.text = prompt.Format(_playerInteraction.InteractKeyLabel);
            if (_promptRoot == null) return;

            _promptRoot.gameObject.SetActive(_hasPrompt);
            if (_hasPrompt) PlacePrompt(_playerInteraction.Current);
        }

        void PlacePrompt(IInteractable focused)
        {
            if (_promptRoot == null || focused == null) return;
            if (!TryProject(_playerInteraction.CurrentAnchor, out Vector2 local)) return;

            local += _promptOffset;

            // Keep the whole box on screen. Rect sizes are in the root's local units, like 'local'.
            Rect bounds = _markerRoot.rect;
            Rect box = _promptRoot.rect;
            Vector2 pivot = _promptRoot.pivot;
            float minX = bounds.xMin + _screenMargin + box.width * pivot.x;
            float maxX = bounds.xMax - _screenMargin - box.width * (1f - pivot.x);
            float minY = bounds.yMin + _screenMargin + box.height * pivot.y;
            float maxY = bounds.yMax - _screenMargin - box.height * (1f - pivot.y);
            local.x = minX < maxX ? Mathf.Clamp(local.x, minX, maxX) : bounds.center.x;
            local.y = minY < maxY ? Mathf.Clamp(local.y, minY, maxY) : bounds.center.y;

            _promptRoot.localPosition = new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));
        }

        /// <summary>World point → position inside the marker root, snapped to whole pixels. False if behind the camera.</summary>
        bool TryProject(Vector3 world, out Vector2 local)
        {
            Vector3 screen = _camera.WorldToScreenPoint(world);
            if (screen.z <= 0f)
            {
                local = default;
                return false;
            }

            screen.x = Mathf.Round(screen.x);
            screen.y = Mathf.Round(screen.y);
            Camera uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_markerRoot, screen, uiCamera, out local);
        }

        RectTransform GetDot(int index)
        {
            if (index == _dots.Count)
            {
                RectTransform dot = Instantiate(_dotTemplate, _markerRoot);
                dot.name = $"Dot {index}";
                _dots.Add(dot);
            }

            RectTransform result = _dots[index];
            if (!result.gameObject.activeSelf) result.gameObject.SetActive(true);
            return result;
        }

        void HideDotsFrom(int index)
        {
            for (int i = index; i < _dots.Count; i++)
                if (_dots[i].gameObject.activeSelf) _dots[i].gameObject.SetActive(false);
        }
    }
}
