using UnityEngine;
using UnityEngine.UI;

namespace Acrofobia.UI
{
    /// <summary>
    /// A face drawn as UI geometry: eyes widen, brows tilt and the mouth bends from a smile to a frown
    /// as <see cref="Fear"/> goes from 0 to 1. Resolution independent and needs no sprites.
    /// </summary>
    public sealed class FaceGraphic : MaskableGraphic
    {
        private const int CircleSegments = 48;

        [SerializeField, Range(0f, 1f)] private float fear;
        [SerializeField] private Color featureColor = new(0.06f, 0.09f, 0.15f, 1f);

        public float Fear
        {
            get => fear;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(fear, value)) return;
                fear = value;
                SetVerticesDirty();
            }
        }

        public Color FaceColor
        {
            get => color;
            set => color = value;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            Vector2 center = rect.center;

            Disc(vh, center, radius, color);

            float eyeRadius = radius * Mathf.Lerp(0.085f, 0.13f, fear);
            Vector2 leftEye = center + new Vector2(-radius * 0.34f, radius * 0.18f);
            Vector2 rightEye = center + new Vector2(radius * 0.34f, radius * 0.18f);
            Disc(vh, leftEye, eyeRadius, featureColor);
            Disc(vh, rightEye, eyeRadius, featureColor);

            // Brows rise toward the middle as fear grows (worried look).
            float tilt = Mathf.Lerp(-0.03f, 0.12f, fear) * radius;
            float browY = radius * 0.42f;
            Line(vh, center + new Vector2(-radius * 0.5f, browY - tilt), center + new Vector2(-radius * 0.18f, browY + tilt), radius * 0.06f, featureColor);
            Line(vh, center + new Vector2(radius * 0.18f, browY + tilt), center + new Vector2(radius * 0.5f, browY - tilt), radius * 0.06f, featureColor);

            float smile = Mathf.Lerp(1f, -1f, fear);
            Vector2 previous = Vector2.zero;
            const int mouthSegments = 16;
            for (int i = 0; i <= mouthSegments; i++)
            {
                float x = Mathf.Lerp(-1f, 1f, i / (float)mouthSegments);
                Vector2 point = center + new Vector2(x * radius * 0.42f, -radius * 0.32f - smile * (1f - x * x) * radius * 0.18f);
                if (i > 0) Line(vh, previous, point, radius * 0.07f, featureColor);
                previous = point;
            }
        }

        private static void Disc(VertexHelper vh, Vector2 center, float radius, Color32 color)
        {
            int start = vh.currentVertCount;
            vh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= CircleSegments; i++)
            {
                float angle = i / (float)CircleSegments * Mathf.PI * 2f;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
            }
            for (int i = 1; i <= CircleSegments; i++) vh.AddTriangle(start, start + i + 1, start + i);
        }

        private static void Line(VertexHelper vh, Vector2 from, Vector2 to, float thickness, Color32 color)
        {
            Vector2 normal = Vector2.Perpendicular((to - from).normalized) * (thickness * 0.5f);
            int start = vh.currentVertCount;
            vh.AddVert(from - normal, color, Vector2.zero);
            vh.AddVert(from + normal, color, Vector2.zero);
            vh.AddVert(to + normal, color, Vector2.zero);
            vh.AddVert(to - normal, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
            // Round caps hide the joints between mouth segments.
            Disc(vh, from, thickness * 0.5f, color);
            Disc(vh, to, thickness * 0.5f, color);
        }
    }
}
