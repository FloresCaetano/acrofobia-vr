using System;
using System.Collections;
using Acrofobia.Data;
using Acrofobia.Environment;
using Acrofobia.Player;
using Acrofobia.Stimuli;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Acrofobia.Core
{
    public enum CalmSource
    {
        Patient,
        Console,
        Operator,
    }

    /// <summary>
    /// Orchestrates the exposure flow: which environment is loaded, which level is active and how the
    /// player gets there. Environments load additively and asynchronously behind a fade; requests that
    /// arrive mid-transition are queued (latest wins), so the calm button always lands in Neutral.
    /// </summary>
    public sealed class SessionManager : MonoBehaviour
    {
        [SerializeField] private PlayerRig playerRig;
        [SerializeField] private FadeController fade;
        [SerializeField] private StimulusController stimuli;
        [SerializeField] private LevelCatalog levels;
        [SerializeField] private float fadeSeconds = 0.9f;

        private EnvironmentRoot environment;
        private string loadedScene;
        private Request? pending;
        private Coroutine transition;

        /// <summary>Active level, or null while in the Neutral environment.</summary>
        public LevelDefinition CurrentLevel { get; private set; }
        public string CurrentScene => loadedScene;
        public bool IsTransitioning => transition != null;
        public bool IsInExposure => CurrentLevel != null && !IsTransitioning;
        public LevelCatalog Levels => levels;

        /// <summary>Raised after the scene or level changed and the new state is in place.</summary>
        public event Action StateChanged;

        /// <summary>Raised the moment calm is requested, before the transition starts.</summary>
        public event Action<CalmSource> CalmTriggered;

        public event Action<LevelDefinition> LevelEntered;
        public event Action<NeutralReason> NeutralEntered;

        private void Start()
        {
            string requested = StartupOptions.ConsumeRequestedScene();
            if (requested == SceneNames.Acrophobia) GoToLevel(1);
            else Enqueue(Request.Neutral(NeutralReason.Startup));
        }

        public void GoToLevel(int number)
        {
            var level = levels.Get(number);
            if (level == null)
            {
                Debug.LogWarning($"[SessionManager] Level {number} does not exist.");
                return;
            }
            Enqueue(Request.ForLevel(level));
        }

        public void TriggerCalm(CalmSource source)
        {
            CalmTriggered?.Invoke(source);
            Enqueue(Request.Neutral(NeutralReason.Calm));
        }

        public void ReturnToNeutral() => Enqueue(Request.Neutral(NeutralReason.Rest));

        private void Enqueue(Request request)
        {
            // A calm request is never replaced by anything queued after it in the same transition.
            if (pending is { Reason: NeutralReason.Calm } && request.Reason != NeutralReason.Calm) return;
            pending = request;
            transition ??= StartCoroutine(RunTransitions());
        }

        private IEnumerator RunTransitions()
        {
            playerRig.Block(this);
            while (pending is { } request)
            {
                pending = null;
                yield return fade.FadeOut(fadeSeconds);
                // Skip stale work: if something newer arrived during the fade, go straight to it.
                if (pending != null) continue;

                string scene = request.Level != null ? SceneNames.Acrophobia : SceneNames.Neutral;
                if (loadedScene != scene) yield return SwapEnvironment(scene);
                Apply(request);
                if (pending != null) continue;

                yield return fade.FadeIn(fadeSeconds);
            }
            transition = null;
            playerRig.Unblock(this);
            StateChanged?.Invoke();
        }

        private IEnumerator SwapEnvironment(string scene)
        {
            playerRig.Unbind();
            environment = null;
            if (loadedScene != null) yield return SceneManager.UnloadSceneAsync(loadedScene);

            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            var loaded = SceneManager.GetSceneByName(scene);
            // The active scene provides sky, fog and ambient light.
            SceneManager.SetActiveScene(loaded);
            environment = EnvironmentRoot.FindIn(loaded);
            loadedScene = scene;
            if (environment == null) throw new InvalidOperationException($"Scene '{scene}' has no EnvironmentRoot.");
        }

        private void Apply(Request request)
        {
            CurrentLevel = request.Level;
            environment.ApplyLevel(request.Level);
            var path = environment.WalkPath;

            if (request.Level is { } level)
            {
                stimuli.SetImmediate(level.defaultStimuli);
                playerRig.Bind(path, level.spawnDistance, level.minDistance, level.maxDistance, level.spawnYaw, level.spawnPitch);
                LevelEntered?.Invoke(level);
            }
            else
            {
                stimuli.SetImmediate(StimulusValues.Minimum);
                playerRig.Bind(path, environment.DefaultSpawnDistance, path.MinDistance, path.MaxDistance);
                NeutralEntered?.Invoke(request.Reason);
            }
            StateChanged?.Invoke();
        }

        private readonly struct Request
        {
            public readonly LevelDefinition Level;
            public readonly NeutralReason Reason;

            private Request(LevelDefinition level, NeutralReason reason)
            {
                Level = level;
                Reason = reason;
            }

            public static Request ForLevel(LevelDefinition level) => new(level, NeutralReason.None);
            public static Request Neutral(NeutralReason reason) => new(null, reason);
        }
    }

    public enum NeutralReason
    {
        None,
        Startup,
        Rest,
        Calm,
    }
}
