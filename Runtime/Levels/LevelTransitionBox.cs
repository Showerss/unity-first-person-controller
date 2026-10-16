using System.Collections;
using FirstPerson.Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FirstPerson.Levels
{
    /// <summary>
    /// Reusable transparent interactable box for doorways and level transitions.
    /// Place and stretch over any door or passage.
    /// Plays an assigned sound and transitions to the next level when interacted with or walked into.
    /// The trigger box itself is what the player looks at; it never blocks movement.
    /// </summary>
    [SelectionBase]
    [RequireComponent(typeof(BoxCollider))]
    public class LevelTransitionBox : MonoBehaviour, IInteractable
    {
        public enum TransitionType
        {
            LoadSceneByName,
            LoadNextSceneInBuild,
            TeleportWithinScene
        }

        [Header("--- Level to Load ---")]
        [Tooltip("How this transition triggers the next level.")]
        [SerializeField] private TransitionType _transitionType = TransitionType.LoadSceneByName;

    #if UNITY_EDITOR
        [Tooltip("Drag and drop any Scene asset here to auto-populate the scene name.")]
        [SerializeField] private SceneAsset _sceneAsset;
    #endif

        [Tooltip("Name of the scene/level to load.")]
        [SerializeField] private string _nextLevelName = string.Empty;

        [Tooltip("If using TeleportWithinScene, the target transform.")]
        [SerializeField] private Transform _teleportDestination;

        [Tooltip("If using TeleportWithinScene and no target transform is assigned, world XYZ position.")]
        [SerializeField] private Vector3 _teleportCoordinates = Vector3.zero;

        [Header("--- Sound FX ---")]
        [Tooltip("Sound clip to play when interacted with (e.g. metal door opening sound).")]
        [SerializeField] private AudioClip _interactSound;

        [Range(0f, 1f)]
        [Tooltip("Playback volume of the interaction sound.")]
        [SerializeField] private float _soundVolume = 1.0f;

        [Tooltip("Delay in seconds before the level loads, letting the door sound play.")]
        [SerializeField] private float _transitionDelay = 0.85f;

        [Header("--- Interaction Triggers ---")]
        [Tooltip("Action shown when the player looks at this, e.g. 'Open door'. The HUD adds the key.")]
        [SerializeField] private string _promptText = "Open door";

        [Tooltip("Allow pressing Interact when aiming at this doorway.")]
        [SerializeField] private bool _activateOnInteractButton = true;

        [Tooltip("Allow walking into this doorway volume to trigger the transition.")]
        [SerializeField] private bool _activateOnWalkInto = true;

        [Header("--- Lock ---")]
        [Tooltip("While locked, interacting only plays the locked sound and shows the locked prompt. Walking in does nothing.")]
        [SerializeField] private bool _isLocked = false;

        [Tooltip("Message displayed on HUD while locked. Shown without a key, since pressing it won't open anything.")]
        [SerializeField] private string _lockedPromptText = "The gate is barred.";

        [Tooltip("Sound played when the player tries a locked transition (e.g. a rattling gate).")]
        [SerializeField] private AudioClip _lockedSound;

        [Range(0.3f, 1.5f)]
        [Tooltip("Playback pitch of the locked sound. Below 1 sounds heavier and older.")]
        [SerializeField] private float _lockedSoundPitch = 0.7f;

        [Header("--- Scene View Helper ---")]
        [Tooltip("Color of the transparent box visualizer in the Scene View (invisible during gameplay).")]
        [SerializeField] private Color _gizmoColor = new Color(0.1f, 0.85f, 0.4f, 0.3f);

        private static readonly InteractionPrompt OpeningPrompt = new InteractionPrompt("Opening...", false);

        private AudioSource _audioSource;
        private InteractionPrompt _openPrompt;
        private InteractionPrompt _lockedPrompt;
        private bool _isTransitioning = false;

        private void Awake()
        {
            // 1. Ensure AudioSource
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1.0f; // 3D spatial sound
            _audioSource.minDistance = 2.0f;
            _audioSource.maxDistance = 30.0f;

            // 2. Hide any MeshRenderer so this box is completely transparent in gameplay
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = false;
            }

            // 3. The box is a trigger: walk-in fires OnTriggerEnter, and PlayerInteraction sees triggers.
            GetComponent<BoxCollider>().isTrigger = true;

            _openPrompt = new InteractionPrompt(_promptText);
            _lockedPrompt = new InteractionPrompt(_lockedPromptText, false);
        }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (_sceneAsset != null)
            {
                _nextLevelName = _sceneAsset.name;
            }
        }

        private void OnDrawGizmos()
        {
            DrawGizmo(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmo(true);
        }

        private void DrawGizmo(bool selected)
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null) return;

            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Color fill = _gizmoColor;
            Color wire = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, selected ? 1f : 0.7f);

            if (!selected)
            {
                fill.a *= 0.5f;
            }

            Gizmos.color = fill;
            Gizmos.DrawCube(col.center, col.size);

            Gizmos.color = wire;
            Gizmos.DrawWireCube(col.center, col.size);

            Gizmos.matrix = old;
        }
    #endif

        public bool IsLocked => _isLocked;

        /// <summary>Opens a locked transition, e.g. from a key, lever or quest step.</summary>
        public void Unlock()
        {
            _isLocked = false;
        }

        // --- IInteractable Implementation (Interact button press) ---
        public InteractionPrompt GetPrompt(PlayerInteraction interactor)
        {
            if (!_activateOnInteractButton) return InteractionPrompt.None;
            if (_isLocked) return _lockedPrompt;
            return _isTransitioning ? OpeningPrompt : _openPrompt;
        }

        public void Interact(PlayerInteraction interactor)
        {
            if (!_activateOnInteractButton || _isTransitioning) return;

            if (_isLocked)
            {
                PlayLockedSound();
                return;
            }

            StartCoroutine(DoTransition(interactor.gameObject));
        }

        private void PlayLockedSound()
        {
            // Don't stack rattles if the player keeps trying the gate.
            if (_lockedSound == null || _audioSource == null || _audioSource.isPlaying) return;

            _audioSource.pitch = _lockedSoundPitch;
            _audioSource.PlayOneShot(_lockedSound, _soundVolume);
        }

        // --- Walk into doorway trigger ---
        private void OnTriggerEnter(Collider other)
        {
            if (!_activateOnWalkInto || _isTransitioning || _isLocked) return;

            if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
            {
                StartCoroutine(DoTransition(other.gameObject));
            }
        }

        private IEnumerator DoTransition(GameObject player)
        {
            _isTransitioning = true;

            // Play metal door sound effect
            if (_audioSource != null && _interactSound != null)
            {
                _audioSource.pitch = Random.Range(0.96f, 1.04f);
                _audioSource.PlayOneShot(_interactSound, _soundVolume);
            }

            // Wait for sound to play
            if (_transitionDelay > 0f)
            {
                yield return new WaitForSeconds(_transitionDelay);
            }

            // Execute level transition
            switch (_transitionType)
            {
                case TransitionType.TeleportWithinScene:
                    TeleportPlayer(player);
                    _isTransitioning = false;
                    break;

                case TransitionType.LoadNextSceneInBuild:
                    // Index 0 is Boot, which stays loaded; wrap round to the first gameplay scene.
                    int nextIdx = gameObject.scene.buildIndex + 1;
                    if (nextIdx >= SceneManager.sceneCountInBuildSettings) nextIdx = 1;
                    yield return SwapScene(SceneManager.LoadSceneAsync(nextIdx, LoadSceneMode.Additive));
                    break;

                case TransitionType.LoadSceneByName:
                default:
                    if (string.IsNullOrEmpty(_nextLevelName))
                    {
                        Debug.LogWarning($"[LevelTransitionBox] No scene specified to load on '{gameObject.name}'!", this);
                        _isTransitioning = false;
                        break;
                    }
                    yield return SwapScene(SceneManager.LoadSceneAsync(_nextLevelName, LoadSceneMode.Additive));
                    break;
            }
        }

        /// <summary>Boot stays loaded (see CONVENTIONS §4): add the next scene, make it active, then drop this one.</summary>
        private IEnumerator SwapScene(AsyncOperation load)
        {
            if (load == null)
            {
                Debug.LogWarning($"[LevelTransitionBox] Scene load failed on '{gameObject.name}'. Is the scene in Build Settings?", this);
                _isTransitioning = false;
                yield break;
            }

            Scene from = gameObject.scene;
            yield return load;

            Scene to = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            SceneManager.SetActiveScene(to);
            SceneManager.UnloadSceneAsync(from);
        }

        private void TeleportPlayer(GameObject player)
        {
            Vector3 targetPos = _teleportDestination != null ? _teleportDestination.position : _teleportCoordinates;
            Quaternion targetRot = _teleportDestination != null ? _teleportDestination.rotation : player.transform.rotation;

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = targetPos;
            player.transform.rotation = targetRot;
            Physics.SyncTransforms();

            if (cc != null) cc.enabled = true;
        }
    }
}
