using System;
using Acrofobia.Controls;
using Acrofobia.Suds;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Acrofobia.UI
{
    /// <summary>
    /// The SUDS question. The value is set with the wheel, the arrow keys or by dragging, and is shown
    /// as a number, a word, a color and a face. Enter or the button confirms.
    /// </summary>
    public sealed class SudsPanel : UiView
    {
        [SerializeField] private ModalFocus focus;
        [SerializeField] private SudsSettings settings;
        [Tooltip("Component that implements IPlayerInputSource.")]
        [SerializeField] private MonoBehaviour inputSource;

        private IPlayerInputSource input;
        private Slider slider;
        private Image fill;
        private FaceGraphic face;
        private TextMeshProUGUI number;
        private TextMeshProUGUI word;
        private SudsTrigger trigger;
        private float openedAt;

        public event Action<int, SudsTrigger> Submitted;

        public bool IsOpen => focus.IsOpen(this);
        private int Value => Mathf.RoundToInt(slider.value);

        protected override void Awake()
        {
            input = (IPlayerInputSource)inputSource;
            base.Awake();
        }

        protected override void Build(RectTransform root)
        {
            var dim = Factory.Panel("Dim", root, new Color(0f, 0f, 0f, 0.35f), rounded: false);
            UiFactory.Stretch(dim.rectTransform);

            var card = Factory.Panel("Card", root, Theme.background);
            UiFactory.Anchor(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 640f), Vector2.zero);
            UiFactory.Vertical(card.gameObject, 44, 22f, TextAnchor.UpperCenter);

            Factory.Label(card.transform, "¿Qué tan ansioso te sientes ahora?", Theme.titleSize, Theme.textPrimary, true, TextAlignmentOptions.Center);
            Factory.Label(card.transform, "0 = totalmente tranquilo   ·   100 = el peor miedo de tu vida",
                Theme.smallSize, Theme.textSecondary, alignment: TextAlignmentOptions.Center);

            var row = UiFactory.Rect("Reading", card.transform);
            UiFactory.Horizontal(row.gameObject, 36f, TextAnchor.MiddleCenter);
            UiFactory.Size(row, preferredHeight: 190f);

            face = UiFactory.Rect("Face", row).gameObject.AddComponent<FaceGraphic>();
            face.raycastTarget = false;
            UiFactory.Size(face, 180f, 180f);

            var readout = UiFactory.Rect("Readout", row);
            UiFactory.Vertical(readout.gameObject, 0, 0f, TextAnchor.MiddleLeft);
            UiFactory.Size(readout, 420f, 190f);
            number = Factory.Label(readout, "50", 120f, Theme.textPrimary, true);
            word = Factory.Label(readout, "", Theme.bodySize, Theme.textPrimary);

            slider = Factory.Slider(card.transform, 0f, 100f, _ => Refresh(), Theme.accent, 56f);
            slider.wholeNumbers = true;
            fill = slider.fillRect.GetComponent<Image>();

            Factory.Label(card.transform, "Rueda del mouse o flechas para ajustar  ·  Enter o clic para confirmar",
                Theme.smallSize, Theme.textSecondary, alignment: TextAlignmentOptions.Center);
            Factory.Button(card.transform, "Confirmar", Submit, ButtonStyle.Primary, 72f);

            SetVisible(false);
        }

        private void OnEnable()
        {
            input.ValueStepped += OnStep;
            input.ConfirmPressed += OnConfirm;
        }

        private void OnDisable()
        {
            input.ValueStepped -= OnStep;
            input.ConfirmPressed -= OnConfirm;
        }

        public void Open(int initialValue, SudsTrigger reason)
        {
            trigger = reason;
            openedAt = Time.unscaledTime;
            slider.SetValueWithoutNotify(SudsScale.Clamp(initialValue));
            Refresh();
            SetVisible(true);
            focus.Open(this);
        }

        /// <summary>Closes without an answer (for example, when the calm button is pressed).</summary>
        public void Cancel()
        {
            if (!IsOpen) return;
            SetVisible(false);
            focus.Close(this);
        }

        private void Submit()
        {
            if (!IsOpen) return;
            int value = Value;
            SetVisible(false);
            focus.Close(this);
            Submitted?.Invoke(value, trigger);
        }

        private void OnStep(int direction)
        {
            if (focus.IsTop(this)) slider.value = Mathf.Clamp(Value + direction * settings.step, 0, 100);
        }

        private void OnConfirm()
        {
            // A short guard avoids confirming with the same Enter press that advanced the tutorial.
            if (focus.IsTop(this) && Time.unscaledTime - openedAt > 0.25f) Submit();
        }

        private void Refresh()
        {
            int value = Value;
            Color color = SudsScale.Color(value);
            number.text = value.ToString();
            number.color = color;
            word.text = SudsScale.Label(value);
            fill.color = color;
            face.FaceColor = color;
            face.Fear = value / 100f;
        }
    }
}
