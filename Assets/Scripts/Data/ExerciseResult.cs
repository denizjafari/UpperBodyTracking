using System;

namespace VRRehab.Data
{
    /// <summary>
    /// What a mini-game writes when it ends. One row in the SQLite
    /// <c>exercise_results</c> table once Phase 11 lands; until then the
    /// LoggingManager just stashes it as JSON inside the user's session folder.
    /// </summary>
    [Serializable]
    public class ExerciseResult
    {
        public string sessionId;
        public string game;
        public int    difficulty;
        public int    durationSeconds;
        public float  score;            // game-specific 0..1 normalized
        public int    rawScore;         // game-specific raw count (whacks, reps, …)
        public string startedAtIso;
        public string endedAtIso;
        public bool   aborted;          // user quit before the duration expired

        public static ExerciseResult NewFor(string game, ExerciseEntry entry, string sessionId)
        {
            return new ExerciseResult
            {
                sessionId = sessionId,
                game = game,
                difficulty = entry?.difficulty ?? 1,
                durationSeconds = entry?.durationSeconds ?? 0,
                startedAtIso = DateTime.UtcNow.ToString("o")
            };
        }
    }
}
