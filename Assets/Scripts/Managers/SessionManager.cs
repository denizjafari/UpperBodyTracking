using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRRehab.Core;
using VRRehab.Data;

namespace VRRehab.Managers
{
    /// <summary>
    /// Owns the in-flight session: today's exercise queue, the current exercise
    /// pointer, and the entry/exit hooks the calibration / mini-game scenes use
    /// to advance the routine.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        public event Action<SessionConfig>  OnSessionStarted;
        public event Action<ExerciseEntry> OnExerciseStarted;
        public event Action<ExerciseEntry> OnExerciseCompleted;
        public event Action                OnSessionEnded;

        public SessionConfig Active { get; private set; }
        public ExerciseEntry CurrentExercise { get; private set; }
        public int CurrentIndex { get; private set; } = -1;
        public bool IsActive => Active != null;

        void Awake() => Services.Session = this;

        public void StartSession(SessionConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.userId))
            {
                Debug.LogError("[SessionManager] StartSession called with invalid config");
                return;
            }
            Active = config;
            CurrentIndex = -1;
            CurrentExercise = null;

            // Persist the config alongside the user's data.
            var path = ConfigPath(config.userId);
            File.WriteAllText(path, JsonUtility.ToJson(config, true));

            Debug.Log($"[SessionManager] Started session {config.sessionId} " +
                      $"with {config.exercises.Count} exercise(s).");
            OnSessionStarted?.Invoke(config);

            AdvanceToNextExercise();
        }

        public void AdvanceToNextExercise()
        {
            if (Active == null) return;

            // Mark previous exercise complete.
            if (CurrentExercise != null)
            {
                OnExerciseCompleted?.Invoke(CurrentExercise);
            }

            CurrentIndex++;
            if (CurrentIndex >= Active.exercises.Count)
            {
                EndSession();
                return;
            }

            CurrentExercise = Active.exercises[CurrentIndex];
            OnExerciseStarted?.Invoke(CurrentExercise);
        }

        public void EndSession(bool aborted = false)
        {
            if (Active == null) return;
            Debug.Log($"[SessionManager] Ending session {Active.sessionId} (aborted={aborted})");
            OnSessionEnded?.Invoke();
            Active = null;
            CurrentExercise = null;
            CurrentIndex = -1;
        }

        public string ConfigPath(string userId)
        {
            return Path.Combine(Services.Users.UserFolder(userId), "session_config.json");
        }
    }
}
