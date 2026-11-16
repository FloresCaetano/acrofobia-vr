using System;
using Acrofobia.Core;
using Acrofobia.Stimuli;
using Acrofobia.UI;
using UnityEngine;

namespace Acrofobia.Suds
{
    /// <summary>
    /// Decides when to ask for a SUDS rating (every N seconds of exposure, or on request) and turns
    /// answers into <see cref="SudsRecord"/>s. The timer only runs while the patient is exposed and
    /// no panel is open, and it restarts on every level change.
    /// </summary>
    public sealed class SudsManager : MonoBehaviour
    {
        [SerializeField] private SudsSettings settings;
        [SerializeField] private SessionManager session;
        [SerializeField] private StimulusController stimuli;
        [SerializeField] private ModalFocus focus;
        [SerializeField] private SudsPanel panel;

        private float interval;
        private float elapsed;
        private SudsTrigger? pending;
        private int lastValue;

        /// <summary>Raised for every real answer (practice answers are not reported).</summary>
        public event Action<SudsRecord> Answered;
        public event Action IntervalChanged;

        public float IntervalSeconds => interval;
        public bool IsAsking => panel.IsOpen;

        private void Awake()
        {
            interval = settings.intervalSeconds;
            lastValue = settings.initialValue;
        }

        private void OnEnable()
        {
            session.CalmTriggered += OnCalm;
            session.StateChanged += ResetTimer;
            panel.Submitted += OnSubmitted;
        }

        private void OnDisable()
        {
            session.CalmTriggered -= OnCalm;
            session.StateChanged -= ResetTimer;
            panel.Submitted -= OnSubmitted;
        }

        private void Update()
        {
            if (pending.HasValue && !session.IsTransitioning && !panel.IsOpen)
            {
                panel.Open(lastValue, pending.Value);
                pending = null;
            }

            if (!session.IsInExposure || focus.AnyOpen || pending.HasValue) return;

            elapsed += Time.deltaTime;
            if (elapsed >= interval) Request(SudsTrigger.Auto);
        }

        /// <summary>Asks as soon as possible. Ignored if a question is already open or queued.</summary>
        public void Request(SudsTrigger trigger)
        {
            if (panel.IsOpen || pending.HasValue) return;
            pending = trigger;
            elapsed = 0f;
        }

        public void SetInterval(float seconds)
        {
            interval = Mathf.Clamp(seconds, SudsSettings.MinInterval, SudsSettings.MaxInterval);
            elapsed = Mathf.Min(elapsed, interval);
            IntervalChanged?.Invoke();
        }

        private void OnSubmitted(int value, SudsTrigger trigger)
        {
            lastValue = value;
            elapsed = 0f;
            if (trigger == SudsTrigger.Practice) return;

            int level = session.CurrentLevel != null ? session.CurrentLevel.number : 0;
            Answered?.Invoke(SudsRecord.Create(value, level, session.CurrentScene, trigger, stimuli.Current, DateTimeOffset.UtcNow));
        }

        private void OnCalm(CalmSource source)
        {
            pending = null;
            panel.Cancel();
            elapsed = 0f;
        }

        private void ResetTimer() => elapsed = 0f;
    }
}
