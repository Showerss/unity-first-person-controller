using UnityEngine;

public class HazardZone : MonoBehaviour
{
    [SerializeField] private Vector3 _spawnPosition = new Vector3(0f, 2f, 0f);

    private void OnTriggerEnter(Collider other)
    {
        CharacterController cc = other.GetComponent<CharacterController>();
        if (cc != null || other.CompareTag("Player"))
        {
            if (cc != null)
            {
                // Teleport CharacterController safely
                cc.enabled = false;
                other.transform.position = _spawnPosition;
                cc.enabled = true;
            }
            else
            {
                other.transform.position = _spawnPosition;
            }

            // Reset velocity of FirstPersonController if possible
            FirstPersonController fpc = other.GetComponent<FirstPersonController>();
            if (fpc != null)
            {
                // We can't access private _velocity directly, but character will fall from spawn normally
            }
        }
    }
}
