using System;
using UnityEngine;

namespace Acrofobia.Stimuli
{
    /// <summary>
    /// Owns the stimulus targets (set by the console, the operator panel or the level defaults)
    /// and publishes the interpolated values to <see cref="StimulusState"/> every frame.
    /// </summary>
    public sealed class StimulusController : MonoBehaviour
    {
        [SerializeField] private StimulusState state;

        private StimulusBlender blender;

        /// <summary>Raised when a target changes, for state reporting and UI refresh.</summary>
        public event Action TargetsChanged;

        public StimulusValues Current => blender.Current;
        public StimulusValues Target => blender.Target;

        private void Awake()
        {
            blender = new StimulusBlender(state.Settings.transitionSeconds, StimulusValues.Minimum);
            Publish();
        }

        private void Update()
        {
            blender.Update(Time.unscaledTime);
            Publish();
        }

        private void OnDestroy() => state.Release();

        public void SetTarget(StimulusId id, float value)
        {
            if (blender.SetTarget(id, value, Time.unscaledTime)) TargetsChanged?.Invoke();
        }

        /// <summary>Applies values instantly. Used while the screen is faded out (level changes, calm).</summary>
        public void SetImmediate(StimulusValues values)
        {
            blender.Reset(values);
            Publish();
            TargetsChanged?.Invoke();
        }

        private void Publish() => state.Publish(blender.Current, blender.Target);
    }
}
