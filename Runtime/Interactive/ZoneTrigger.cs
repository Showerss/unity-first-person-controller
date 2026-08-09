using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    [SerializeField] private string _zoneName;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CharacterController>() != null || other.CompareTag("Player"))
        {
            SandboxHUD hud = Object.FindAnyObjectByType<SandboxHUD>();
            if (hud != null)
            {
                hud.SetActiveZone(_zoneName);
            }
        }
    }
}
