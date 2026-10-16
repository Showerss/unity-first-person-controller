using UnityEngine;

namespace FirstPerson.Interaction
{
    /// <summary>
    /// Lights or puts out a lamp, candle or brazier with Interact.
    /// Put it on the object with the collider (or a parent of it). Saving whether it's lit is up to the save system (see the pause menu package).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LightSwitch : MonoBehaviour, IInteractable
    {
        [Tooltip("Lights turned on and off together.")]
        [SerializeField] private Light[] _lights = new Light[0];

        [Tooltip("Objects shown only while lit, e.g. a flame mesh.")]
        [SerializeField] private GameObject[] _litVisuals = new GameObject[0];

        [Tooltip("Whether the lamp is lit when the scene starts.")]
        [SerializeField] private bool _startsLit = true;

        [Tooltip("Prompt while lit. The HUD adds the key.")]
        [SerializeField] private string _putOutText = "Put out the lamp";

        [Tooltip("Prompt while dark. The HUD adds the key.")]
        [SerializeField] private string _lightText = "Light the lamp";

        [Tooltip("Optional sound played on each toggle.")]
        [SerializeField] private AudioClip _toggleSound;

        [Tooltip("Source for the toggle sound. Leave empty to play it at the lamp's position.")]
        [SerializeField] private AudioSource _audioSource;

        private InteractionPrompt _putOutPrompt;
        private InteractionPrompt _lightPrompt;

        public bool IsLit { get; private set; }

        private void Awake()
        {
            _putOutPrompt = new InteractionPrompt(_putOutText);
            _lightPrompt = new InteractionPrompt(_lightText);
            SetLit(_startsLit);
        }

        /// <summary>Set the lamp's state directly, e.g. from a quest step or a time-of-day system.</summary>
        public void SetLit(bool lit)
        {
            IsLit = lit;
            foreach (Light l in _lights)
            {
                if (l != null) l.enabled = lit;
            }
            foreach (GameObject visual in _litVisuals)
            {
                if (visual != null) visual.SetActive(lit);
            }
        }

        public InteractionPrompt GetPrompt(PlayerInteraction interactor) => IsLit ? _putOutPrompt : _lightPrompt;

        public void Interact(PlayerInteraction interactor)
        {
            SetLit(!IsLit);

            if (_toggleSound == null) return;
            if (_audioSource != null) _audioSource.PlayOneShot(_toggleSound);
            else AudioSource.PlayClipAtPoint(_toggleSound, transform.position);
        }
    }
}
