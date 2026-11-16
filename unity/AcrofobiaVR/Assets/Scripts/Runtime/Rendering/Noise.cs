using UnityEngine;

namespace Acrofobia.Rendering
{
    /// <summary>Deterministic noise helpers used by procedural meshes, terrain generation and animation.</summary>
    public static class Noise
    {
        private static readonly int[] Permutation = BuildPermutation();

        /// <summary>Signed 2D noise in roughly [-1, 1].</summary>
        public static float Signed2(float x, float y) => Mathf.PerlinNoise(x, y) * 2f - 1f;

        /// <summary>Fractal 2D noise in roughly [-1, 1].</summary>
        public static float Fbm2(float x, float y, int octaves, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f, amplitude = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Signed2(x, y) * amplitude;
                norm += amplitude;
                x = x * lacunarity + 17.31f;
                y = y * lacunarity + 9.73f;
                amplitude *= gain;
            }
            return sum / norm;
        }

        /// <summary>Ridged fractal noise in [0, 1], good for mountain crests.</summary>
        public static float Ridged2(float x, float y, int octaves)
        {
            float sum = 0f, amplitude = 0.5f, weight = 1f;
            for (int i = 0; i < octaves; i++)
            {
                float n = 1f - Mathf.Abs(Signed2(x, y));
                n *= n * weight;
                weight = Mathf.Clamp01(n * 2f);
                sum += n * amplitude;
                x = x * 2.03f + 31.7f;
                y = y * 2.03f + 11.3f;
                amplitude *= 0.5f;
            }
            return Mathf.Clamp01(sum);
        }

        /// <summary>Smooth 1D noise in [-1, 1], used for organic animation.</summary>
        public static float Signed1(float t, float seed = 0f) => Signed2(t, seed * 13.37f + 0.5f);

        /// <summary>Fractal 3D gradient noise in roughly [-1, 1].</summary>
        public static float Fbm3(Vector3 p, int octaves)
        {
            float sum = 0f, amplitude = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Perlin3(p.x, p.y, p.z) * amplitude;
                norm += amplitude;
                p = p * 2f + new Vector3(5.2f, 1.3f, 7.7f);
                amplitude *= 0.5f;
            }
            return sum / norm;
        }

        /// <summary>Classic improved Perlin noise (Ken Perlin, 2002).</summary>
        public static float Perlin3(float x, float y, float z)
        {
            int xi = Mathf.FloorToInt(x) & 255, yi = Mathf.FloorToInt(y) & 255, zi = Mathf.FloorToInt(z) & 255;
            x -= Mathf.Floor(x);
            y -= Mathf.Floor(y);
            z -= Mathf.Floor(z);
            float u = Fade(x), v = Fade(y), w = Fade(z);
            int[] p = Permutation;
            int a = p[xi] + yi, aa = p[a] + zi, ab = p[a + 1] + zi;
            int b = p[xi + 1] + yi, ba = p[b] + zi, bb = p[b + 1] + zi;

            return Mathf.Lerp(
                Mathf.Lerp(Mathf.Lerp(Grad(p[aa], x, y, z), Grad(p[ba], x - 1, y, z), u),
                           Mathf.Lerp(Grad(p[ab], x, y - 1, z), Grad(p[bb], x - 1, y - 1, z), u), v),
                Mathf.Lerp(Mathf.Lerp(Grad(p[aa + 1], x, y, z - 1), Grad(p[ba + 1], x - 1, y, z - 1), u),
                           Mathf.Lerp(Grad(p[ab + 1], x, y - 1, z - 1), Grad(p[bb + 1], x - 1, y - 1, z - 1), u), v),
                w);
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        private static float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        private static int[] BuildPermutation()
        {
            var random = new System.Random(1337);
            var source = new int[256];
            for (int i = 0; i < 256; i++) source[i] = i;
            for (int i = 255; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (source[i], source[j]) = (source[j], source[i]);
            }
            var permutation = new int[512];
            for (int i = 0; i < 512; i++) permutation[i] = source[i & 255];
            return permutation;
        }
    }
}
