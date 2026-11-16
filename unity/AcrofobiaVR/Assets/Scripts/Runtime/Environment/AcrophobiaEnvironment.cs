using Acrofobia.Data;
using Acrofobia.Environment.Audio;
using Acrofobia.Environment.Bridge;
using Acrofobia.Environment.Canyon;
using Acrofobia.Player;
using Acrofobia.Stimuli;
using UnityEngine;

namespace Acrofobia.Environment
{
    /// <summary>
    /// Root of the canyon scene. Translates the five stimulus intensities into the bridge motion,
    /// canyon floor depth, wind, fog and ambience volume, every frame.
    /// </summary>
    public sealed class AcrophobiaEnvironment : EnvironmentRoot
    {
        [SerializeField] private StimulusState stimuli;
        [SerializeField] private SuspensionBridge bridge;
        [SerializeField] private CanyonFloor canyonFloor;
        [SerializeField] private WindParticles windParticles;
        [SerializeField] private ProceduralWindAudio windAudio;
        [SerializeField] private float defaultSpawnDistance = -3f;

        private IWalkPath walkPath;

        public override IWalkPath WalkPath => walkPath ??= new BridgeWalkPath(bridge);
        public override float DefaultSpawnDistance => defaultSpawnDistance;
        public SuspensionBridge Bridge => bridge;

        private void Start()
        {
            canyonFloor.SetReferenceHeight(bridge.RestDeckCenterHeight);
            ApplyStimuli();
        }

        private void Update() => ApplyStimuli();

        /// <summary>Opens the level's gap in the deck (a nod to Rothbaum et al., 1995) or closes it.</summary>
        public override void ApplyLevel(LevelDefinition level)
        {
            bridge.SetMissingPlank(level != null && level.missingPlank ? level.missingPlankDistance : null);
        }

#if UNITY_EDITOR
        /// <summary>Builds the procedural parts and applies the preview stimuli without entering play mode (screenshots, scene view).</summary>
        public void PreviewInEditor()
        {
            bridge.Rebuild();
            Start();
        }
#endif

        private void ApplyStimuli()
        {
            StimulusSettings map = stimuli.Settings;
            StimulusValues values = stimuli.Current;

            canyonFloor.SetDepth(map.floorDepth.Evaluate(values.height));

            BridgeShape shape = bridge.Shape;
            shape.SwayAmplitude = map.swayAmplitude.Evaluate(values.sway);
            shape.SwayFrequency = map.swayFrequency.Evaluate(values.sway);
            shape.WindFlutter = map.ropeFlutter.Evaluate(values.wind);

            windParticles.SetRate(map.windParticleRate.Evaluate(values.wind));
            windAudio.Intensity = values.wind / StimulusValues.Max;
            windAudio.Volume = map.windVolume.Evaluate(values.wind) * map.ambienceVolume.Evaluate(values.sound);

            RenderSettings.fogDensity = map.fogDensity.Evaluate(values.fog);
        }
    }
}
