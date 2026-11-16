using System;
using UnityEngine;

namespace Acrofobia.Controls
{
    /// <summary>
    /// Abstraction over the patient's input device. The mouse and keyboard implementation is the
    /// only one for now; an XR implementation (head tracking + controllers) will plug in here
    /// without touching the player rig, the SUDS panel or the session flow.
    /// </summary>
    public interface IPlayerInputSource
    {
        /// <summary>True when the head pose comes from tracking (XR), so the rig must not apply <see cref="LookDelta"/>.</summary>
        bool DrivesHeadPose { get; }

        /// <summary>Look rotation requested this frame, in degrees (x = yaw, y = pitch).</summary>
        Vector2 LookDelta { get; }

        /// <summary>Forward/backward intent in the range [-1, 1].</summary>
        float Move { get; }

        /// <summary>Raised once per press of the calm button.</summary>
        event Action CalmPressed;

        /// <summary>Raised once per press of the pause button.</summary>
        event Action PausePressed;

        /// <summary>Raised once per confirmation (Enter on keyboard, trigger on XR).</summary>
        event Action ConfirmPressed;

        /// <summary>Raised with +1 / -1 for each discrete step on a value selector (wheel notch, arrow key).</summary>
        event Action<int> ValueStepped;
    }
}
