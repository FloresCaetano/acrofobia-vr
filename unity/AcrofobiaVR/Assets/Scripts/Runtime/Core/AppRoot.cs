using UnityEngine;

namespace Acrofobia.Core
{
    /// <summary>Root of the Boot scene. Keeps the managers, the player rig and the UI alive across scene loads.</summary>
    [DisallowMultipleComponent]
    public sealed class AppRoot : MonoBehaviour
    {
        private void Awake() => DontDestroyOnLoad(gameObject);
    }
}
