using Acrofobia.Player;
using UnityEngine;

namespace Acrofobia.Environment
{
    /// <summary>
    /// Root component of an environment scene (Neutral, Acrophobia). The session flow finds it among
    /// the root objects of a freshly loaded scene, so scenes never need references to the Boot managers.
    /// </summary>
    public abstract class EnvironmentRoot : MonoBehaviour
    {
        public abstract IWalkPath WalkPath { get; }

        /// <summary>Default place for the player when the environment is entered without a specific level.</summary>
        public abstract float DefaultSpawnDistance { get; }

        public static EnvironmentRoot FindIn(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out EnvironmentRoot environment)) return environment;
            }
            return null;
        }
    }
}
