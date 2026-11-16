using System.Collections.Generic;
using UnityEngine;

namespace Acrofobia.Rendering
{
    /// <summary>Procedural primitives with UVs sized in meters, so tiled textures keep a constant scale.</summary>
    public static class MeshFactory
    {
        /// <summary>Box centered on the origin. UVs run along the longest horizontal axis (wood grain).</summary>
        public static Mesh Box(Vector3 size, float uvMetersPerTile = 1f, string name = "Box")
        {
            var vertices = new List<Vector3>(24);
            var normals = new List<Vector3>(24);
            var uvs = new List<Vector2>(24);
            var triangles = new List<int>(36);
            Vector3 half = size * 0.5f;
            bool grainAlongZ = size.z >= size.x;

            void Face(Vector3 normal, Vector3 uAxis, Vector3 vAxis, float uSize, float vSize)
            {
                int start = vertices.Count;
                Vector3 center = Vector3.Scale(normal, half);
                Vector3 u = uAxis * (uSize * 0.5f);
                Vector3 v = vAxis * (vSize * 0.5f);
                vertices.Add(center - u - v);
                vertices.Add(center - u + v);
                vertices.Add(center + u + v);
                vertices.Add(center + u - v);
                for (int i = 0; i < 4; i++) normals.Add(normal);
                float su = uSize / uvMetersPerTile;
                float sv = vSize / uvMetersPerTile;
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, sv));
                uvs.Add(new Vector2(su, sv));
                uvs.Add(new Vector2(su, 0f));
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            // u always follows the grain axis so the wood texture is not rotated between faces.
            Vector3 grain = grainAlongZ ? Vector3.forward : Vector3.right;
            Vector3 across = grainAlongZ ? Vector3.right : Vector3.forward;
            float grainSize = grainAlongZ ? size.z : size.x;
            float acrossSize = grainAlongZ ? size.x : size.z;

            Face(Vector3.up, grain, Vector3.Cross(grain, Vector3.up) * -1f, grainSize, acrossSize);
            Face(Vector3.down, grain, Vector3.Cross(grain, Vector3.up), grainSize, acrossSize);
            Face(across, grain, Vector3.up * (grainAlongZ ? -1f : 1f), grainSize, size.y);
            Face(-across, grain, Vector3.up * (grainAlongZ ? 1f : -1f), grainSize, size.y);
            Face(grain, across, Vector3.up * (grainAlongZ ? 1f : -1f), acrossSize, size.y);
            Face(-grain, across, Vector3.up * (grainAlongZ ? -1f : 1f), acrossSize, size.y);

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            FixWinding(mesh);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Unit-height cylinder along +Y from y = 0 to y = 1, used for straight rope segments and posts.</summary>
        public static Mesh Cylinder(int sides = 6, float radius = 0.5f, string name = "Cylinder")
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            for (int i = 0; i <= sides; i++)
            {
                float angle = i / (float)sides * Mathf.PI * 2f;
                var normal = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices.Add(normal * radius);
                vertices.Add(normal * radius + Vector3.up);
                normals.Add(normal);
                normals.Add(normal);
                uvs.Add(new Vector2(i / (float)sides, 0f));
                uvs.Add(new Vector2(i / (float)sides, 1f));
            }

            for (int i = 0; i < sides; i++)
            {
                int a = i * 2;
                triangles.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 });
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Noise-displaced icosphere that reads as a boulder. UVs use a box projection.</summary>
        public static Mesh Rock(int seed, float radius, Vector3 squash, int subdivisions = 2, float roughness = 0.35f, float uvMetersPerTile = 2f)
        {
            Icosphere(subdivisions, out var directions, out var triangles);
            var random = new System.Random(seed);
            var offset = new Vector3((float)random.NextDouble() * 100f, (float)random.NextDouble() * 100f, (float)random.NextDouble() * 100f);

            var vertices = new Vector3[directions.Count];
            for (int i = 0; i < directions.Count; i++)
            {
                Vector3 d = directions[i];
                Vector3 p = d * 1.7f + offset;
                float noise = Noise.Fbm3(p, 3) * roughness + Noise.Fbm3(p * 3.1f, 2) * roughness * 0.35f;
                Vector3 position = d * radius * (1f + noise);
                vertices[i] = Vector3.Scale(position, squash);
            }

            // Flat-ish base so rocks sit on the ground instead of floating on a point.
            for (int i = 0; i < vertices.Length; i++)
            {
                float floor = -radius * squash.y * 0.55f;
                if (vertices[i].y < floor) vertices[i].y = Mathf.Lerp(vertices[i].y, floor, 0.85f);
            }

            var mesh = new Mesh { name = $"Rock_{seed}" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.SetUVs(0, BoxProjectUvs(vertices, mesh.normals, uvMetersPerTile));
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Vector2[] BoxProjectUvs(IReadOnlyList<Vector3> vertices, IReadOnlyList<Vector3> normals, float metersPerTile)
        {
            var uvs = new Vector2[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 n = normals[i];
                Vector3 p = vertices[i] / metersPerTile;
                float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
                uvs[i] = ay >= ax && ay >= az ? new Vector2(p.x, p.z)
                       : ax >= az ? new Vector2(p.z, p.y)
                       : new Vector2(p.x, p.y);
            }
            return uvs;
        }

        private static void Icosphere(int subdivisions, out List<Vector3> vertices, out List<int> triangles)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            vertices = new List<Vector3>
            {
                new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0),
                new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t),
                new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1),
            };
            for (int i = 0; i < vertices.Count; i++) vertices[i] = vertices[i].normalized;

            triangles = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };

            var verts = vertices;
            var midpoints = new Dictionary<long, int>();
            int Midpoint(int a, int b)
            {
                long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
                if (midpoints.TryGetValue(key, out int index)) return index;
                verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
                midpoints[key] = verts.Count - 1;
                return verts.Count - 1;
            }

            for (int s = 0; s < subdivisions; s++)
            {
                var next = new List<int>(triangles.Count * 4);
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    int ab = Midpoint(a, b), bc = Midpoint(b, c), ca = Midpoint(c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                triangles = next;
            }
        }

        /// <summary>Makes every triangle wind clockwise when seen from the side its normal points to (Unity's front face).</summary>
        private static void FixWinding(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                Vector3 faceNormal = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(faceNormal, normals[triangles[i]]) < 0f)
                {
                    (triangles[i + 1], triangles[i + 2]) = (triangles[i + 2], triangles[i + 1]);
                }
            }
            mesh.triangles = triangles;
        }
    }
}
