using Acrofobia.Controls;
using Acrofobia.Player;
using TMPro;
using UnityEngine;

namespace Acrofobia.UI
{
    /// <summary>Esc menu: frees the cursor and offers comfort options. The world keeps running (as it would in VR).</summary>
    public sealed class PauseMenu : UiView
    {
        [SerializeField] private ModalFocus focus;
        [SerializeField] private SudsPanel sudsPanel;
        [SerializeField] private PlayerRig playerRig;
        [SerializeField] private KeyboardMouseInputSource mouse;

        private TextMeshProUGUI swayLabel;
        private TextMeshProUGUI sensitivityLabel;

        public bool IsOpen => focus.IsOpen(this);

        protected override void Build(RectTransform root)
        {
            var dim = Factory.Panel("Dim", root, new Color(0f, 0f, 0f, 0.45f), rounded: false);
            UiFactory.Stretch(dim.rectTransform);

            var card = Factory.Panel("Card", root, Theme.background);
            UiFactory.Anchor(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(640f, 600f), Vector2.zero);
            UiFactory.Vertical(card.gameObject, 40, 16f, TextAnchor.UpperCenter);

            Factory.Label(card.transform, "Pausa", Theme.titleSize, Theme.textPrimary, true, TextAlignmentOptions.Center);
            Factory.Button(card.transform, "Continuar", Close, ButtonStyle.Primary, 72f);
            swayLabel = Factory.Button(card.transform, "", ToggleSway).GetComponentInChildren<TextMeshProUGUI>();

            var sensitivity = UiFactory.Rect("Sensitivity", card.transform);
            UiFactory.Horizontal(sensitivity.gameObject, 12f, TextAnchor.MiddleCenter);
            UiFactory.Size(sensitivity, preferredHeight: 64f);
            UiFactory.Size(Factory.Button(sensitivity, "–", () => ChangeSensitivity(-0.01f)), preferredWidth: 90f, preferredHeight: 64f);
            sensitivityLabel = Factory.Label(sensitivity, "", Theme.smallSize, Theme.textPrimary, alignment: TextAlignmentOptions.Center);
            UiFactory.Size(sensitivityLabel, preferredWidth: 300f);
            UiFactory.Size(Factory.Button(sensitivity, "+", () => ChangeSensitivity(0.01f)), preferredWidth: 90f, preferredHeight: 64f);

            Factory.Button(card.transform, "Salir de la aplicación", Quit);
            Factory.Label(card.transform, "Esc vuelve a la exposición", Theme.smallSize, Theme.textSecondary, alignment: TextAlignmentOptions.Center);
            SetVisible(false);
        }

        private void OnEnable() => ((IPlayerInputSource)mouse).PausePressed += Toggle;
        private void OnDisable() => ((IPlayerInputSource)mouse).PausePressed -= Toggle;

        private void Toggle()
        {
            if (IsOpen) Close();
            else if (!sudsPanel.IsOpen) Open();
        }

        private void Open()
        {
            Refresh();
            SetVisible(true);
            focus.Open(this);
        }

        private void Close()
        {
            SetVisible(false);
            focus.Close(this);
        }

        private void ToggleSway()
        {
            playerRig.CameraSwayEnabled = !playerRig.CameraSwayEnabled;
            Refresh();
        }

        private void ChangeSensitivity(float delta)
        {
            mouse.MouseSensitivity += delta;
            Refresh();
        }

        private void Refresh()
        {
            swayLabel.text = $"Balanceo de cámara: {(playerRig.CameraSwayEnabled ? "activado" : "desactivado")}";
            sensitivityLabel.text = $"Sensibilidad: {mouse.MouseSensitivity * 100f:0}";
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
