using UnityEngine;

/// <summary>
/// Marks a collider (or any parent of colliders) as a footstep surface. Untagged ground uses the player's default clips.
/// </summary>
public sealed class FootstepSurfaceTag : MonoBehaviour
{
    [SerializeField] FootstepSurface _surface;

    public FootstepSurface Surface => _surface;
}
