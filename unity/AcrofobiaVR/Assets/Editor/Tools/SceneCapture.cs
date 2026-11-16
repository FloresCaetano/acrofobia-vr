using System;
using System.IO;
using Acrofobia.Core;
using Acrofobia.Editor.Build;
using Acrofobia.Environment;
using Acrofobia.Environment.Meadow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Acrofobia.Editor.Tools
{
    /// <summary>
    /// Renders reference screenshots of the environments into docs/screenshots without entering play mode.
    /// Works in batch mode as long as Unity runs with a graphics device (no -nographics).
    /// </summary>
    public static class SceneCapture
    {
        private const int Width = 1920;
        private const int Height = 1080;

        private static string OutputFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../../docs/screenshots"));

        [MenuItem("Acrofobia/Capturas de referencia", priority = 20)]
        public static void CaptureAll()
        {
            bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                Directory.CreateDirectory(OutputFolder);
                CaptureCanyon();
                CaptureMeadow();
                Debug.Log($"[SceneCapture] Screenshots written to {OutputFolder}");
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncCompilation;
            }
        }

        /// <summary>Entry point for <c>-executeMethod Acrofobia.Editor.Tools.SceneCapture.CaptureBatch</c>.</summary>
        public static void CaptureBatch()
        {
            try
            {
                CaptureAll();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Rebuilds everything and then captures: one Unity launch instead of two.</summary>
        public static void BuildAndCaptureBatch()
        {
            try
            {
                ProjectBuilder.BuildAll();
                CaptureAll();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void CaptureCanyon()
        {
            EditorSceneManager.OpenScene(ProjectBuilder.ScenePath(SceneNames.Acrophobia), OpenSceneMode.Single);
            var environment = UnityEngine.Object.FindAnyObjectByType<AcrophobiaEnvironment>();
            environment.PreviewInEditor();
            var path = environment.WalkPath;
            float center = environment.Bridge.Settings.span * 0.5f;

            Shot("canyon_shore", FromPath(path.Evaluate(-3f), 0f, -3f));
            Shot("canyon_center", FromPath(path.Evaluate(center - 0.5f), 35f, 8f));
            // Level 4: the missing plank right in front of the feet.
            environment.Bridge.SetMissingPlank(center);
            Shot("canyon_look_down", FromPath(path.Evaluate(center - 0.7f), 10f, 62f));
            environment.Bridge.SetMissingPlank(null);
            // Hovering inside the gorge, beside the bridge.
            float deck = environment.Bridge.transform.position.y;
            Vector3 overview = new(-75f, deck - 12f, -14f);
            Shot("canyon_overview", new Pose(overview, Quaternion.LookRotation(new Vector3(0f, deck - 8f, 4f) - overview)));
        }

        private static void CaptureMeadow()
        {
            EditorSceneManager.OpenScene(ProjectBuilder.ScenePath(SceneNames.Neutral), OpenSceneMode.Single);
            var environment = UnityEngine.Object.FindAnyObjectByType<NeutralEnvironment>();
            var path = environment.WalkPath;

            Shot("meadow_spawn", FromPath(path.Evaluate(environment.DefaultSpawnDistance), 0f, 2f));
            Shot("meadow_pond", FromPath(path.Evaluate(path.MaxDistance), 25f, 12f));
            Vector3 aerial = path.Evaluate(0f).position + new Vector3(-30f, 22f, -18f);
            Shot("meadow_overview", new Pose(aerial, Quaternion.LookRotation(path.Evaluate(path.MaxDistance).position + new Vector3(8f, 0f, 14f) - aerial)));
        }

        private static Pose FromPath(Pose surface, float yaw, float pitch)
        {
            Vector3 position = surface.position + surface.up * 1.65f;
            return new Pose(position, surface.rotation * Quaternion.Euler(pitch, yaw, 0f));
        }

        private static void Shot(string name, Pose pose)
        {
            var cameraObject = new GameObject("Capture Camera") { hideFlags = HideFlags.HideAndDontSave };
            var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                cameraObject.transform.SetPositionAndRotation(pose.position, pose.rotation);
                var camera = cameraObject.AddComponent<Camera>();
                camera.fieldOfView = 70f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 2500f;
                camera.targetTexture = renderTexture;
                var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

                // The first frame warms up shadows and shader variants; keep the second one.
                for (int i = 0; i < 2; i++) Render(camera, renderTexture);

                var previous = RenderTexture.active;
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;

                File.WriteAllBytes(Path.Combine(OutputFolder, $"{name}.jpg"), texture.EncodeToJPG(90));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void Render(Camera camera, RenderTexture target)
        {
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            else
            {
                camera.Render();
            }
        }
    }
}
