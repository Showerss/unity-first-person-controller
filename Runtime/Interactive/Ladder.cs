using UnityEngine;

public class Ladder : MonoBehaviour
{
    [SerializeField] private float _climbSpeedOverride = -1f;

    public float ClimbSpeedOverride => _climbSpeedOverride;
}
