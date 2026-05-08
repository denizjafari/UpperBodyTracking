using System;
using System.IO;
using UnityEngine;

namespace VRRehab.Scenes.Calibration
{
    /// <summary>
    /// Mirrors <c>Assets/Settings/rom_calibration.json</c>. All values are
    /// configurable to support future clinical protocols.
    /// </summary>
    [Serializable]
    public class RomCalibrationConfig
    {
        public float prep_countdown_s = 5f;
        public float recording_window_s = 10f;
        public float transition_pause_s = 1.5f;
        public int   max_retries = 1;
        public float min_valid_range_deg = 5f;

        public static RomCalibrationConfig LoadOrDefault()
        {
            // Tries StreamingAssets first, then a Resources fallback, finally defaults.
            string streaming = Path.Combine(Application.streamingAssetsPath,
                                            "rom_calibration.json");
            if (File.Exists(streaming))
            {
                try { return JsonUtility.FromJson<RomCalibrationConfig>(File.ReadAllText(streaming)); }
                catch (Exception e) { Debug.LogWarning($"[RomConfig] Bad streaming JSON: {e.Message}"); }
            }

            var ta = Resources.Load<TextAsset>("rom_calibration");
            if (ta != null)
            {
                try { return JsonUtility.FromJson<RomCalibrationConfig>(ta.text); }
                catch (Exception e) { Debug.LogWarning($"[RomConfig] Bad resource JSON: {e.Message}"); }
            }
            Debug.Log("[RomConfig] Falling back to defaults.");
            return new RomCalibrationConfig();
        }
    }
}
