using Acrofobia.Player;
using UnityEngine;

namespace Acrofobia.Environment.Bridge
{
    /// <summary>Walk path over the bridge: distance 0 is the first anchor, negative values are on the near landing.</summary>
    public sealed class BridgeWalkPath : IWalkPath
    {
        private const float LandingMargin = 0.4f;
        private readonly SuspensionBridge bridge;

        public BridgeWalkPath(SuspensionBridge bridge) => this.bridge = bridge;

        public float MinDistance => -bridge.Settings.landingLength + LandingMargin;
        public float MaxDistance => bridge.Settings.span + bridge.Settings.landingLength - LandingMargin;

        public Pose Evaluate(float distance) => bridge.GetSurfacePose(distance);
    }
}
