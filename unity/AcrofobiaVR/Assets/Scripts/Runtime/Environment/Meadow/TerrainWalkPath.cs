using Acrofobia.Player;
using UnityEngine;

namespace Acrofobia.Environment.Meadow
{
    /// <summary>Straight path that follows the terrain surface, from a start transform along its forward axis.</summary>
    public sealed class TerrainWalkPath : IWalkPath
    {
        private readonly Terrain terrain;
        private readonly Transform start;
        private readonly float length;

        public TerrainWalkPath(Terrain terrain, Transform start, float length)
        {
            this.terrain = terrain;
            this.start = start;
            this.length = length;
        }

        public float MinDistance => 0f;
        public float MaxDistance => length;

        public Pose Evaluate(float distance)
        {
            Vector3 forward = Vector3.ProjectOnPlane(start.forward, Vector3.up).normalized;
            Vector3 point = start.position + forward * Mathf.Clamp(distance, 0f, length);
            point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
            return new Pose(point, Quaternion.LookRotation(forward, Vector3.up));
        }
    }
}
