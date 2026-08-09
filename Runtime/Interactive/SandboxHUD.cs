using UnityEngine;
using TMPro;

public class SandboxHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private TextMeshProUGUI _promptText;
    [SerializeField] private TextMeshProUGUI _zoneText;

    [Header("Stamina Bar")]
    [SerializeField] private UnityEngine.UI.Image _staminaFillImage;
    [SerializeField] private GameObject _staminaBarContainer;

    private FirstPersonController _playerController;
    private PlayerInteraction _playerInteraction;
    private string _currentZoneName = "Spawn / Hub";

    void Start()
    {
        // Find player components in the scene
        _playerController = Object.FindAnyObjectByType<FirstPersonController>();
        _playerInteraction = Object.FindAnyObjectByType<PlayerInteraction>();

        if (_zoneText != null)
        {
            _zoneText.text = "Zone: " + _currentZoneName;
        }
    }

    void Update()
    {
        UpdateStateDisplay();
        UpdatePromptDisplay();
        UpdateStaminaBar();
    }

    private void UpdateStaminaBar()
    {
        if (_playerController == null) return;

        float percent = _playerController.StaminaPercent;

        if (_staminaFillImage != null)
        {
            _staminaFillImage.fillAmount = percent;
        }

        if (_staminaBarContainer != null)
        {
            // If stamina is full, hide the bar. If not, show it.
            bool shouldBeVisible = percent < 0.999f;
            if (_staminaBarContainer.activeSelf != shouldBeVisible)
            {
                _staminaBarContainer.SetActive(shouldBeVisible);
            }
        }
    }

    private void UpdateStateDisplay()
    {
        if (_stateText == null || _playerController == null) return;

        string stateStr = "State: ";
        if (_playerController.IsClimbing)
        {
            stateStr += "<color=#FFA500>Climbing</color>";
        }
        else if (_playerController.IsSliding)
        {
            stateStr += "<color=#FFFF00>Sliding</color>";
        }
        else if (_playerController.IsCrouching)
        {
            stateStr += "<color=#00FFFF>Crouching</color>";
        }
        else if (_playerController.IsSprinting)
        {
            stateStr += "<color=#FF00FF>Sprinting</color>";
        }
        else if (_playerController.IsGrounded)
        {
            stateStr += "<color=#00FF00>Grounded</color>";
        }
        else
        {
            stateStr += "<color=#FF0000>Airborne</color>";
        }

        _stateText.text = stateStr;
    }

    private void UpdatePromptDisplay()
    {
        if (_promptText == null) return;

        if (_playerInteraction != null && !string.IsNullOrEmpty(_playerInteraction.CurrentPrompt))
        {
            _promptText.gameObject.SetActive(true);
            _promptText.text = _playerInteraction.CurrentPrompt;
        }
        else
        {
            _promptText.gameObject.SetActive(false);
        }
    }

    public void SetActiveZone(string zoneName)
    {
        _currentZoneName = zoneName;
        if (_zoneText != null)
        {
            _zoneText.text = "Zone: " + zoneName;
        }
    }

    public void SetStaminaBarFields(UnityEngine.UI.Image fillImage, GameObject container)
    {
        _staminaFillImage = fillImage;
        _staminaBarContainer = container;
    }
}
