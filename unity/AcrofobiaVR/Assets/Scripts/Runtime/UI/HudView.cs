using Acrofobia.Core;
using Acrofobia.Data;
using Acrofobia.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Acrofobia.UI
{
    /// <summary>
    /// Minimal heads-up display: level name, a discreet connection dot, the calm key reminder and a
    /// short instruction that fades out after a few seconds.
    /// </summary>
    public sealed class HudView : UiView
    {
        private const float HintSeconds = 10f;

        [SerializeField] private SessionManager session;
        [SerializeField] private ConsoleLink consoleLink;

        private TextMeshProUGUI levelLabel;
        private TextMeshProUGUI hintLabel;
        private CanvasGroup hintGroup;
        private Image connectionDot;
        private TextMeshProUGUI connectionLabel;
        private float hintTimer;
        private bool wasConnected;

        protected override void Build(RectTransform root)
        {
            var levelChip = Factory.Panel("Level", root, Theme.background);
            UiFactory.Anchor(levelChip.rectTransform, new Vector2(0f, 1f), new Vector2(520f, 72f), new Vector2(32f, -32f));
            levelLabel = Factory.Label(levelChip.transform, "", Theme.bodySize, Theme.textPrimary, true);
            UiFactory.Stretch(levelLabel.rectTransform, 18f);
            levelLabel.alignment = TextAlignmentOptions.MidlineLeft;

            var connection = Factory.Panel("Connection", root, Theme.background);
            UiFactory.Anchor(connection.rectTransform, new Vector2(1f, 1f), new Vector2(300f, 56f), new Vector2(-32f, -32f));
            connectionDot = Factory.Circle("Dot", connection.transform, Theme.textSecondary);
            UiFactory.Anchor(connectionDot.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 18f), new Vector2(20f, 0f));
            connectionLabel = Factory.Label(connection.transform, "Sin servidor", Theme.smallSize, Theme.textSecondary);
            UiFactory.Stretch(connectionLabel.rectTransform);
            connectionLabel.rectTransform.offsetMin = new Vector2(50f, 0f);
            connectionLabel.alignment = TextAlignmentOptions.MidlineLeft;

            var calm = Factory.Panel("CalmReminder", root, Theme.background);
            UiFactory.Anchor(calm.rectTransform, new Vector2(1f, 0f), new Vector2(360f, 64f), new Vector2(-32f, 32f));
            var calmLabel = Factory.Label(calm.transform, "<color=#F2A93B><b>C</b></color>  Botón de calma", Theme.smallSize, Theme.textPrimary,
                alignment: TextAlignmentOptions.Center);
            UiFactory.Stretch(calmLabel.rectTransform, 8f);

            var hint = Factory.Panel("Hint", root, Theme.background);
            UiFactory.Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(1100f, 96f), new Vector2(0f, 120f));
            hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hintGroup.alpha = 0f;
            hintLabel = Factory.Label(hint.transform, "", Theme.bodySize, Theme.textPrimary, alignment: TextAlignmentOptions.Center);
            UiFactory.Stretch(hintLabel.rectTransform, 16f);
        }

        private void OnEnable()
        {
            session.LevelEntered += OnLevelEntered;
            session.NeutralEntered += OnNeutralEntered;
        }

        private void OnDisable()
        {
            session.LevelEntered -= OnLevelEntered;
            session.NeutralEntered -= OnNeutralEntered;
        }

        private void Update()
        {
            bool connected = consoleLink.IsConnected;
            if (connected != wasConnected || connectionLabel.text.Length == 0)
            {
                wasConnected = connected;
                SetConnection(connected, connected ? "Conectado" : "Sin servidor");
            }

            if (hintTimer <= 0f) return;
            hintTimer -= Time.unscaledDeltaTime;
            float fadeIn = Mathf.Clamp01((HintSeconds - hintTimer) / 0.6f);
            float fadeOut = Mathf.Clamp01(hintTimer / 1.2f);
            hintGroup.alpha = Mathf.Min(fadeIn, fadeOut);
        }

        private void SetConnection(bool connected, string text)
        {
            connectionDot.color = connected ? Theme.accent : Theme.textSecondary;
            connectionLabel.text = text;
            connectionLabel.color = connected ? Theme.textPrimary : Theme.textSecondary;
        }

        private void ShowHint(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            hintLabel.text = text;
            hintTimer = HintSeconds;
        }

        private void OnLevelEntered(LevelDefinition level)
        {
            levelLabel.text = level.Title;
            ShowHint(level.hint);
        }

        private void OnNeutralEntered(NeutralReason reason)
        {
            levelLabel.text = "Prado · Zona de descanso";
            hintTimer = 0f;
            hintGroup.alpha = 0f;
        }
    }
}
