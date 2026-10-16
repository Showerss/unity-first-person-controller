using FirstPerson.Interaction;
using TMPro;
using UnityEngine;

namespace FirstPerson.UI
{
    /// <summary>
    /// Shows the player's current interaction prompt (e.g. "[E] Open door" or "The gate is barred.").
    /// Appears and disappears instantly, like the message line in classic DOS RPGs.
    /// Redraws only when the prompt changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionPromptHUD : MonoBehaviour
    {
        [Tooltip("The player's interaction component that supplies the prompt.")]
        [SerializeField] private PlayerInteraction _playerInteraction;

        [Tooltip("Text element that displays the prompt.")]
        [SerializeField] private TMP_Text _promptText;

        [Tooltip("Message box behind the text; hidden while there is no prompt.")]
        [SerializeField] private GameObject _panel;

        private void OnEnable()
        {
            if (_playerInteraction == null)
            {
                Debug.LogError($"[InteractionPromptHUD] No PlayerInteraction assigned on '{name}'.", this);
                Show(InteractionPrompt.None);
                return;
            }

            _playerInteraction.PromptChanged += Show;
            Show(_playerInteraction.CurrentPrompt);
        }

        private void OnDisable()
        {
            if (_playerInteraction != null) _playerInteraction.PromptChanged -= Show;
        }

        private void Show(InteractionPrompt prompt)
        {
            string key = _playerInteraction != null ? _playerInteraction.InteractKeyLabel : string.Empty;
            _promptText.text = prompt.Format(key);
            if (_panel != null) _panel.SetActive(!prompt.IsEmpty);
        }
    }
}
