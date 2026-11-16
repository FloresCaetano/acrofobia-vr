using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Acrofobia.Controls
{
    /// <summary>
    /// Mouse and keyboard implementation of <see cref="IPlayerInputSource"/> built on Input System actions.
    /// The actions are declared in code so the bindings are reviewable in one place.
    /// </summary>
    public sealed class KeyboardMouseInputSource : MonoBehaviour, IPlayerInputSource
    {
        [Tooltip("Degrees of rotation per pixel of mouse movement.")]
        [SerializeField, Range(0.01f, 0.5f)] private float mouseSensitivity = 0.08f;

        private InputActionMap map;
        private InputAction look;
        private InputAction move;

        public event Action CalmPressed;
        public event Action PausePressed;
        public event Action ConfirmPressed;
        public event Action<int> ValueStepped;

        public bool DrivesHeadPose => false;
        public Vector2 LookDelta => look.ReadValue<Vector2>() * mouseSensitivity;
        public float Move => move.ReadValue<float>();

        public float MouseSensitivity
        {
            get => mouseSensitivity;
            set => mouseSensitivity = Mathf.Clamp(value, 0.01f, 0.5f);
        }

        private void Awake()
        {
            map = new InputActionMap("Patient");

            look = map.AddAction("Look", InputActionType.Value, "<Mouse>/delta");

            move = map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/s")
                .With("Positive", "<Keyboard>/w");

            Bind("Calm", () => CalmPressed?.Invoke(), "<Keyboard>/c");
            Bind("Pause", () => PausePressed?.Invoke(), "<Keyboard>/escape");
            Bind("Confirm", () => ConfirmPressed?.Invoke(), "<Keyboard>/enter", "<Keyboard>/numpadEnter");
            Bind("StepUp", () => ValueStepped?.Invoke(1), "<Keyboard>/upArrow", "<Keyboard>/rightArrow");
            Bind("StepDown", () => ValueStepped?.Invoke(-1), "<Keyboard>/downArrow", "<Keyboard>/leftArrow");

            var scroll = map.AddAction("Scroll", InputActionType.PassThrough, "<Mouse>/scroll/y");
            scroll.performed += context =>
            {
                float value = context.ReadValue<float>();
                if (Mathf.Abs(value) > 0.01f) ValueStepped?.Invoke(value > 0f ? 1 : -1);
            };
        }

        private void OnEnable() => map.Enable();
        private void OnDisable() => map.Disable();
        private void OnDestroy() => map.Dispose();

        private void Bind(string name, Action onPressed, params string[] bindings)
        {
            var action = map.AddAction(name, InputActionType.Button);
            foreach (string binding in bindings) action.AddBinding(binding);
            action.performed += _ => onPressed();
        }
    }
}
