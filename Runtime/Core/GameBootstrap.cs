using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstPerson.Core
{
    /// <summary>
    /// Persistent root for cross-scene systems (EventSystem, save, quests, pause menu...).
    /// Lives in a Boot scene at build index 0 and additively loads the first gameplay scene.
    /// Add new persistent systems as children of this object rather than as singletons.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Gameplay scene loaded after boot. Must be in Build Settings.")]
        [SerializeField] private string _firstSceneName = string.Empty;

        private static GameBootstrap _instance;

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (string.IsNullOrEmpty(_firstSceneName))
            {
                Debug.LogError($"[GameBootstrap] '{name}' has no First Scene Name.", this);
                return;
            }

            // Already loaded when entering play mode from a gameplay scene in the editor.
            if (SceneManager.GetSceneByName(_firstSceneName).isLoaded)
            {
                return;
            }

            AsyncOperation load = SceneManager.LoadSceneAsync(_firstSceneName, LoadSceneMode.Additive);
            load.completed += _ => SceneManager.SetActiveScene(SceneManager.GetSceneByName(_firstSceneName));
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
