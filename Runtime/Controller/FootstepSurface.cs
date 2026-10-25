using UnityEngine;

/// <summary>
/// Sound set for one ground type (grass, gravel, stone...). Tag colliders with <see cref="FootstepSurfaceTag"/> to use it.
/// </summary>
[CreateAssetMenu(menuName = "First Person/Footstep Surface", fileName = "SO_Footsteps_")]
public sealed class FootstepSurface : ScriptableObject
{
    [Tooltip("Picked at random, never the same one twice in a row.")]
    [SerializeField] AudioClip[] _stepClips = new AudioClip[0];
    [Tooltip("Optional. Falls back to the player's default landing clip.")]
    [SerializeField] AudioClip _landClip;
    [Tooltip("Multiplies the player's walk/sprint/crouch volume, so soft surfaces can be quieter.")]
    [SerializeField, Range(0f, 2f)] float _volume = 1f;

    public AudioClip[] StepClips => _stepClips;
    public AudioClip LandClip => _landClip;
    public float Volume => _volume;
}
