using UnityEngine;

namespace FirstPerson.World
{
    /// <summary>
    /// Invisible walls along the edge of the walkable area: the hard stop behind the darkness.
    /// Built from the bounds settings on Awake, so they always match the edge.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldBoundsWalls : MonoBehaviour
    {
        [Tooltip("The walkable area whose edge gets a wall.")]
        [SerializeField] private WorldBoundsSettings _settings;

        [Tooltip("Height of each wall in metres; tall enough to cover the hills along the edge.")]
        [Min(1f)]
        [SerializeField] private float _height = 200f;

        [Tooltip("Thickness of each wall in metres, extending outward from the edge.")]
        [Min(0.1f)]
        [SerializeField] private float _thickness = 2f;

        private void Awake()
        {
            if (_settings == null)
            {
                Debug.LogError($"[WorldBoundsWalls] '{name}' needs Settings assigned.", this);
                return;
            }

            Vector2 min = _settings.Min;
            Vector2 max = _settings.Max;
            float width = max.x - min.x + _thickness * 2f;
            float depth = max.y - min.y + _thickness * 2f;
            float midX = (min.x + max.x) * 0.5f;
            float midZ = (min.y + max.y) * 0.5f;
            float half = _thickness * 0.5f;

            AddWall("Wall_South", new Vector3(midX, 0f, min.y - half), new Vector3(width, _height, _thickness));
            AddWall("Wall_North", new Vector3(midX, 0f, max.y + half), new Vector3(width, _height, _thickness));
            AddWall("Wall_West", new Vector3(min.x - half, 0f, midZ), new Vector3(_thickness, _height, depth));
            AddWall("Wall_East", new Vector3(max.x + half, 0f, midZ), new Vector3(_thickness, _height, depth));
        }

        private void AddWall(string wallName, Vector3 center, Vector3 size)
        {
            var wall = new GameObject(wallName);
            wall.transform.SetParent(transform, false);
            wall.transform.position = center;
            wall.AddComponent<BoxCollider>().size = size;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_settings == null) return;
            Vector2 min = _settings.Min;
            Vector2 max = _settings.Max;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3((min.x + max.x) * 0.5f, 0f, (min.y + max.y) * 0.5f), new Vector3(max.x - min.x, 60f, max.y - min.y));
        }
#endif
    }
}
