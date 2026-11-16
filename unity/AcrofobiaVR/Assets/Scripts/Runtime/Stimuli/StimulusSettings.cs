using System;
using UnityEngine;

namespace Acrofobia.Stimuli
{
    /// <summary>Maps a 0–100 intensity to a real-world value through a response curve and a range.</summary>
    [Serializable]
    public sealed class StimulusMapping
    {
        public float min;
        public float max;
        [Tooltip("Shape of the response: x is intensity/100, y is the fraction of the range.")]
        public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public StimulusMapping(float min, float max, AnimationCurve curve = null)
        {
            this.min = min;
            this.max = max;
            if (curve != null) this.curve = curve;
        }

        public float Evaluate(float intensity)
        {
            float t = Mathf.Clamp01(intensity / StimulusValues.Max);
            return Mathf.LerpUnclamped(min, max, curve.Evaluate(t));
        }
    }

    /// <summary>Converts stimulus intensities into the values the environment understands.</summary>
    [CreateAssetMenu(menuName = "Acrofobia/Stimulus Settings", fileName = "StimulusSettings")]
    public sealed class StimulusSettings : ScriptableObject
    {
        [Tooltip("Seconds a change takes to reach its target.")]
        [Min(0f)] public float transitionSeconds = 2f;

        [Header("Height")]
        [Tooltip("Depth of the canyon floor under the bridge, in meters.")]
        public StimulusMapping floorDepth = new(10f, 150f);

        [Header("Wind")]
        public StimulusMapping windParticleRate = new(0f, 220f);
        public StimulusMapping windVolume = new(0.05f, 0.9f);
        [Tooltip("Rope flutter amplitude, in meters.")]
        public StimulusMapping ropeFlutter = new(0f, 0.09f);

        [Header("Sway")]
        [Tooltip("Lateral sway at mid-span, in meters.")]
        public StimulusMapping swayAmplitude = new(0f, 0.55f);
        [Tooltip("Sway frequency, in hertz.")]
        public StimulusMapping swayFrequency = new(0.12f, 0.34f);

        [Header("Fog")]
        [Tooltip("Exponential squared fog density.")]
        public StimulusMapping fogDensity = new(0.0007f, 0.016f, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));

        [Header("Sound")]
        public StimulusMapping ambienceVolume = new(0f, 1f);
    }
}
