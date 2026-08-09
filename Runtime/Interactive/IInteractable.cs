using UnityEngine;

public interface IInteractable
{
    void Interact(GameObject player);
    string GetPromptText();
}
