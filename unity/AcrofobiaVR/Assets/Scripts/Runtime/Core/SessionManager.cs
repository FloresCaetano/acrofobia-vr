using System.Collections;
using Acrofobia.Environment;
using Acrofobia.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Acrofobia.Core
{
    /// <summary>Loads the exposure scene additively and places the persistent player rig on its walk path.</summary>
    public sealed class SessionManager : MonoBehaviour
    {
        [SerializeField] private PlayerRig playerRig;

        private IEnumerator Start()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Acrophobia, LoadSceneMode.Additive);

            Scene scene = SceneManager.GetSceneByName(SceneNames.Acrophobia);
            SceneManager.SetActiveScene(scene);
            EnvironmentRoot environment = EnvironmentRoot.FindIn(scene);
            IWalkPath path = environment.WalkPath;
            playerRig.Bind(path, environment.DefaultSpawnDistance, path.MinDistance, path.MaxDistance);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
