using System.Collections;
using System.IO;
using System.Reflection;
using Acrofobia.Core;
using Acrofobia.Suds;
using Acrofobia.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Acrofobia.Tests
{
    /// <summary>
    /// Renders the running app with its UI into docs/screenshots, to review layout and legibility from
    /// the command line. Explicit: run it with -testFilter Acrofobia.Tests.UiCaptureTests.
    /// </summary>
    [Explicit("Writes screenshots; run on demand.")]
    public sealed class UiCaptureTests
    {
        private const int Width = 1920;
        private const int Height = 1080;

        [UnityTest]
        public IEnumerator CaptureMainScreens()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Boot, LoadSceneMode.Single);
            var session = Object.FindAnyObjectByType<SessionManager>();
            yield return WaitIdle(session);
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture("ui_intro");

            Press<IntroView>("Advance");
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Capture("ui_tutorial");

            session.GoToLevel(3);
            yield return WaitIdle(session);
            yield return new WaitForSecondsRealtime(1.5f);
            yield return Capture("ui_level_hud");

            Press<OperatorPanel>("Toggle");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture("ui_operator_panel");
            Press<OperatorPanel>("Toggle");

            Object.FindAnyObjectByType<SudsManager>().Request(SudsTrigger.Local);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture("ui_suds");

            session.TriggerCalm(CalmSource.Patient);
            yield return WaitIdle(session);
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Capture("ui_calm");

            Object.Destroy(Object.FindAnyObjectByType<AppRoot>().gameObject);
            File.Delete(Path.Combine(Application.persistentDataPath, "suds_outbox.json"));
        }

        private static void Press<T>(string method) where T : Object
        {
            var target = Object.FindAnyObjectByType<T>();
            typeof(T).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(target, null);
        }

        private static IEnumerator WaitIdle(SessionManager session)
        {
            yield return null;
            while (session.IsTransitioning) yield return null;
        }

        /// <summary>Temporarily draws every overlay canvas through the main camera so it lands in the render texture.</summary>
        private static IEnumerator Capture(string name)
        {
            // WaitForEndOfFrame never fires in batch mode; a plain frame is enough because we render manually.
            yield return null;
            var camera = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            foreach (var canvas in canvases)
            {
                if (!canvas.isRootCanvas) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.05f;
            }

            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            camera.Render();

            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            foreach (var canvas in canvases)
            {
                if (canvas.isRootCanvas) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../docs/screenshots"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, $"{name}.jpg"), texture.EncodeToJPG(90));
            Object.Destroy(texture);
            target.Release();
        }
    }
}
