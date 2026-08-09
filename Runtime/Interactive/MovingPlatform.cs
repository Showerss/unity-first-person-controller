using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Vector3[] _localWaypoints;
    [SerializeField] private float _speed = 3f;
    [SerializeField] private float _waitTime = 1f;

    private Vector3[] _globalWaypoints;
    private int _currentWaypointIndex = 0;
    private float _nextMoveTime = 0f;
    private Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }

        // Initialize waypoints relative to starting position
        if (_localWaypoints == null || _localWaypoints.Length == 0)
        {
            _localWaypoints = new Vector3[] { Vector3.zero, new Vector3(0f, 0f, 5f) };
        }

        _globalWaypoints = new Vector3[_localWaypoints.Length];
        for (int i = 0; i < _localWaypoints.Length; i++)
        {
            _globalWaypoints[i] = transform.TransformPoint(_localWaypoints[i]);
        }
    }

    void FixedUpdate()
    {
        if (Time.time < _nextMoveTime) return;

        Vector3 targetPosition = _globalWaypoints[_currentWaypointIndex];
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, _speed * Time.fixedDeltaTime);

        if (_rb != null)
        {
            _rb.MovePosition(newPosition);
        }
        else
        {
            transform.position = newPosition;
        }

        // Check if waypoint reached
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            _currentWaypointIndex = (_currentWaypointIndex + 1) % _globalWaypoints.Length;
            _nextMoveTime = Time.time + _waitTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_localWaypoints == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < _localWaypoints.Length; i++)
        {
            Vector3 globalPos = Application.isPlaying ? _globalWaypoints[i] : transform.TransformPoint(_localWaypoints[i]);
            Gizmos.DrawSphere(globalPos, 0.2f);

            if (i < _localWaypoints.Length - 1)
            {
                Vector3 nextGlobalPos = Application.isPlaying ? _globalWaypoints[i+1] : transform.TransformPoint(_localWaypoints[i+1]);
                Gizmos.DrawLine(globalPos, nextGlobalPos);
            }
        }
    }
}
