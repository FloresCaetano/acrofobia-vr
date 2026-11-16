using Acrofobia.Stimuli;
using UnityEngine;

namespace Acrofobia.Data
{
    /// <summary>
    /// One step of the exposure hierarchy. Distances are meters along the walk path of the canyon:
    /// 0 is the first bridge anchor and negative values are on the near landing.
    /// </summary>
    [CreateAssetMenu(menuName = "Acrofobia/Level Definition", fileName = "Level")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Min(1)] public int number = 1;
        public string displayName = "Nivel";
        [TextArea] public string description;
        [Tooltip("Short instruction shown to the patient when the level starts.")]
        [TextArea] public string hint;

        [Header("Position on the path (m)")]
        public float spawnDistance;
        public float minDistance;
        public float maxDistance;

        [Header("Initial view (degrees)")]
        public float spawnYaw;
        [Range(-80f, 75f)] public float spawnPitch;

        [Header("Bridge")]
        [Tooltip("Removes one plank in front of the patient (Rothbaum et al., 1995).")]
        public bool missingPlank;
        public float missingPlankDistance;

        [Header("Default stimuli (0-100)")]
        public StimulusValues defaultStimuli;

        public string Title => $"Nivel {number} · {displayName}";

        private void OnValidate()
        {
            if (maxDistance < minDistance) maxDistance = minDistance;
            spawnDistance = Mathf.Clamp(spawnDistance, minDistance, maxDistance);
        }
    }
}
