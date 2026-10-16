using UnityEngine;

namespace FirstPerson.World
{
    /// <summary>
    /// Remembers the last point on the road the player walked past, so they can be put back there.
    /// Points are ordered along the road; each sits on the ground at the road's centre.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PathBreadcrumbs : MonoBehaviour
    {
        [Tooltip("The player whose footsteps are tracked.")]
        [SerializeField] private Transform _player;

        [Tooltip("Points on the road, in order from the start of the road to its end.")]
        [SerializeField] private Transform[] _points = new Transform[0];

        [Tooltip("The player counts as on the path within this many metres (horizontally) of a point.")]
        [Min(0.5f)]
        [SerializeField] private float _captureRadius = 5f;

        [Tooltip("Seconds between checks.")]
        [Min(0.02f)]
        [SerializeField] private float _sampleInterval = 0.25f;

        private int _lastVisited;
        private float _nextSampleTime;

        /// <summary>Index into the points of the last one the player walked past (the first point until then).</summary>
        public int LastVisitedIndex => _lastVisited;

        private void Awake()
        {
            if (_player == null || _points == null || _points.Length == 0)
            {
                Debug.LogError($"[PathBreadcrumbs] '{name}' needs a Player and at least one path point.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (Time.time < _nextSampleTime) return;
            _nextSampleTime = Time.time + _sampleInterval;

            int nearest = NearestPoint(_player.position, out float sqrDistance);
            if (sqrDistance <= _captureRadius * _captureRadius) _lastVisited = nearest;
        }

        /// <summary>Ground position of the last visited point, and the yaw that faces on along the road.</summary>
        public void GetRespawnPose(out Vector3 position, out float yaw)
        {
            position = _points[_lastVisited].position;

            Vector3 along = _lastVisited + 1 < _points.Length
                ? _points[_lastVisited + 1].position - position
                : _lastVisited > 0 ? position - _points[_lastVisited - 1].position : Vector3.forward;
            along.y = 0f;
            yaw = along.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(along).eulerAngles.y : 0f;
        }

        private int NearestPoint(Vector3 position, out float bestSqr)
        {
            int best = 0;
            bestSqr = float.MaxValue;
            for (int i = 0; i < _points.Length; i++)
            {
                Vector3 d = _points[i].position - position;
                float sqr = d.x * d.x + d.z * d.z;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = i;
                }
            }
            return best;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_points == null) return;
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _points.Length; i++)
            {
                if (_points[i] == null) continue;
                Gizmos.DrawWireSphere(_points[i].position, 0.5f);
                if (i > 0 && _points[i - 1] != null) Gizmos.DrawLine(_points[i - 1].position, _points[i].position);
            }
        }
#endif
    }
}
