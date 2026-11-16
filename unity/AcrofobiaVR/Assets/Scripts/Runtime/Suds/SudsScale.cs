using UnityEngine;

namespace Acrofobia.Suds
{
    /// <summary>
    /// Presentation of a SUDS value: a word, a color and a facial expression, so the scale never
    /// depends on color alone.
    /// </summary>
    public static class SudsScale
    {
        private static readonly Color Calm = new(0.169f, 0.702f, 0.659f);
        private static readonly Color Uneasy = new(0.949f, 0.663f, 0.231f);
        private static readonly Color Panic = new(0.910f, 0.353f, 0.298f);

        public static int Clamp(int value) => Mathf.Clamp(value, 0, 100);

        public static string Label(int value) => Clamp(value) switch
        {
            <= 10 => "Totalmente tranquilo",
            <= 30 => "Algo inquieto",
            <= 50 => "Ansioso",
            <= 70 => "Muy ansioso",
            <= 90 => "Con mucho miedo",
            _ => "El peor miedo",
        };

        public static Color Color(int value)
        {
            float t = Clamp(value) / 100f;
            return t < 0.5f ? UnityEngine.Color.Lerp(Calm, Uneasy, t * 2f) : UnityEngine.Color.Lerp(Uneasy, Panic, (t - 0.5f) * 2f);
        }

        /// <summary>Mouth curvature from +1 (smile) to -1 (frown).</summary>
        public static float Smile(int value) => Mathf.Lerp(1f, -1f, Clamp(value) / 100f);
    }
}
