using UnityEngine;

namespace Acrofobia.Stimuli
{
    /// <summary>
    /// Smooth, time-based interpolation for the five stimuli. Each channel eases from its current
    /// value to the new target over a fixed duration; a new target mid-transition starts from where
    /// the value is, so rapid slider changes never cause jumps. Pure C#, unit-tested.
    /// </summary>
    public sealed class StimulusBlender
    {
        private readonly float duration;
        private readonly Transition[] transitions = new Transition[StimulusValues.Count];
        private StimulusValues current;
        private StimulusValues target;

        public StimulusBlender(float durationSeconds, StimulusValues initial)
        {
            duration = Mathf.Max(0f, durationSeconds);
            Reset(initial);
        }

        public StimulusValues Current => current;
        public StimulusValues Target => target;

        /// <summary>Jumps to <paramref name="values"/> without interpolation.</summary>
        public void Reset(StimulusValues values)
        {
            current = values;
            target = values;
            foreach (var id in StimulusIds.All)
            {
                transitions[(int)id] = new Transition(values[id], values[id], float.NegativeInfinity);
            }
        }

        /// <summary>Returns false when the value is already the target.</summary>
        public bool SetTarget(StimulusId id, float value, float now)
        {
            value = Mathf.Clamp(value, StimulusValues.Min, StimulusValues.Max);
            if (Mathf.Approximately(target[id], value)) return false;

            transitions[(int)id] = new Transition(current[id], value, now);
            target[id] = value;
            return true;
        }

        public void Update(float now)
        {
            foreach (var id in StimulusIds.All)
            {
                var transition = transitions[(int)id];
                float t = duration <= 0f ? 1f : Mathf.Clamp01((now - transition.StartTime) / duration);
                current[id] = Mathf.Lerp(transition.From, transition.To, Mathf.SmoothStep(0f, 1f, t));
            }
        }

        private readonly struct Transition
        {
            public readonly float From;
            public readonly float To;
            public readonly float StartTime;

            public Transition(float from, float to, float startTime)
            {
                From = from;
                To = to;
                StartTime = startTime;
            }
        }
    }
}
