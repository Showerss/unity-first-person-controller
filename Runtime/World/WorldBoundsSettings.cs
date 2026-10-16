using UnityEngine;

namespace FirstPerson.World
{
    /// <summary>
    /// Where a scene's walkable area ends, how the darkness closes in as the player walks toward the edge,
    /// and how long the return takes. What they're told afterwards is an <see cref="IWorldBoundsWarning"/> next to the warden.
    /// </summary>
    [CreateAssetMenu(menuName = "First Person/World Bounds Settings", fileName = "SO_WorldBounds_")]
    public sealed class WorldBoundsSettings : ScriptableObject
    {
        [Header("Area (world XZ)")]
        [Tooltip("South-west corner of the walkable area (x, z). An invisible wall stands on the edge.")]
        [SerializeField] private Vector2 _min = new Vector2(-75f, -38f);

        [Tooltip("North-east corner of the walkable area (x, z). An invisible wall stands on the edge.")]
        [SerializeField] private Vector2 _max = new Vector2(75f, 165f);

        [Header("Darkness")]
        [Tooltip("Metres inside the edge where the screen starts to darken. The player keeps walking freely while it does.")]
        [Min(0f)]
        [SerializeField] private float _darkenStartDistance = 20f;

        [Tooltip("Metres inside the edge where the screen is fully black and the player is sent back. Keep it short of the wall so they never touch it.")]
        [Min(0f)]
        [SerializeField] private float _blackoutDistance = 5f;

        [Header("Return")]
        [Tooltip("Seconds of black after the player is moved, before any warning appears.")]
        [Min(0f)]
        [SerializeField] private float _holdSeconds = 0.5f;

        [Tooltip("Seconds to fade back in afterwards.")]
        [Min(0f)]
        [SerializeField] private float _fadeInSeconds = 1.5f;

        public Vector2 Min => _min;
        public Vector2 Max => _max;
        public float BlackoutDistance => _blackoutDistance;
        public float HoldSeconds => _holdSeconds;
        public float FadeInSeconds => _fadeInSeconds;

        /// <summary>Metres from <paramref name="position"/> to the nearest edge; negative once outside.</summary>
        public float DistanceInside(Vector3 position)
        {
            float x = Mathf.Min(position.x - _min.x, _max.x - position.x);
            float z = Mathf.Min(position.z - _min.y, _max.y - position.z);
            return Mathf.Min(x, z);
        }

        /// <summary>How dark the screen should be at <paramref name="position"/>: 0 well inside, 1 at the blackout line.</summary>
        public float DarknessAt(Vector3 position)
        {
            float t = Mathf.InverseLerp(_darkenStartDistance, _blackoutDistance, DistanceInside(position));
            return Mathf.SmoothStep(0f, 1f, t);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _darkenStartDistance = Mathf.Max(_darkenStartDistance, _blackoutDistance + 0.1f);
        }
#endif
    }
}
