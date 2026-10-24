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
            Scene bootScene = gameObject.scene;
            DontDestroyOnLoad(gameObject);

            if (string.IsNullOrEmpty(_firstSceneName))
            {
                Debug.LogError($"[GameBootstrap] '{name}' has no First Scene Name.", this);
                return;
            }

            // Entering play mode from a gameplay scene in the editor (the first scene or any other, e.g. a sandbox):
            // that scene is already loaded, so don't pull the first scene in on top of it.
            if (IsOtherSceneLoaded(bootScene))
            {
                return;
            }

            AsyncOperation load = SceneManager.LoadSceneAsync(_firstSceneName, LoadSceneMode.Additive);
            load.completed += _ => SceneManager.SetActiveScene(SceneManager.GetSceneByName(_firstSceneName));
        }

        private static bool IsOtherSceneLoaded(Scene bootScene)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene != bootScene) return true;
            }

            return false;
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
