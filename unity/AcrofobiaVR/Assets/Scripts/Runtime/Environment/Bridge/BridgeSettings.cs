using System;
using UnityEngine;

namespace Acrofobia.Environment.Bridge
{
    /// <summary>Dimensions of the procedural suspension bridge, in meters.</summary>
    [Serializable]
    public sealed class BridgeSettings
    {
        [Header("Deck")]
        public float span = 90f;
        public float deckSag = 2.6f;
        public float deckWidth = 1.4f;
        public float plankDepth = 0.2f;
        public float plankGap = 0.07f;
        public float plankThickness = 0.05f;
        public float plankOverhang = 0.08f;

        [Header("Ropes")]
        public float postHeight = 1.55f;
        public float handrailHeight = 1.05f;
        public float midRailHeight = 0.55f;
        public float suspenderSpacing = 1.2f;
        public float cableRadius = 0.022f;
        public float ropeRadius = 0.014f;

        [Header("Landings")]
        public float landingLength = 6f;

        [Header("Motion")]
        [Tooltip("Roll of the deck, in degrees per meter of lateral displacement.")]
        public float rollPerMeter = 7f;

        public float PlankPitch => plankDepth + plankGap;
        public int PlankCount => Mathf.FloorToInt(span / PlankPitch);
        public float HalfWidth => deckWidth * 0.5f;
    }
}
