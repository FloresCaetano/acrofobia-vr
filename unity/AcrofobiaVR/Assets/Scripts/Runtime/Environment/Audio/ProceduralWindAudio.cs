using UnityEngine;

namespace Acrofobia.Environment.Audio
{
    /// <summary>
    /// Wind synthesized on the audio thread: brown noise through a band-pass filter whose cutoff
    /// follows slow gusts. No audio clips are needed, so there is nothing to license.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class ProceduralWindAudio : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float volume = 0.4f;
        [SerializeField, Range(0f, 1f)] private float intensity = 0.5f;
        [SerializeField] private float baseCutoff = 220f;
        [SerializeField] private float gustCutoff = 900f;
        [SerializeField] private float gustRate = 0.11f;
        [SerializeField] private float smoothingSeconds = 0.4f;

        private AudioSource source;
        private int sampleRate;
        private uint rng = 0x9E3779B9u;
        private float brown;
        private float low, band;
        private double time;
        private float appliedVolume, appliedIntensity;

        // Written by the main thread, read by the audio thread.
        private volatile float targetVolume;
        private volatile float targetIntensity;

        public float Volume
        {
            get => volume;
            set => volume = Mathf.Clamp01(value);
        }

        public float Intensity
        {
            get => intensity;
            set => intensity = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate;
            source = GetComponent<AudioSource>();
            // A silent looping clip keeps the source playing so OnAudioFilterRead receives buffers.
            source.clip = AudioClip.Create("Silence", sampleRate, 1, sampleRate, false);
            source.loop = true;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
        }

        private void OnEnable() => source.Play();

        private void Update()
        {
            targetVolume = volume;
            targetIntensity = intensity;
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            float dt = 1f / sampleRate;
            float smoothing = 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, smoothingSeconds));

            for (int i = 0; i < data.Length; i += channels)
            {
                appliedVolume += (targetVolume - appliedVolume) * smoothing;
                appliedIntensity += (targetIntensity - appliedIntensity) * smoothing;
                time += dt;

                float white = NextRandom();
                brown = Mathf.Clamp((brown + 0.02f * white) / 1.02f, -1f, 1f);

                float gust = 0.5f + 0.5f * Mathf.Sin((float)(time * gustRate * Mathf.PI * 2.0)) * Mathf.Sin((float)(time * gustRate * 2.7));
                float cutoff = baseCutoff + gustCutoff * appliedIntensity * (0.4f + 0.6f * gust);

                // Chamberlin state-variable filter, band-pass output.
                float f = 2f * Mathf.Sin(Mathf.PI * Mathf.Min(cutoff, sampleRate * 0.2f) / sampleRate);
                low += f * band;
                float high = brown * 3f + white * 0.08f * appliedIntensity - low - 0.9f * band;
                band += f * high;

                float sample = (band * 1.6f + low * 0.5f) * appliedVolume * (0.55f + 0.45f * gust);
                for (int c = 0; c < channels; c++) data[i + c] += sample;
            }
        }

        private float NextRandom()
        {
            // xorshift32: allocation-free and safe to call from the audio thread.
            rng ^= rng << 13;
            rng ^= rng >> 17;
            rng ^= rng << 5;
            return rng / (float)uint.MaxValue * 2f - 1f;
        }
    }
}
