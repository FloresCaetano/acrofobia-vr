using Acrofobia.Controls;
using Acrofobia.Core;
using Acrofobia.Data;
using Acrofobia.Suds;
using TMPro;
using UnityEngine;

namespace Acrofobia.UI
{
    /// <summary>
    /// Disclaimer and step-by-step tutorial shown on startup in the meadow. It is not modal, so the
    /// patient can practice looking around while reading. Enter advances; the SUDS step opens a practice question.
    /// </summary>
    public sealed class IntroView : UiView
    {
        [SerializeField] private SessionManager session;
        [SerializeField] private SudsManager suds;
        [SerializeField] private SudsPanel sudsPanel;
        [SerializeField] private ModalFocus focus;
        [SerializeField] private MonoBehaviour inputSource;

        private static readonly Step[] Steps =
        {
            new("Prototipo académico",
                "<b>No es una herramienta clínica validada.</b>\nEste prototipo se desarrolló con fines académicos en la ESPE.",
                "Pulsa Enter para continuar"),
            new("1 · Mirar", "Mueve el mouse para mirar a tu alrededor. Tómate tu tiempo.", "Pulsa Enter para continuar"),
            new("2 · Moverte", "Pulsa W para avanzar y S para retroceder. Te moverás despacio y solo dentro de la zona de cada nivel.",
                "Pulsa Enter para continuar"),
            new("3 · Escala SUDS",
                "Cada cierto tiempo te preguntaremos qué tan ansioso te sientes, de 0 (totalmente tranquilo) a 100 (el peor miedo de tu vida). Usa la rueda del mouse o las flechas y confirma con Enter.",
                "Pulsa Enter para probarla ahora", practicesSuds: true),
            new("4 · Botón de calma",
                "Si en algún momento quieres parar, pulsa <b><color=#F2A93B>C</color></b>. Volverás de inmediato a este prado.",
                "Pulsa Enter para continuar"),
            new("Todo listo",
                "Tu terapeuta iniciará la exposición cuando estés preparado. Mientras tanto, puedes quedarte aquí.",
                "Pulsa Enter para cerrar"),
        };

        private IPlayerInputSource input;
        private CanvasGroup group;
        private TextMeshProUGUI title;
        private TextMeshProUGUI body;
        private TextMeshProUGUI prompt;
        private TextMeshProUGUI progress;
        private int step = -1;

        private bool Active => step >= 0 && step < Steps.Length;

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
            UiFactory.Anchor(card.rectTransform, new Vector2(0.5f, 0f), new Vector2(1180f, 330f), new Vector2(0f, 110f));
            UiFactory.Vertical(card.gameObject, 40, 14f);

            progress = Factory.Label(card.transform, "", Theme.smallSize, Theme.accent, true);
            title = Factory.Label(card.transform, "", Theme.titleSize, Theme.textPrimary, true);
            body = Factory.Label(card.transform, "", Theme.bodySize, Theme.textPrimary);
            prompt = Factory.Label(card.transform, "", Theme.smallSize, Theme.textSecondary, alignment: TextAlignmentOptions.Right);
        }

        private void OnEnable()
        {
            session.NeutralEntered += OnNeutralEntered;
            session.LevelEntered += OnLevelEntered;
            sudsPanel.Submitted += OnPracticeAnswered;
            input.ConfirmPressed += Advance;
        }

        private void OnDisable()
        {
            session.NeutralEntered -= OnNeutralEntered;
            session.LevelEntered -= OnLevelEntered;
            sudsPanel.Submitted -= OnPracticeAnswered;
            input.ConfirmPressed -= Advance;
        }

        private void Update()
        {
            group.alpha = Mathf.MoveTowards(group.alpha, Active && !focus.AnyOpen ? 1f : 0f, Time.unscaledDeltaTime * 2.5f);
        }

        private void OnNeutralEntered(NeutralReason reason)
        {
            if (reason == NeutralReason.Startup) Show(0);
        }

        private void OnLevelEntered(LevelDefinition _) => step = -1;

        private void Advance()
        {
            if (!Active || focus.AnyOpen || group.alpha < 0.9f) return;

            if (Steps[step].PracticesSuds)
            {
                suds.Request(SudsTrigger.Practice);
                return;
            }
            Show(step + 1);
        }

        private void OnPracticeAnswered(int value, SudsTrigger trigger)
        {
            if (trigger == SudsTrigger.Practice && Active) Show(step + 1);
        }

        private void Show(int index)
        {
            step = index;
            if (!Active) return;

            var current = Steps[step];
            progress.text = step == 0 ? "Aviso" : $"Tutorial · paso {step} de {Steps.Length - 1}";
            title.text = current.Title;
            body.text = current.Body;
            prompt.text = current.Prompt;
        }

        private readonly struct Step
        {
            public readonly string Title;
            public readonly string Body;
            public readonly string Prompt;
            public readonly bool PracticesSuds;

            public Step(string title, string body, string prompt, bool practicesSuds = false)
            {
                Title = title;
                Body = body;
                Prompt = prompt;
                PracticesSuds = practicesSuds;
            }
        }
    }
}
