using Acrofobia.Rendering;
using UnityEngine;

namespace Acrofobia.Environment.Bridge
{
    /// <summary>
    /// Kinematic model of the bridge in its local space: +Z runs along the span, the anchors sit at
    /// z = ±span/2 and y = 0 is the walking surface at the anchors. Sway is driven by a sine plus
    /// low-frequency noise, never by physics, so it is stable and fully deterministic.
    /// </summary>
    public sealed class BridgeShape
    {
        private readonly BridgeSettings settings;
        private readonly Catenary deck;
        private float phase;
        private float time;

        public BridgeShape(BridgeSettings settings)
        {
            this.settings = settings;
            deck = Catenary.FromSag(settings.span, settings.deckSag);
        }

        /// <summary>Peak lateral displacement at mid-span, in meters.</summary>
        public float SwayAmplitude { get; set; }

        /// <summary>Sway oscillation frequency, in hertz.</summary>
        public float SwayFrequency { get; set; } = 0.2f;

        /// <summary>Peak rope flutter caused by wind, in meters.</summary>
        public float WindFlutter { get; set; }

        public float Span => settings.span;

        public void Advance(float deltaTime)
        {
            phase = (phase + Mathf.PI * 2f * SwayFrequency * deltaTime) % (Mathf.PI * 2f);
            time += deltaTime;
        }

        /// <summary>Signed sway signal in about [-1, 1]: a dominant sine with a slow random drift.</summary>
        private float SwaySignal => 0.8f * Mathf.Sin(phase) + 0.3f * Noise.Signed1(time * 0.23f, 3f);

        public Vector3 DeckPoint(float u)
        {
            float bell = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u));
            float lateral = SwayAmplitude * bell * SwaySignal;
            float bob = SwayAmplitude * 0.15f * bell * Mathf.Cos(phase * 2f);
            return new Vector3(lateral, -deck.Drop(u) + bob, (u - 0.5f) * settings.span);
        }

        public Quaternion DeckRotation(float u)
        {
            const float epsilon = 0.002f;
            Vector3 tangent = DeckPoint(u + epsilon) - DeckPoint(u - epsilon);
            float lateral = DeckPoint(u).x;
            Quaternion roll = Quaternion.AngleAxis(lateral * settings.rollPerMeter, Vector3.forward);
            return Quaternion.LookRotation(tangent.normalized, roll * Vector3.up);
        }

        /// <summary>Point attached to the deck at normalized position <paramref name="u"/>, offset in the deck's own frame.</summary>
        public Vector3 DeckAttached(float u, Vector3 localOffset)
        {
            return DeckPoint(u) + DeckRotation(u) * localOffset;
        }

        /// <summary>Height of the main cable above the walking surface: highest at the posts, lowest at mid-span.</summary>
        public float CableHeight(float u)
        {
            float bell = 4f * u * (1f - u);
            return Mathf.Lerp(settings.postHeight, settings.handrailHeight, bell);
        }

        /// <summary>Wind-driven offset for a rope point. Each rope uses its own seed so they do not move in lockstep.</summary>
        public Vector3 Flutter(float u, float seed)
        {
            if (WindFlutter <= 0f) return Vector3.zero;
            float envelope = WindFlutter * Mathf.Sin(Mathf.PI * u);
            return new Vector3(
                Noise.Signed1(time * 1.9f + u * 7f, seed) * envelope,
                Noise.Signed1(time * 1.3f + u * 5f, seed + 17f) * envelope * 0.4f,
                0f);
        }
    }
}
