using System;
using System.Collections.Generic;

namespace VRRehab.Data
{
    /// <summary>
    /// One exercise entry inside a session's routine.
    /// </summary>
    [Serializable]
    public class ExerciseEntry
    {
        public string game;        // canonical mini-game key, e.g. "FlappyBird"
        public int difficulty;     // 1..5
        public int durationSeconds;
    }

    /// <summary>
    /// Today's session. Written by Scene 6 (Session Setup) and read by every
    /// downstream scene. Persisted as <c>session_config.json</c>.
    /// </summary>
    [Serializable]
    public class SessionConfig
    {
        public string sessionId;
        public string userId;
        public string trackingMode = "IMU_UDP"; // see VRRehab.Input.TrackingMode
        public string startedAtIso;
        public List<ExerciseEntry> exercises = new List<ExerciseEntry>();

        public static SessionConfig NewFor(string userId)
        {
            var now = DateTime.UtcNow;
            return new SessionConfig
            {
                userId = userId,
                sessionId = now.ToString("yyyy-MM-ddTHH-mm-ssZ"),
                startedAtIso = now.ToString("o")
            };
        }
    }
}
