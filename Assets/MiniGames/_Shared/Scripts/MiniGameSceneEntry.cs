using System;
using UnityEngine;
using VRRehab.Core;
using VRRehab.Data;

namespace VRRehab.MiniGames
{
    /// <summary>
    /// Base class every mini-game's root SceneEntry must extend. Standardises
    /// how a game starts, ends, and reports its result.
    /// </summary>
    /// <remarks>
    /// Lifecycle:
    ///   - SceneFlowManager.LoadAdditive(GameKey) loads the scene.
    ///   - SceneEntry.OnEnter() runs; this base reads the active ExerciseEntry
    ///     and creates an in-flight <see cref="Result"/>.
    ///   - The concrete subclass plays its game, optionally calling
    ///     <see cref="UpdateScore"/> as the player accumulates points.
    ///   - When the game's duration expires (or the user quits), the subclass
    ///     calls <see cref="CompleteGame"/>; the base writes the result, raises
    ///     <see cref="OnGameCompleted"/>, and asks the SessionManager to advance.
    ///   - Phase 10 will insert a questionnaire overlay between completion and
    ///     advancement; this base is unaffected because the subclass calls
    ///     <c>CompleteGame</c> only.
    /// </remarks>
    public abstract class MiniGameSceneEntry : SceneEntry
    {
        public static event Action<ExerciseResult> OnGameCompleted;

        protected ExerciseEntry ActiveEntry { get; private set; }
        protected ExerciseResult Result { get; private set; }
        protected float ElapsedSeconds { get; private set; }
        protected bool IsRunning { get; private set; }

        public override void OnEnter()
        {
            ActiveEntry = Services.Session?.CurrentExercise;
            string sid = Services.Session?.Active?.sessionId ?? "_unbound";
            Result = ExerciseResult.NewFor(GameKey, ActiveEntry, sid);
            ElapsedSeconds = 0f;
            IsRunning = true;
            OnGameStarted();
        }

        public override void OnExit()
        {
            // If we're being unloaded mid-game, tag the result as aborted but
            // don't double-fire OnGameCompleted (CompleteGame may already have).
            if (IsRunning)
            {
                Result.aborted = true;
                Result.endedAtIso = System.DateTime.UtcNow.ToString("o");
                IsRunning = false;
                Services.Logging?.LogExerciseResult(Result);
            }
            OnGameTeardown();
        }

        protected virtual void Update()
        {
            if (!IsRunning) return;
            ElapsedSeconds += Time.deltaTime;
            if (ActiveEntry != null && ElapsedSeconds >= ActiveEntry.durationSeconds)
            {
                CompleteGame();
            }
        }

        // -------- subclass hooks --------

        /// <summary>Called once when the scene becomes active. Spawn props, wire UI.</summary>
        protected abstract void OnGameStarted();

        /// <summary>Called once when the scene is being unloaded. Tear down listeners.</summary>
        protected virtual void OnGameTeardown() { }

        // -------- helpers for the subclass --------

        protected void UpdateScore(float normalizedScore01, int rawScore)
        {
            Result.score = Mathf.Clamp01(normalizedScore01);
            Result.rawScore = rawScore;
        }

        /// <summary>
        /// Marks the game complete, persists the result, and asks
        /// <see cref="Managers.SessionManager"/> to advance to the next exercise.
        /// </summary>
        protected void CompleteGame()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Result.endedAtIso = System.DateTime.UtcNow.ToString("o");
            Services.Logging?.LogExerciseResult(Result);
            OnGameCompleted?.Invoke(Result);
            // Phase 10 wedge: when the questionnaire overlay exists, it intercepts
            // OnGameCompleted and calls AdvanceToNextExercise itself. Until then,
            // we advance directly so dev builds don't hang.
            if (!QuestionnaireOverlayActive())
            {
                Services.Session?.AdvanceToNextExercise();
                Services.Scenes?.UnloadAdditive(SceneKey);
            }
        }

        static bool QuestionnaireOverlayActive()
        {
            // Phase 10 will replace this with a real check.
            return false;
        }
    }
}
