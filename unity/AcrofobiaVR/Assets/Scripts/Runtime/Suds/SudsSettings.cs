using UnityEngine;

namespace Acrofobia.Suds
{
    /// <summary>How often the patient is asked for a SUDS rating. The therapist console can override it live.</summary>
    [CreateAssetMenu(menuName = "Acrofobia/SUDS Settings", fileName = "SudsSettings")]
    public sealed class SudsSettings : ScriptableObject
    {
        public const float MinInterval = 10f;
        public const float MaxInterval = 600f;

        [Tooltip("Seconds between automatic questions during exposure. 120 in clinical use; the demo asset uses 30.")]
        [Range(MinInterval, MaxInterval)] public float intervalSeconds = 120f;

        [Tooltip("Value shown the first time the panel opens.")]
        [Range(0, 100)] public int initialValue = 50;

        [Tooltip("Change per wheel notch or arrow key press.")]
        [Range(1, 10)] public int step = 5;
    }
}
