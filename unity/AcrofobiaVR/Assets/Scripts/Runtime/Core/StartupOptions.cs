using UnityEngine;
using UnityEngine.SceneManagement;

namespace Acrofobia.Core
{
    /// <summary>
    /// Lets the Boot flow start in the environment the developer pressed Play on. In the editor, pressing
    /// Play in Acrophobia or Neutral reloads Boot first, then enters that environment.
    /// </summary>
    public static class StartupOptions
    {
        private static string requestedScene;

        public static string ConsumeRequestedScene()
        {
            string scene = requestedScene;
            requestedScene = null;
            return scene;
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RedirectToBoot()
        {
            requestedScene = null;
            string active = SceneManager.GetActiveScene().name;
            if (active == SceneNames.Boot || System.Array.IndexOf(SceneNames.All, active) < 0) return;

            requestedScene = active;
            SceneManager.LoadScene(SceneNames.Boot, LoadSceneMode.Single);
        }
#endif
    }
}
