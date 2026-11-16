using System;
using System.Collections;
using System.Linq;
using Acrofobia.Core;
using Acrofobia.Environment;
using Acrofobia.Stimuli;
using Acrofobia.Suds;
using Acrofobia.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Acrofobia.Tests
{
    /// <summary>
    /// End-to-end checks of the Boot flow in play mode: startup in the meadow, level changes with the
    /// canyon loaded additively, the calm button, stimulus interpolation and a SUDS answer.
    /// </summary>
    public sealed class SessionFlowTests
    {
        private const float Timeout = 30f;

        private SessionManager session;

        [UnitySetUp]
        public IEnumerator LoadBoot()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.Boot, LoadSceneMode.Single);
            session = Object.FindAnyObjectByType<SessionManager>();
            Assert.That(session, Is.Not.Null);
            yield return WaitUntil(() => session.CurrentScene == SceneNames.Neutral && !session.IsTransitioning);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            // The App root lives in DontDestroyOnLoad; remove it so every test starts from a clean Boot.
            var app = Object.FindAnyObjectByType<AppRoot>();
            if (app != null) Object.Destroy(app.gameObject);
            yield return null;
            // Answers given by the tests must not be delivered to a real server later.
            System.IO.File.Delete(System.IO.Path.Combine(Application.persistentDataPath, "suds_outbox.json"));
        }

        [UnityTest]
        public IEnumerator Startup_EntersTheNeutralMeadow()
        {
            Assert.That(session.CurrentLevel, Is.Null);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneNames.Neutral));
            yield break;
        }

        [UnityTest]
        public IEnumerator Levels_LoadTheCanyonAndApplyTheirDefaults()
        {
            session.GoToLevel(2);
            yield return WaitForIdle();

            Assert.That(session.CurrentScene, Is.EqualTo(SceneNames.Acrophobia));
            Assert.That(session.CurrentLevel.number, Is.EqualTo(2));
            Assert.That(SceneManager.GetSceneByName(SceneNames.Neutral).isLoaded, Is.False, "the previous environment is unloaded");
            var stimuli = Object.FindAnyObjectByType<StimulusController>();
            Assert.That(stimuli.Target, Is.EqualTo(session.CurrentLevel.defaultStimuli));

            session.GoToLevel(4);
            yield return WaitForIdle();

            var canyon = Object.FindAnyObjectByType<AcrophobiaEnvironment>();
            int hiddenPlanks = canyon.Bridge.GetComponentsInChildren<MeshRenderer>(true).Count(r => r.name.StartsWith("Plank_") && !r.gameObject.activeSelf);
            Assert.That(hiddenPlanks, Is.EqualTo(1), "level 4 removes one plank");
        }

        [UnityTest]
        public IEnumerator Calm_ReturnsToNeutral_EvenWithTheSudsPanelOpen()
        {
            session.GoToLevel(3);
            yield return WaitForIdle();
            var suds = Object.FindAnyObjectByType<SudsManager>();
            suds.Request(SudsTrigger.Local);
            yield return WaitUntil(() => suds.IsAsking);

            session.TriggerCalm(CalmSource.Patient);
            yield return WaitForIdle();

            Assert.That(session.CurrentScene, Is.EqualTo(SceneNames.Neutral));
            Assert.That(suds.IsAsking, Is.False);
            Assert.That(Object.FindAnyObjectByType<StimulusController>().Target, Is.EqualTo(StimulusValues.Minimum));
        }

        [UnityTest]
        public IEnumerator Stimuli_InterpolateToTheirTargetInAboutTwoSeconds()
        {
            var stimuli = Object.FindAnyObjectByType<StimulusController>();
            stimuli.SetTarget(StimulusId.Wind, 90f);
            yield return null;
            Assert.That(stimuli.Current.wind, Is.LessThan(20f), "changes are never instantaneous");

            yield return new WaitForSecondsRealtime(2.2f);
            Assert.That(stimuli.Current.wind, Is.EqualTo(90f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator SudsAnswer_IsReportedWithItsContext()
        {
            session.GoToLevel(1);
            yield return WaitForIdle();

            var suds = Object.FindAnyObjectByType<SudsManager>();
            SudsRecord answer = null;
            suds.Answered += record => answer = record;
            suds.Request(SudsTrigger.Console);
            yield return WaitUntil(() => suds.IsAsking);

            var confirm = Object.FindAnyObjectByType<SudsPanel>().GetComponentsInChildren<Button>().Single(b => b.name.Contains("Confirmar"));
            confirm.onClick.Invoke();

            Assert.That(answer, Is.Not.Null);
            Assert.That(answer.level, Is.EqualTo(1));
            Assert.That(answer.scene, Is.EqualTo(SceneNames.Acrophobia));
            Assert.That(answer.trigger, Is.EqualTo("console"));
            Assert.That(answer.id, Is.Not.Empty);
        }

        private IEnumerator WaitForIdle()
        {
            yield return null;
            yield return WaitUntil(() => !session.IsTransitioning);
        }

        private static IEnumerator WaitUntil(Func<bool> condition)
        {
            float start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > Timeout) Assert.Fail("Timed out waiting for the condition.");
                yield return null;
            }
        }
    }
}
