using UnityEngine;

namespace Acrofobia.Player
{
    /// <summary>
    /// A one-dimensional route the player can walk along. Distances are in meters.
    /// The pose is the walking surface at that distance: forward points along the route
    /// and up includes any tilt of the surface (for example, a swaying bridge).
    /// </summary>
    public interface IWalkPath
    {
        float MinDistance { get; }
        float MaxDistance { get; }
        Pose Evaluate(float distance);
    }
}
