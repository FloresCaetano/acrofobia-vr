using System;

namespace Acrofobia.Environment.Bridge
{
    /// <summary>
    /// Catenary curve y = a·cosh(x/a) between two anchors at the same height,
    /// parameterized by its span and the sag at mid-span.
    /// </summary>
    public readonly struct Catenary
    {
        public readonly float Span;
        public readonly float Sag;
        private readonly double a;

        private Catenary(float span, float sag, double a)
        {
            Span = span;
            Sag = sag;
            this.a = a;
        }

        public static Catenary FromSag(float span, float sag)
        {
            if (span <= 0f) throw new ArgumentOutOfRangeException(nameof(span));
            if (sag <= 1e-4f) return new Catenary(span, 0f, double.PositiveInfinity);

            // sag(a) = a·(cosh(h/a) − 1) decreases monotonically with a, so bisection is robust.
            double half = span * 0.5;
            double low = 1e-3 * span, high = 1e4 * span;
            for (int i = 0; i < 100; i++)
            {
                double mid = 0.5 * (low + high);
                double midSag = mid * (Math.Cosh(half / mid) - 1.0);
                if (midSag > sag) low = mid; else high = mid;
            }
            return new Catenary(span, sag, 0.5 * (low + high));
        }

        /// <summary>Vertical drop below the anchor line at normalized position <paramref name="u"/> (0 and 1 are the anchors).</summary>
        public float Drop(float u)
        {
            if (double.IsPositiveInfinity(a)) return 0f;
            double half = Span * 0.5;
            double x = (u - 0.5) * Span;
            return (float)(a * (Math.Cosh(half / a) - Math.Cosh(x / a)));
        }
    }
}
