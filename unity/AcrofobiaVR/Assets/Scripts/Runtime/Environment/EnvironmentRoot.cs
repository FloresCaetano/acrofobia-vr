using Acrofobia.Data;
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

        /// <summary>Configures level-specific scenery. Called while the screen is faded out; null means "no level".</summary>
        public virtual void ApplyLevel(LevelDefinition level) { }

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
