using System;
using UnityEngine;

namespace Acrofobia.Stimuli
{
    public enum StimulusId
    {
        Height,
        Wind,
        Sway,
        Fog,
        Sound,
    }

    /// <summary>The five stimulus intensities, each on the 0–100 scale used by the therapist.</summary>
    [Serializable]
    public struct StimulusValues : IEquatable<StimulusValues>
    {
        public const int Count = 5;
        public const float Min = 0f;
        public const float Max = 100f;

        [Range(Min, Max)] public float height;
        [Range(Min, Max)] public float wind;
        [Range(Min, Max)] public float sway;
        [Range(Min, Max)] public float fog;
        [Range(Min, Max)] public float sound;

        public StimulusValues(float height, float wind, float sway, float fog, float sound)
        {
            this.height = height;
            this.wind = wind;
            this.sway = sway;
            this.fog = fog;
            this.sound = sound;
        }

        public static StimulusValues Minimum => default;

        public float this[StimulusId id]
        {
            readonly get => id switch
            {
                StimulusId.Height => height,
                StimulusId.Wind => wind,
                StimulusId.Sway => sway,
                StimulusId.Fog => fog,
                StimulusId.Sound => sound,
                _ => throw new ArgumentOutOfRangeException(nameof(id)),
            };
            set
            {
                float clamped = Mathf.Clamp(value, Min, Max);
                switch (id)
                {
                    case StimulusId.Height: height = clamped; break;
                    case StimulusId.Wind: wind = clamped; break;
                    case StimulusId.Sway: sway = clamped; break;
                    case StimulusId.Fog: fog = clamped; break;
                    case StimulusId.Sound: sound = clamped; break;
                    default: throw new ArgumentOutOfRangeException(nameof(id));
                }
            }
        }

        public readonly bool Equals(StimulusValues other) =>
            height.Equals(other.height) && wind.Equals(other.wind) && sway.Equals(other.sway) &&
            fog.Equals(other.fog) && sound.Equals(other.sound);

        public override readonly bool Equals(object obj) => obj is StimulusValues other && Equals(other);
        public override readonly int GetHashCode() => HashCode.Combine(height, wind, sway, fog, sound);
        public override readonly string ToString() => $"h={height:0} w={wind:0} s={sway:0} f={fog:0} snd={sound:0}";
    }

    public static class StimulusIds
    {
        public static readonly StimulusId[] All = (StimulusId[])Enum.GetValues(typeof(StimulusId));

        /// <summary>Protocol key, shared with the server and the console.</summary>
        public static string Key(this StimulusId id) => id switch
        {
            StimulusId.Height => "height",
            StimulusId.Wind => "wind",
            StimulusId.Sway => "sway",
            StimulusId.Fog => "fog",
            StimulusId.Sound => "sound",
            _ => throw new ArgumentOutOfRangeException(nameof(id)),
        };

        /// <summary>Label shown to people, in Spanish.</summary>
        public static string DisplayName(this StimulusId id) => id switch
        {
            StimulusId.Height => "Altura",
            StimulusId.Wind => "Viento",
            StimulusId.Sway => "Balanceo",
            StimulusId.Fog => "Niebla",
            StimulusId.Sound => "Sonido",
            _ => throw new ArgumentOutOfRangeException(nameof(id)),
        };

        public static bool TryParse(string key, out StimulusId id)
        {
            foreach (var candidate in All)
            {
                if (candidate.Key() == key)
                {
                    id = candidate;
                    return true;
                }
            }
            id = default;
            return false;
        }
    }
}
