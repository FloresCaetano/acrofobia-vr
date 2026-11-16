using Acrofobia.Controls;
using Acrofobia.Core;
using Acrofobia.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Acrofobia.UI
{
    /// <summary>
    /// Reassuring message shown after the calm button, with a breathing guide: the circle grows for
    /// four seconds (inhale) and shrinks for six (exhale).
    /// </summary>
    public sealed class CalmView : UiView
    {
        private const float Inhale = 4f;
        private const float Exhale = 6f;

        [SerializeField] private SessionManager session;
        [SerializeField] private MonoBehaviour inputSource;

        private IPlayerInputSource input;
        private CanvasGroup group;
        private RectTransform circle;
        private TextMeshProUGUI breathLabel;
        private bool visible;
        private float time;

        protected override void Awake()
        {
            input = (IPlayerInputSource)inputSource;
            base.Awake();
        }

        protected override void Build(RectTransform root)
        {
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0f;

            var card = Factory.Panel("Card", root, Theme.background);
            UiFactory.Anchor(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(980f, 560f), new Vector2(0f, 40f));
            UiFactory.Vertical(card.gameObject, 44, 18f, TextAnchor.UpperCenter);

            Factory.Label(card.transform, "Estás a salvo", Theme.titleSize * 1.2f, Theme.calm, true, TextAlignmentOptions.Center);
            Factory.Label(card.transform, "Ya no estás en el puente. Quédate aquí todo el tiempo que necesites.",
                Theme.bodySize, Theme.textPrimary, alignment: TextAlignmentOptions.Center);

            var holder = UiFactory.Rect("Breathing", card.transform);
            UiFactory.Size(holder, preferredHeight: 230f);
            var ring = Factory.Circle("Ring", holder, new Color(Theme.calm.r, Theme.calm.g, Theme.calm.b, 0.25f));
            UiFactory.Anchor(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(220f, 220f), Vector2.zero);
            var dot = Factory.Circle("Circle", holder, Theme.calm);
            circle = dot.rectTransform;
            UiFactory.Anchor(circle, new Vector2(0.5f, 0.5f), new Vector2(120f, 120f), Vector2.zero);
            breathLabel = Factory.Label(holder, "", Theme.bodySize, Theme.textOnAccent, true, TextAlignmentOptions.Center);
            UiFactory.Stretch(breathLabel.rectTransform);

            Factory.Label(card.transform, "Pulsa Enter cuando te sientas mejor.", Theme.smallSize, Theme.textSecondary,
                alignment: TextAlignmentOptions.Center);
        }

        private void OnEnable()
        {
            session.NeutralEntered += OnNeutralEntered;
            session.LevelEntered += Hide;
            input.ConfirmPressed += OnConfirm;
        }

        private void OnDisable()
        {
            session.NeutralEntered -= OnNeutralEntered;
            session.LevelEntered -= Hide;
            input.ConfirmPressed -= OnConfirm;
        }

        private void Update()
        {
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime * 1.5f);
            if (!visible) return;

            time = (time + Time.unscaledDeltaTime) % (Inhale + Exhale);
            bool inhaling = time < Inhale;
            float t = inhaling ? time / Inhale : 1f - (time - Inhale) / Exhale;
            float size = Mathf.Lerp(90f, 210f, Mathf.SmoothStep(0f, 1f, t));
            circle.sizeDelta = new Vector2(size, size);
            breathLabel.text = inhaling ? "Inhala" : "Exhala";
        }

        private void OnNeutralEntered(NeutralReason reason)
        {
            if (reason != NeutralReason.Calm) return;
            visible = true;
            time = 0f;
        }

        private void Hide(LevelDefinition _) => visible = false;

        private void OnConfirm()
        {
            if (visible && group.alpha > 0.9f) visible = false;
        }
    }
}
