using UnityEngine;

namespace Acrofobia.Stimuli
{
    /// <summary>
    /// Runtime channel shared through an asset: <see cref="StimulusController"/> writes the values,
    /// environment scenes read them. This keeps scenes free of references to the Boot managers.
    /// </summary>
    [CreateAssetMenu(menuName = "Acrofobia/Stimulus State", fileName = "StimulusState")]
    public sealed class StimulusState : ScriptableObject
    {
        [SerializeField] private StimulusSettings settings;

        [Tooltip("Values used when an environment runs without the Boot scene (edit mode, screenshots).")]
        [SerializeField] private StimulusValues editorPreview = new(60f, 30f, 30f, 15f, 70f);

        public StimulusSettings Settings => settings;

        /// <summary>Interpolated values applied this frame.</summary>
        public StimulusValues Current { get; private set; }

        /// <summary>Values the interpolation is heading to.</summary>
        public StimulusValues Target { get; private set; }

        private void OnEnable() => Release();

        public void Publish(StimulusValues current, StimulusValues target)
        {
            Current = current;
            Target = target;
        }

        /// <summary>Returns to the preview values. Assets keep runtime state between play sessions in the editor.</summary>
        public void Release()
        {
            Current = editorPreview;
            Target = editorPreview;
        }
    }
}
