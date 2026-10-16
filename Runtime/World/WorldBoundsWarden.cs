using System.Collections;
using FirstPerson.Interaction;
using FirstPerson.UI;
using UnityEngine;

namespace FirstPerson.World
{
    /// <summary>
    /// Keeps the player inside the world. The screen darkens as they walk toward the edge (and lightens if they turn back);
    /// once it is fully black they are put back on the last stretch of road they walked.
    /// An <see cref="IWorldBoundsWarning"/> on the same object (optional) is shown before the screen fades back in.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldBoundsWarden : MonoBehaviour
    {
        [Tooltip("The walkable area, darkness and return timings for this scene.")]
        [SerializeField] private WorldBoundsSettings _settings;

        [Tooltip("The player's controller; moved back onto the path.")]
        [SerializeField] private FirstPersonController _player;

        [Tooltip("Remembers where on the road the player last was.")]
        [SerializeField] private PathBreadcrumbs _breadcrumbs;

        [Tooltip("Full-screen black overlay (under --- UI ---).")]
        [SerializeField] private ScreenFader _fader;

        private IWorldBoundsWarning _warning;
        private bool _returning;

        private void Awake()
        {
            if (_settings == null || _player == null || _breadcrumbs == null || _fader == null)
            {
                Debug.LogError($"[WorldBoundsWarden] '{name}' is missing a reference.", this);
                enabled = false;
                return;
            }
            _warning = GetComponent<IWorldBoundsWarning>();
        }

        private void Update()
        {
            if (_returning) return;

            Vector3 position = _player.transform.position;
            _fader.Alpha = _settings.DarknessAt(position);

            if (_settings.DistanceInside(position) <= _settings.BlackoutDistance)
                StartCoroutine(ReturnToPath());
        }

        private IEnumerator ReturnToPath()
        {
            _returning = true;
            _fader.Alpha = 1f;
            PlayerLock.Set(_player, true);

            _breadcrumbs.GetRespawnPose(out Vector3 ground, out float yaw);
            _player.Teleport(ground + Vector3.up * PlayerLock.FeetToPivot(_player), yaw);

            yield return new WaitForSecondsRealtime(_settings.HoldSeconds);

            if (_warning != null) yield return _warning.Show();
            PlayerLock.Set(_player, false);

            // Fade to however dark it should be where they now stand, so the darkness doesn't jump afterwards.
            yield return _fader.FadeTo(_settings.DarknessAt(_player.transform.position), _settings.FadeInSeconds);
            _returning = false;
        }
    }
}
