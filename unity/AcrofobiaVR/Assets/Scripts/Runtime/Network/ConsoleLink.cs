using System.IO;
using System.Linq;
using Acrofobia.Core;
using Acrofobia.Stimuli;
using Acrofobia.Suds;
using UnityEngine;

namespace Acrofobia.Network
{
    /// <summary>
    /// Bridge between the therapist console and the app: turns protocol messages into session commands
    /// and reports state and SUDS answers back. The managers know nothing about the network.
    /// </summary>
    public sealed class ConsoleLink : MonoBehaviour
    {
        private const float StateThrottleSeconds = 0.25f;

        [SerializeField] private NetworkClient network;
        [SerializeField] private NetworkSettings settings;
        [SerializeField] private SessionManager session;
        [SerializeField] private StimulusController stimuli;
        [SerializeField] private SudsManager suds;

        private SudsOutbox outbox;
        private float nextPeriodicState;
        private float pendingStateAt = -1f;

        public bool IsConnected => network.Status == ConnectionStatus.Connected;
        public string ServerAddress => network.ServerUri?.ToString() ?? "";
        public string SessionAlias { get; private set; }
        public int PendingSuds => outbox.Count;

        private void Awake()
        {
            outbox = new SudsOutbox(Path.Combine(Application.persistentDataPath, "suds_outbox.json"));
        }

        private void OnEnable()
        {
            network.StatusChanged += OnStatusChanged;
            network.MessageReceived += OnMessage;
            session.StateChanged += ScheduleState;
            session.CalmTriggered += OnCalmTriggered;
            stimuli.TargetsChanged += ScheduleState;
            suds.IntervalChanged += ScheduleState;
            suds.Answered += OnSudsAnswered;
        }

        private void OnDisable()
        {
            network.StatusChanged -= OnStatusChanged;
            network.MessageReceived -= OnMessage;
            session.StateChanged -= ScheduleState;
            session.CalmTriggered -= OnCalmTriggered;
            stimuli.TargetsChanged -= ScheduleState;
            suds.IntervalChanged -= ScheduleState;
            suds.Answered -= OnSudsAnswered;
        }

        private void Update()
        {
            if (!IsConnected) return;

            bool throttleElapsed = pendingStateAt >= 0f && Time.unscaledTime >= pendingStateAt;
            if (throttleElapsed || Time.unscaledTime >= nextPeriodicState) SendState();
        }

        private void OnStatusChanged(ConnectionStatus status)
        {
            if (status == ConnectionStatus.Disconnected) outbox.MarkAllQueued();
            if (status != ConnectionStatus.Connected) return;

            network.Send(Protocol.Serialize(Protocol.Hello, new HelloPayload
            {
                client = "AcrofobiaVR Unity",
                version = Application.version,
                levels = session.Levels.Levels.Select(level => new LevelInfo
                {
                    number = level.number,
                    name = level.displayName,
                    description = level.description,
                }).ToArray(),
            }));
            FlushOutbox();
            SendState();
        }

        private void OnMessage(string type, string json)
        {
            switch (type)
            {
                case Protocol.LevelSet:
                    session.GoToLevel(Protocol.ReadPayload<LevelSetPayload>(json).level);
                    break;
                case Protocol.StimulusSet:
                    var change = Protocol.ReadPayload<StimulusSetPayload>(json);
                    if (StimulusIds.TryParse(change.stimulus, out var id)) stimuli.SetTarget(id, change.value);
                    break;
                case Protocol.CalmTrigger:
                    session.TriggerCalm(CalmSource.Console);
                    break;
                case Protocol.SudsRequest:
                    suds.Request(SudsTrigger.Console);
                    break;
                case Protocol.SudsConfig:
                    suds.SetInterval(Protocol.ReadPayload<SudsConfigPayload>(json).intervalSeconds);
                    break;
                case Protocol.SudsAck:
                    outbox.Acknowledge(Protocol.ReadPayload<IdPayload>(json).id);
                    break;
                case Protocol.SessionStart:
                    SessionAlias = Protocol.ReadPayload<SessionPayload>(json).patientAlias;
                    break;
                case Protocol.SessionEnd:
                    SessionAlias = null;
                    break;
                case Protocol.Error:
                    var error = Protocol.ReadPayload<ErrorPayload>(json);
                    Debug.LogWarning($"[ConsoleLink] Server error {error.code}: {error.message}");
                    break;
            }
        }

        private void OnSudsAnswered(SudsRecord record)
        {
            outbox.Add(record, sentOffline: !IsConnected);
            FlushOutbox();
            ScheduleState();
        }

        private void OnCalmTriggered(CalmSource source)
        {
            // Console-initiated calm is already known to the server; report only the local ones.
            if (source != CalmSource.Console) network.Send(Protocol.Serialize(Protocol.CalmTrigger, new SourcePayload { source = "patient" }));
        }

        private void FlushOutbox()
        {
            foreach (var entry in outbox.Pending)
            {
                var record = entry.record;
                bool sent = network.Send(Protocol.Serialize(Protocol.SudsResponse, new SudsResponsePayload
                {
                    id = record.id,
                    value = record.value,
                    level = record.level,
                    scene = record.scene?.ToLowerInvariant(),
                    trigger = record.trigger,
                    answeredAt = record.answeredAt,
                    queued = entry.queued,
                    stimuli = record.stimuli,
                }));
                if (!sent) return;
            }
        }

        private void ScheduleState()
        {
            if (pendingStateAt < 0f) pendingStateAt = Time.unscaledTime + StateThrottleSeconds;
        }

        private void SendState()
        {
            pendingStateAt = -1f;
            nextPeriodicState = Time.unscaledTime + settings.stateIntervalSeconds;
            var level = session.CurrentLevel;
            network.Send(Protocol.Serialize(Protocol.StateUpdate, new StatePayload
            {
                scene = session.CurrentScene?.ToLowerInvariant() ?? "",
                level = level != null ? level.number : 0,
                levelName = level != null ? level.displayName : "Prado",
                stimuli = stimuli.Current,
                targets = stimuli.Target,
                sudsIntervalSeconds = suds.IntervalSeconds,
                transitioning = session.IsTransitioning,
                sudsOpen = suds.IsAsking,
            }));
        }
    }
}
