using System;
using System.IO;
using System.Text;
using UnityEngine;
using VRRehab.Core;

namespace VRRehab.Scenes.Calibration
{
    /// <summary>
    /// Append-only sensor-health log used by ROM calibration. Format and
    /// fields per design §3.7. Persisted at
    /// <c>Users/&lt;id&gt;/logs/sensor_health.json</c>.
    /// One JSON object per line (NDJSON) — keeps the file small and crash-safe.
    /// </summary>
    public static class SensorHealthLog
    {
        public static void AppendRomFailure(string joint, float finalRangeDeg, int packetsReceived, int attempts)
        {
            var u = Services.Users?.Current;
            if (u == null) return;
            var path = Path.Combine(Services.Users.UserFolder(u.id), "logs", "sensor_health.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var line = new StringBuilder();
            line.Append("{");
            line.Append($"\"timestamp_ms\":{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()},");
            line.Append($"\"type\":\"rom_calibration_failed\",");
            line.Append($"\"joint\":\"{Esc(joint)}\",");
            line.Append($"\"final_range_deg\":{finalRangeDeg.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
            line.Append($"\"packets_received\":{packetsReceived},");
            line.Append($"\"attempts\":{attempts}");
            line.Append("}\n");

            try { File.AppendAllText(path, line.ToString()); }
            catch (Exception e) { Debug.LogError($"[SensorHealthLog] {e.Message}"); }
        }

        static string Esc(string s) =>
            s == null ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
