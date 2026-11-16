using System.Collections.Generic;
using UnityEngine;

namespace Acrofobia.Environment.Bridge
{
    /// <summary>A tube mesh that follows a polyline and can be re-shaped every frame without allocations.</summary>
    public sealed class RopeTube
    {
        private const int Sides = 6;

        private readonly Mesh mesh;
        private readonly float radius;
        private readonly Vector3[] vertices;
        private readonly Vector3[] normals;
        private readonly int pointCount;

        public RopeTube(Transform parent, string name, Material material, int pointCount, float radius)
        {
            this.pointCount = pointCount;
            this.radius = radius;
            vertices = new Vector3[pointCount * (Sides + 1)];
            normals = new Vector3[vertices.Length];

            mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = BuildUvs(pointCount);
            mesh.triangles = BuildTriangles(pointCount);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>Rebuilds the tube along <paramref name="points"/>, given in the parent's local space.</summary>
        public void SetPoints(IReadOnlyList<Vector3> points)
        {
            Vector3 previousNormal = Vector3.up;
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 tangent = (points[Mathf.Min(i + 1, pointCount - 1)] - points[Mathf.Max(i - 1, 0)]).normalized;
                // Parallel transport keeps the ring orientation stable along the rope (no twisting).
                Vector3 normal = Vector3.ProjectOnPlane(previousNormal, tangent).normalized;
                if (normal.sqrMagnitude < 1e-6f) normal = Vector3.ProjectOnPlane(Vector3.right, tangent).normalized;
                Vector3 binormal = Vector3.Cross(tangent, normal);
                previousNormal = normal;

                for (int s = 0; s <= Sides; s++)
                {
                    float angle = s / (float)Sides * Mathf.PI * 2f;
                    Vector3 direction = normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle);
                    int index = i * (Sides + 1) + s;
                    vertices[index] = points[i] + direction * radius;
                    normals[index] = direction;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.RecalculateBounds();
        }

        private static Vector2[] BuildUvs(int pointCount)
        {
            var uvs = new Vector2[pointCount * (Sides + 1)];
            for (int i = 0; i < pointCount; i++)
            {
                for (int s = 0; s <= Sides; s++) uvs[i * (Sides + 1) + s] = new Vector2(i * 0.5f, s / (float)Sides);
            }
            return uvs;
        }

        private static int[] BuildTriangles(int pointCount)
        {
            var triangles = new int[(pointCount - 1) * Sides * 6];
            int t = 0;
            for (int i = 0; i < pointCount - 1; i++)
            {
                for (int s = 0; s < Sides; s++)
                {
                    int a = i * (Sides + 1) + s;
                    int b = a + Sides + 1;
                    triangles[t++] = a;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = b + 1;
                    triangles[t++] = b;
                }
            }
            return triangles;
        }
    }
}
