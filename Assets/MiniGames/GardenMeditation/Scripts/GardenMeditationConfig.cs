using System;
using System.IO;
using UnityEngine;

namespace VRRehab.MiniGames.GardenMeditation
{
    /// <summary>
    /// Mirrors <c>Settings/garden_meditation_config.json</c>. Default values
    /// implement the 4-7-8 breathing pattern (Andrew Weil) which is the most
    /// commonly cited evidence-based pacing for relaxation:
    ///   inhale 4 s · hold 7 s · exhale 8 s.
    /// </summary>
    [Serializable]
    public class GardenMeditationConfig
    {
        public float inhale_s = 4f;
        public float hold_s   = 7f;
        public float exhale_s = 8f;
        public bool  use_microphone = false;
        public float mic_amplitude_target = 0.18f;
        public string ambient_music_clip = "garden_ambient";

        public static GardenMeditationConfig LoadOrDefault()
        {
            string streaming = Path.Combine(Application.streamingAssetsPath,
                                            "garden_meditation_config.json");
            if (File.Exists(streaming))
            {
                try { return JsonUtility.FromJson<GardenMeditationConfig>(File.ReadAllText(streaming)); }
                catch (Exception e) { Debug.LogWarning($"[GardenConfig] {e.Message}"); }
            }
            return new GardenMeditationConfig();
        }
    }
}
