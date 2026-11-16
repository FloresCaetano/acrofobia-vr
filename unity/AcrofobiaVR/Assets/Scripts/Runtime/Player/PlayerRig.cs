using Acrofobia.Controls;
using UnityEngine;

namespace Acrofobia.Player
{
    /// <summary>
    /// First-person rig that rides on an <see cref="IWalkPath"/>. Movement is restricted to a
    /// distance range set by the current level, slow and without head bob, following VR comfort rules.
    /// </summary>
    public sealed class PlayerRig : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float eyeHeight = 1.65f;
        [SerializeField] private float walkSpeed = 0.8f;
        [SerializeField] private float acceleration = 2.5f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 75f;

        [Tooltip("When disabled, the camera follows the bridge position but never its roll.")]
        [SerializeField] private bool cameraSwayEnabled = true;

        private IPlayerInputSource input;
        private IWalkPath path;
        private float distance;
        private float speed;
        private float minDistance;
        private float maxDistance;
        private float yaw;
        private float pitch;

        public bool MovementEnabled { get; set; } = true;
        public bool LookEnabled { get; set; } = true;
        public float Distance => distance;
        public Transform CameraPivot => cameraPivot;

        public bool CameraSwayEnabled
        {
            get => cameraSwayEnabled;
            set => cameraSwayEnabled = value;
        }

        private void Awake()
        {
            input = GetComponentInParent<IPlayerInputSource>();
        }

        /// <summary>Places the rig on a path. The allowed range is clamped to the path extents.</summary>
        public void Bind(IWalkPath walkPath, float startDistance, float rangeMin, float rangeMax, float startYaw = 0f, float startPitch = 0f)
        {
            path = walkPath;
            SetAllowedRange(rangeMin, rangeMax);
            distance = Mathf.Clamp(startDistance, minDistance, maxDistance);
            speed = 0f;
            yaw = startYaw;
            pitch = startPitch;
            ApplyPose();
        }

        public void Unbind() => path = null;

        public void SetAllowedRange(float rangeMin, float rangeMax)
        {
            float pathMin = path?.MinDistance ?? float.MinValue;
            float pathMax = path?.MaxDistance ?? float.MaxValue;
            minDistance = Mathf.Clamp(Mathf.Min(rangeMin, rangeMax), pathMin, pathMax);
            maxDistance = Mathf.Clamp(Mathf.Max(rangeMin, rangeMax), pathMin, pathMax);
        }

        private void Update()
        {
            if (path == null || input == null) return;

            if (LookEnabled && !input.DrivesHeadPose)
            {
                Vector2 look = input.LookDelta;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            }

            float targetSpeed = MovementEnabled ? input.Move * walkSpeed : 0f;
            speed = Mathf.MoveTowards(speed, targetSpeed, acceleration * Time.deltaTime);
            distance += speed * Time.deltaTime;

            if (distance <= minDistance || distance >= maxDistance)
            {
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
                speed = 0f;
            }
        }

        // LateUpdate so the rig samples the path after the environment animated it this frame.
        private void LateUpdate()
        {
            if (path != null) ApplyPose();
        }

        private void ApplyPose()
        {
            Pose surface = path.Evaluate(distance);
            Vector3 up = cameraSwayEnabled ? surface.up : Vector3.up;
            Vector3 forward = Vector3.ProjectOnPlane(surface.forward, up).normalized;

            transform.SetPositionAndRotation(surface.position, Quaternion.LookRotation(forward, up));
            cameraPivot.localPosition = new Vector3(0f, eyeHeight, 0f);

            if (input == null || !input.DrivesHeadPose)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            }
        }
    }
}
