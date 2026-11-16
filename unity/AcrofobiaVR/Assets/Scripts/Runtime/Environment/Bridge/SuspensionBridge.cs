using System.Collections.Generic;
using Acrofobia.Rendering;
using UnityEngine;

namespace Acrofobia.Environment.Bridge
{
    /// <summary>
    /// Procedural rope bridge: planks, main cables, hand ropes, suspenders, posts and landings.
    /// The geometry is generated on Awake and re-posed every frame from <see cref="BridgeShape"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SuspensionBridge : MonoBehaviour
    {
        private const int RopePoints = 80;
        private const float SuspenderRadius = 0.012f;

        [SerializeField] private BridgeSettings settings = new();
        [SerializeField] private Material[] plankMaterials;
        [SerializeField] private Material ropeMaterial;
        [SerializeField] private Material postMaterial;
        [SerializeField] private int seed = 7;

        private readonly List<RopeTube> ropes = new();
        private readonly List<RopeSpec> ropeSpecs = new();
        private readonly List<Vector3> ropePoints = new(RopePoints);
        private Transform generatedRoot;
        private Transform[] planks;
        private PlankJitter[] plankJitter;
        private Transform[] suspenders;
        private float[] suspenderU;
        private int missingPlank = -1;

        public BridgeShape Shape { get; private set; }
        public BridgeSettings Settings => settings;

        /// <summary>World height of the walking surface at mid-span when the bridge is at rest.</summary>
        public float RestDeckCenterHeight => transform.position.y - settings.deckSag;

        private void Awake() => Rebuild();

        private void Update()
        {
            Shape.Advance(Time.deltaTime);
            UpdatePose();
        }

        /// <summary>Hides the plank closest to <paramref name="distance"/> (meters from the first anchor), or restores all planks with null.</summary>
        public void SetMissingPlank(float? distance)
        {
            if (missingPlank >= 0) planks[missingPlank].gameObject.SetActive(true);
            missingPlank = distance.HasValue
                ? Mathf.Clamp(Mathf.RoundToInt(distance.Value / settings.PlankPitch - 0.5f), 0, planks.Length - 1)
                : -1;
            if (missingPlank >= 0) planks[missingPlank].gameObject.SetActive(false);
        }

        /// <summary>World pose of the walking surface. Distances below 0 or above the span fall on the landings.</summary>
        public Pose GetSurfacePose(float distance)
        {
            Vector3 localPosition;
            Quaternion localRotation;
            if (distance <= 0f || distance >= settings.span)
            {
                localPosition = new Vector3(0f, 0f, distance - settings.span * 0.5f);
                localRotation = Quaternion.identity;
            }
            else
            {
                float u = distance / settings.span;
                localPosition = Shape.DeckPoint(u);
                localRotation = Shape.DeckRotation(u);
            }
            return new Pose(transform.TransformPoint(localPosition), transform.rotation * localRotation);
        }

        public void Rebuild()
        {
            Shape = new BridgeShape(settings);
            if (generatedRoot != null) DestroySafely(generatedRoot.gameObject);

            generatedRoot = new GameObject("Generated").transform;
            generatedRoot.gameObject.hideFlags = HideFlags.DontSave;
            generatedRoot.SetParent(transform, false);

            var random = new System.Random(seed);
            BuildPlanks(random);
            BuildRopes();
            BuildSuspenders();
            BuildPostsAndLandings(random);
            missingPlank = -1;
            UpdatePose();
        }

        private void BuildPlanks(System.Random random)
        {
            var mesh = MeshFactory.Box(
                new Vector3(settings.deckWidth + settings.plankOverhang * 2f, settings.plankThickness, settings.plankDepth),
                2f, "Plank");
            var parent = CreateGroup("Planks");

            planks = new Transform[settings.PlankCount];
            plankJitter = new PlankJitter[planks.Length];
            for (int i = 0; i < planks.Length; i++)
            {
                planks[i] = CreateMeshObject($"Plank_{i:000}", parent, mesh, PickPlankMaterial(random));
                plankJitter[i] = PlankJitter.Random(random);
            }
        }

        private void BuildRopes()
        {
            float hw = settings.HalfWidth;
            float cableX = hw + 0.04f;
            float stringerX = hw - 0.12f;
            float stringerY = -settings.plankThickness - settings.ropeRadius;
            ropeSpecs.Clear();
            ropes.Clear();

            foreach (float side in new[] { -1f, 1f })
            {
                ropeSpecs.Add(new RopeSpec(side * cableX, null, settings.cableRadius, side + 1f, 1f));
                ropeSpecs.Add(new RopeSpec(side * cableX, settings.midRailHeight, settings.ropeRadius, side + 5f, 0.7f));
                ropeSpecs.Add(new RopeSpec(side * stringerX, stringerY, settings.cableRadius, side + 9f, 0f));
            }

            var parent = CreateGroup("Ropes");
            foreach (var spec in ropeSpecs)
            {
                ropes.Add(new RopeTube(parent, "Rope", ropeMaterial, RopePoints, spec.Radius));
            }
        }

        private void BuildSuspenders()
        {
            var mesh = MeshFactory.Cylinder(5, 0.5f, "Suspender");
            var parent = CreateGroup("Suspenders");
            int perSide = Mathf.Max(2, Mathf.FloorToInt(settings.span / settings.suspenderSpacing) - 1);

            suspenders = new Transform[perSide * 2];
            suspenderU = new float[perSide];
            for (int i = 0; i < perSide; i++)
            {
                suspenderU[i] = (i + 1f) / (perSide + 1f);
                suspenders[i * 2] = CreateMeshObject("Suspender_L", parent, mesh, ropeMaterial);
                suspenders[i * 2 + 1] = CreateMeshObject("Suspender_R", parent, mesh, ropeMaterial);
            }
        }

        private void BuildPostsAndLandings(System.Random random)
        {
            var parent = CreateGroup("Anchors");
            float hw = settings.HalfWidth;
            float halfSpan = settings.span * 0.5f;
            float postX = hw + 0.13f;
            const float postBury = 1.2f;
            var postMesh = MeshFactory.Box(new Vector3(0.18f, settings.postHeight + 0.15f + postBury, 0.18f), 1f, "Post");
            var beamMesh = MeshFactory.Box(new Vector3(0.14f, 0.14f, settings.landingLength), 1f, "LandingBeam");
            var stakeMesh = MeshFactory.Box(new Vector3(0.14f, 0.9f, 0.14f), 1f, "Stake");
            var plankMesh = MeshFactory.Box(
                new Vector3(settings.deckWidth + settings.plankOverhang * 2f + 0.3f, settings.plankThickness, settings.plankDepth),
                2f, "LandingPlank");

            foreach (float end in new[] { -1f, 1f })
            {
                float anchorZ = end * halfSpan;
                foreach (float side in new[] { -1f, 1f })
                {
                    float postCenterY = (settings.postHeight + 0.15f - postBury) * 0.5f;
                    CreateMeshObject("Post", parent, postMesh, postMaterial).localPosition = new Vector3(side * postX, postCenterY, anchorZ);

                    var stakePosition = new Vector3(side * (hw + 0.6f), -0.25f, anchorZ + end * settings.landingLength * 0.8f);
                    CreateMeshObject("Stake", parent, stakeMesh, postMaterial).localPosition = stakePosition;
                    var tie = new RopeTube(parent, "AnchorRope", ropeMaterial, 12, settings.cableRadius);
                    tie.SetPoints(SaggingLine(new Vector3(side * (hw + 0.06f), settings.postHeight, anchorZ), stakePosition + Vector3.up * 0.4f, 0.08f, 12));

                    var beam = CreateMeshObject("LandingBeam", parent, beamMesh, postMaterial);
                    beam.localPosition = new Vector3(side * (hw - 0.1f), -settings.plankThickness - 0.07f, anchorZ + end * settings.landingLength * 0.5f);
                }

                int landingPlanks = Mathf.FloorToInt(settings.landingLength / settings.PlankPitch);
                for (int i = 0; i < landingPlanks; i++)
                {
                    float z = anchorZ + end * (i + 0.5f) * settings.PlankPitch;
                    var plank = CreateMeshObject("LandingPlank", parent, plankMesh, PickPlankMaterial(random));
                    plank.localPosition = new Vector3(0f, -settings.plankThickness * 0.5f, z);
                    plank.localRotation = PlankJitter.Random(random).Rotation;
                }
            }
        }

        private void UpdatePose()
        {
            float halfThickness = settings.plankThickness * 0.5f;
            for (int i = 0; i < planks.Length; i++)
            {
                float u = (i + 0.5f) * settings.PlankPitch / settings.span;
                var jitter = plankJitter[i];
                planks[i].SetLocalPositionAndRotation(
                    Shape.DeckAttached(u, new Vector3(jitter.Offset.x, -halfThickness + jitter.Offset.y, jitter.Offset.z)),
                    Shape.DeckRotation(u) * jitter.Rotation);
            }

            for (int r = 0; r < ropes.Count; r++)
            {
                ropePoints.Clear();
                for (int k = 0; k < RopePoints; k++) ropePoints.Add(RopePoint(ropeSpecs[r], k / (RopePoints - 1f)));
                ropes[r].SetPoints(ropePoints);
            }

            for (int i = 0; i < suspenderU.Length; i++)
            {
                float u = suspenderU[i];
                PoseSuspender(suspenders[i * 2], ropeSpecs[0], u);
                PoseSuspender(suspenders[i * 2 + 1], ropeSpecs[3], u);
            }
        }

        private Vector3 RopePoint(in RopeSpec spec, float u)
        {
            float height = spec.Height ?? Shape.CableHeight(u);
            return Shape.DeckAttached(u, new Vector3(spec.X, height, 0f)) + Shape.Flutter(u, spec.Seed) * spec.FlutterScale;
        }

        private void PoseSuspender(Transform suspender, in RopeSpec cable, float u)
        {
            Vector3 top = RopePoint(cable, u);
            Vector3 bottom = Shape.DeckAttached(u, new Vector3(cable.X * 0.97f, 0f, 0f));
            Vector3 span = top - bottom;
            suspender.SetLocalPositionAndRotation(bottom, Quaternion.FromToRotation(Vector3.up, span));
            suspender.localScale = new Vector3(SuspenderRadius * 2f, span.magnitude, SuspenderRadius * 2f);
        }

        private Material PickPlankMaterial(System.Random random) => plankMaterials[random.Next(plankMaterials.Length)];

        private Transform CreateGroup(string groupName)
        {
            var group = new GameObject(groupName).transform;
            group.SetParent(generatedRoot, false);
            return group;
        }

        private static Transform CreateMeshObject(string objectName, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go.transform;
        }

        private static List<Vector3> SaggingLine(Vector3 from, Vector3 to, float sag, int points)
        {
            var line = new List<Vector3>(points);
            for (int i = 0; i < points; i++)
            {
                float t = i / (points - 1f);
                line.Add(Vector3.Lerp(from, to, t) + Vector3.down * (sag * 4f * t * (1f - t)));
            }
            return line;
        }

        private static void DestroySafely(Object target)
        {
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        private readonly struct RopeSpec
        {
            public readonly float X;
            public readonly float? Height;
            public readonly float Radius;
            public readonly float Seed;
            public readonly float FlutterScale;

            public RopeSpec(float x, float? height, float radius, float seed, float flutterScale)
            {
                X = x;
                Height = height;
                Radius = radius;
                Seed = seed;
                FlutterScale = flutterScale;
            }
        }

        private readonly struct PlankJitter
        {
            public readonly Vector3 Offset;
            public readonly Quaternion Rotation;

            private PlankJitter(Vector3 offset, Quaternion rotation)
            {
                Offset = offset;
                Rotation = rotation;
            }

            public static PlankJitter Random(System.Random random)
            {
                float Range(float amplitude) => ((float)random.NextDouble() * 2f - 1f) * amplitude;
                return new PlankJitter(
                    new Vector3(Range(0.025f), Range(0.004f), Range(0.01f)),
                    Quaternion.Euler(Range(0.8f), Range(1.6f), Range(0.6f)));
            }
        }
    }
}
