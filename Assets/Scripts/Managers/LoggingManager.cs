using System.IO;
using UnityEngine;
using VRRehab.Core;
using VRRehab.Data;

namespace VRRehab.Managers
{
    /// <summary>
    /// Phase-11 wedge — for now writes <see cref="ExerciseResult"/> as JSON-per-line
    /// (NDJSON) into <c>Users/&lt;id&gt;/logs/exercise_results.ndjson</c>. Phase 11
    /// will replace this with proper SQLite tables; the public API stays stable so
    /// nothing upstream changes.
    /// </summary>
    public class LoggingManager : MonoBehaviour
    {
        void Awake() => Services.Logging = this;

        public void LogExerciseResult(ExerciseResult r)
        {
            if (r == null) return;
            var u = Services.Users?.Current;
            if (u == null) { Debug.LogWarning("[Logging] no current user; result discarded."); return; }
            var dir = Path.Combine(Services.Users.UserFolder(u.id), "logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "exercise_results.ndjson");
            try
            {
                File.AppendAllText(path, JsonUtility.ToJson(r) + "\n");
            }
            catch (System.Exception e) { Debug.LogError($"[Logging] {e.Message}"); }
        }
    }
}
