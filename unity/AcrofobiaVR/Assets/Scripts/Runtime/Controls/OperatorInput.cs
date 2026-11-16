using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Acrofobia.Controls
{
    /// <summary>
    /// Keyboard shortcuts for whoever runs the demo (not the patient): the F1 panel and level keys.
    /// Kept apart from <see cref="IPlayerInputSource"/> because in VR the operator stays on the keyboard.
    /// </summary>
    public sealed class OperatorInput : MonoBehaviour
    {
        private InputActionMap map;

        public event Action PanelToggled;
        public event Action<int> LevelKeyPressed;

        private void Awake()
        {
            map = new InputActionMap("Operator");
            var toggle = map.AddAction("TogglePanel", InputActionType.Button, "<Keyboard>/f1");
            toggle.performed += _ => PanelToggled?.Invoke();

            for (int level = 1; level <= 4; level++)
            {
                int captured = level;
                var action = map.AddAction($"Level{level}", InputActionType.Button, $"<Keyboard>/{level}");
                action.AddBinding($"<Keyboard>/numpad{level}");
                action.performed += _ => LevelKeyPressed?.Invoke(captured);
            }
        }

        private void OnEnable() => map.Enable();
        private void OnDisable() => map.Disable();
        private void OnDestroy() => map.Dispose();
    }
}
