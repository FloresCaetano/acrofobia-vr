using System.Collections.Generic;
using Acrofobia.Controls;
using Acrofobia.Core;
using Acrofobia.Network;
using Acrofobia.Player;
using Acrofobia.Stimuli;
using Acrofobia.Suds;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Acrofobia.UI
{
    /// <summary>
    /// Local operator panel (F1) to run the demo without the web console: change level, move the five
    /// stimuli, ask for a SUDS rating and see the connection state.
    /// </summary>
    public sealed class OperatorPanel : UiView
    {
        [SerializeField] private SessionManager session;
        [SerializeField] private StimulusController stimuli;
        [SerializeField] private OperatorInput operatorInput;
        [SerializeField] private ModalFocus focus;
        [SerializeField] private PlayerRig playerRig;
        [SerializeField] private SudsManager suds;
        [SerializeField] private ConsoleLink consoleLink;

        private readonly Dictionary<StimulusId, Slider> sliders = new();
        private readonly Dictionary<StimulusId, TextMeshProUGUI> values = new();
        private readonly List<(int number, Image image, TextMeshProUGUI label)> levelButtons = new();
        private TextMeshProUGUI connectionLabel;
        private TextMeshProUGUI swayLabel;
        private TextMeshProUGUI sudsLabel;

        public bool IsOpen => focus.IsOpen(this);

        protected override void Build(RectTransform root)
        {
            var panel = Factory.Panel("Operator", root, Theme.background);
            UiFactory.Anchor(panel.rectTransform, new Vector2(0f, 0.5f), new Vector2(620f, 0f), new Vector2(32f, 0f));
            panel.rectTransform.anchorMin = new Vector2(0f, 0.04f);
            panel.rectTransform.anchorMax = new Vector2(0f, 0.96f);
            panel.rectTransform.sizeDelta = new Vector2(620f, 0f);
            UiFactory.Vertical(panel.gameObject, 28, 14f);

            Factory.Label(panel.transform, "Panel de operador", Theme.titleSize * 0.8f, Theme.textPrimary, true);
            connectionLabel = Factory.Label(panel.transform, "Servidor: sin conexión", Theme.smallSize, Theme.textSecondary);

            Factory.Label(panel.transform, "Nivel (teclas 1–4)", Theme.smallSize, Theme.textSecondary, true);
            var levelGrid = UiFactory.Rect("Levels", panel.transform);
            var grid = levelGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(275f, 60f);
            grid.spacing = new Vector2(14f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            UiFactory.Size(levelGrid, preferredHeight: 130f);
            foreach (var level in session.Levels.Levels)
            {
                int number = level.number;
                var button = Factory.Button(levelGrid, $"{number} · {level.displayName}", () => session.GoToLevel(number));
                levelButtons.Add((number, (Image)button.targetGraphic, button.GetComponentInChildren<TextMeshProUGUI>()));
            }
            Factory.Button(panel.transform, "Volver al prado (descanso)", session.ReturnToNeutral, height: 56f);

            Factory.Label(panel.transform, "Estímulos", Theme.smallSize, Theme.textSecondary, true);
            foreach (var id in StimulusIds.All) BuildStimulusRow(panel.transform, id);

            sudsLabel = Factory.Label(panel.transform, "", Theme.smallSize, Theme.textSecondary, true);
            Factory.Button(panel.transform, "Pedir SUDS ahora", () => suds.Request(SudsTrigger.Local), ButtonStyle.Primary, 56f);
            swayLabel = Factory.Button(panel.transform, "", ToggleCameraSway, height: 56f).GetComponentInChildren<TextMeshProUGUI>();

            Factory.Label(panel.transform, "F1 cierra este panel · C activa el botón de calma", Theme.smallSize * 0.9f, Theme.textSecondary);
            SetVisible(false);
        }

        private void BuildStimulusRow(Transform parent, StimulusId id)
        {
            var row = UiFactory.Rect(id.ToString(), parent);
            UiFactory.Horizontal(row.gameObject, 16f);
            UiFactory.Size(row, preferredHeight: 44f);

            var name = Factory.Label(row, id.DisplayName(), Theme.smallSize, Theme.textPrimary);
            UiFactory.Size(name, preferredWidth: 130f);
            var slider = Factory.Slider(row, 0f, 100f, value => stimuli.SetTarget(id, value), Theme.accent);
            slider.wholeNumbers = true;
            var value = Factory.Label(row, "0", Theme.smallSize, Theme.textPrimary, true, TextAlignmentOptions.Right);
            UiFactory.Size(value, preferredWidth: 60f);

            sliders[id] = slider;
            values[id] = value;
        }

        private void OnEnable()
        {
            operatorInput.PanelToggled += Toggle;
            operatorInput.LevelKeyPressed += OnLevelKey;
        }

        private void OnDisable()
        {
            operatorInput.PanelToggled -= Toggle;
            operatorInput.LevelKeyPressed -= OnLevelKey;
        }

        private void Update()
        {
            if (!IsOpen) return;

            StimulusValues target = stimuli.Target;
            foreach (var id in StimulusIds.All)
            {
                sliders[id].SetValueWithoutNotify(target[id]);
                values[id].text = Mathf.RoundToInt(stimuli.Current[id]).ToString();
            }

            int current = session.CurrentLevel != null ? session.CurrentLevel.number : 0;
            foreach (var (number, image, label) in levelButtons)
            {
                bool active = number == current;
                image.color = active ? Theme.accent : Theme.surfaceRaised;
                label.color = active ? Theme.textOnAccent : Theme.textPrimary;
            }
            connectionLabel.text = ConnectionText();
            sudsLabel.text = $"Escala SUDS · cada {suds.IntervalSeconds:0} s durante la exposición";
            swayLabel.text = $"Balanceo de cámara: {(playerRig.CameraSwayEnabled ? "activado" : "desactivado")}";
        }

        private string ConnectionText()
        {
            string address = consoleLink.ServerAddress;
            if (!consoleLink.IsConnected) return $"Servidor: sin conexión · reintentando ({address})";
            string session = string.IsNullOrEmpty(consoleLink.SessionAlias) ? "sin sesión activa" : $"sesión con {consoleLink.SessionAlias}";
            string pending = consoleLink.PendingSuds > 0 ? $" · {consoleLink.PendingSuds} SUDS por confirmar" : "";
            return $"Servidor: conectado ({address}) · {session}{pending}";
        }

        private void Toggle()
        {
            bool open = !IsOpen;
            SetVisible(open);
            if (open) focus.Open(this);
            else focus.Close(this);
        }

        private void OnLevelKey(int number)
        {
            if (IsOpen) session.GoToLevel(number);
        }

        private void ToggleCameraSway() => playerRig.CameraSwayEnabled = !playerRig.CameraSwayEnabled;
    }
}
