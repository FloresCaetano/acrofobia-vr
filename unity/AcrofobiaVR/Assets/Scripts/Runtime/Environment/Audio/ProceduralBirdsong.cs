using UnityEngine;

namespace Acrofobia.Environment.Audio
{
    /// <summary>
    /// Distant birds synthesized on the audio thread: short phrases of frequency-swept sine chirps
    /// at random intervals. Gentle and license-free ambience for the meadow.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class ProceduralBirdsong : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float volume = 0.12f;
        [SerializeField] private Vector2 pauseSeconds = new(1.5f, 5f);

        private AudioSource source;
        private int sampleRate;
        private uint rng = 0x2545F491u;
        private volatile float targetVolume;

        // Audio-thread state.
        private float silence;
        private int chirpsLeft;
        private float chirpTime, chirpLength, startHz, endHz, phase, pan;

        private void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate;
            source = GetComponent<AudioSource>();
            source.clip = AudioClip.Create("Silence", sampleRate, 1, sampleRate, false);
            source.loop = true;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            silence = 1f;
        }

        private void OnEnable() => source.Play();

        private void Update() => targetVolume = volume;

        private void OnAudioFilterRead(float[] data, int channels)
        {
            float dt = 1f / sampleRate;
            float gain = targetVolume;
            for (int i = 0; i < data.Length; i += channels)
            {
                float sample = 0f;
                if (silence > 0f)
                {
                    silence -= dt;
                    if (silence <= 0f) StartPhrase();
                }
                else if ((chirpTime += dt) >= 0f)
                {
                    float t = chirpTime / chirpLength;
                    if (t >= 1f)
                    {
                        NextChirp();
                    }
                    else
                    {
                        float frequency = Mathf.Lerp(startHz, endHz, t * t);
                        phase += 2f * Mathf.PI * frequency * dt;
                        if (phase > Mathf.PI * 2f) phase -= Mathf.PI * 2f;
                        float envelope = Mathf.Sin(Mathf.PI * t);
                        sample = Mathf.Sin(phase) * envelope * envelope * gain;
                    }
                }

                if (channels == 2)
                {
                    data[i] += sample * (1f - pan);
                    data[i + 1] += sample * pan;
                }
                else
                {
                    for (int c = 0; c < channels; c++) data[i + c] += sample;
                }
            }
        }

        private void StartPhrase()
        {
            chirpsLeft = 2 + (int)(Random01() * 5f);
            pan = 0.2f + Random01() * 0.6f;
            NextChirp();
        }

        private void NextChirp()
        {
            if (chirpsLeft-- <= 0)
            {
                silence = Mathf.Lerp(pauseSeconds.x, pauseSeconds.y, Random01());
                return;
            }
            chirpTime = -0.03f - Random01() * 0.08f; // short gap before each chirp
            chirpLength = 0.05f + Random01() * 0.12f;
            startHz = 2600f + Random01() * 2200f;
            endHz = startHz * (0.7f + Random01() * 0.7f);
        }

        private float Random01()
        {
            rng ^= rng << 13;
            rng ^= rng >> 17;
            rng ^= rng << 5;
            return rng / (float)uint.MaxValue;
        }
    }
}
