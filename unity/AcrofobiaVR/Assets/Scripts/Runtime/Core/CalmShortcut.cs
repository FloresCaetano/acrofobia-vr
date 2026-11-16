using Acrofobia.Controls;
using UnityEngine;

namespace Acrofobia.Core
{
    /// <summary>Routes the patient's calm button to the session. It works in any state, panels included.</summary>
    public sealed class CalmShortcut : MonoBehaviour
    {
        [SerializeField] private SessionManager session;
        [SerializeField] private MonoBehaviour inputSource;

        private IPlayerInputSource input;

        private void Awake() => input = (IPlayerInputSource)inputSource;
        private void OnEnable() => input.CalmPressed += OnCalm;
        private void OnDisable() => input.CalmPressed -= OnCalm;

        private void OnCalm() => session.TriggerCalm(CalmSource.Patient);
    }
}
