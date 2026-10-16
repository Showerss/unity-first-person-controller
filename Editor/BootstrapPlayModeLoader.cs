using System.IO;
using FirstPerson.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstPerson.Editor
{
    /// <summary>
    /// Lets you press Play from any gameplay scene: if the Boot scene (build index 0, holding a <see cref="GameBootstrap"/>)
    /// isn't loaded, it is loaded additively so persistent systems exist exactly as in a build.
    /// Projects without a Boot scene are left alone.
    /// </summary>
    [InitializeOnLoad]
    internal static class BootstrapPlayModeLoader
    {
        static BootstrapPlayModeLoader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            if (Object.FindAnyObjectByType<GameBootstrap>() != null) return;

            string bootPath = BootScenePath();
            if (bootPath != null)
                EditorSceneManager.LoadSceneAsyncInPlayMode(bootPath, new LoadSceneParameters(LoadSceneMode.Additive));
        }

        // The first enabled scene in Build Settings, if it contains a GameBootstrap.
        private static string BootScenePath()
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                return SceneHasBootstrap(scene.path) ? scene.path : null;
            }
            return null;
        }

        private static bool SceneHasBootstrap(string scenePath)
        {
            string[] guids = AssetDatabase.FindAssets("t:MonoScript GameBootstrap");
            foreach (string guid in guids)
            {
                if (AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid))?.GetClass() != typeof(GameBootstrap)) continue;
                return File.Exists(scenePath) && File.ReadAllText(scenePath).Contains(guid);
            }
            return false;
        }
    }
}
