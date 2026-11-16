using Acrofobia.Player;
using UnityEngine;

namespace Acrofobia.Environment.Meadow
{
    /// <summary>
    /// Root of the safe place: an open meadow with no sense of height. It deliberately ignores the
    /// stimulus values, so returning here always feels the same.
    /// </summary>
    public sealed class NeutralEnvironment : EnvironmentRoot
    {
        [SerializeField] private Terrain terrain;
        [SerializeField] private Transform pathStart;
        [SerializeField] private float pathLength = 30f;
        [SerializeField] private float defaultSpawnDistance = 12f;

        private IWalkPath walkPath;

        public override IWalkPath WalkPath => walkPath ??= new TerrainWalkPath(terrain, pathStart, pathLength);
        public override float DefaultSpawnDistance => defaultSpawnDistance;
    }
}
