using UnityEngine;
using VRRehab.Core;
using VRRehab.Managers;

namespace VRRehab.MiniGames.GardenMeditation
{
    /// <summary>
    /// Scene 9.5 — Garden Meditation. Passthrough-on, breath-paced relaxation.
    /// No IMU input is required, which makes this the ideal first game to
    /// validate the per-game module template end-to-end.
    /// </summary>
    /// <remarks>
    /// Score model:
    ///   normalized = clamp(completedCycles / targetCycles, 0, 1)
    ///   targetCycles = durationSeconds / cycleSeconds, clamped to ≥ 1
    /// Patients who finish early but stayed engaged still get a complete score.
    /// </remarks>
    public class GardenMeditationController : MiniGameSceneEntry
    {
        public override string SceneKey => "GardenMeditation";

        [Header("Scene wiring")]
        [SerializeField] BreathingPacer pacer;
        [SerializeField] FlowerBloomer  bloom;
        [SerializeField] BreathingHud   hud;
        [SerializeField] MicAmplitudeProbe micProbe;
        [SerializeField] AudioSource    ambienceSource;

        GardenMeditationConfig _cfg;
        int _targetCycles;

        protected override void OnGameStarted()
        {
            // Lock to 72 Hz — passthrough sync requirement on Quest 3S.
            #if UNITY_ANDROID && !UNITY_EDITOR
            try { OVRManager.display.displayFrequency = 72f; } catch { }
            #endif

            _cfg = GardenMeditationConfig.LoadOrDefault();

            if (pacer != null)
            {
                pacer.inhaleSeconds = _cfg.inhale_s;
                pacer.holdSeconds   = _cfg.hold_s;
                pacer.exhaleSeconds = _cfg.exhale_s;
                pacer.Begin();
                pacer.OnFullCycle += OnCycleCompleted;
            }

            if (micProbe != null) micProbe.enableProbe = _cfg.use_microphone;

            if (ambienceSource != null && !ambienceSource.isPlaying) ambienceSource.Play();

            float cycleSeconds = _cfg.inhale_s + _cfg.hold_s + _cfg.exhale_s;
            int duration = ActiveEntry?.durationSeconds ?? 180;
            _targetCycles = Mathf.Max(1, Mathf.RoundToInt(duration / cycleSeconds));

            Debug.Log($"[GardenMeditation] Started. duration={duration}s, target={_targetCycles} cycles, " +
                      $"pattern={_cfg.inhale_s}/{_cfg.hold_s}/{_cfg.exhale_s}, mic={_cfg.use_microphone}");
        }

        protected override void OnGameTeardown()
        {
            if (pacer != null)
            {
                pacer.OnFullCycle -= OnCycleCompleted;
                pacer.End();
            }
            if (ambienceSource != null) ambienceSource.Stop();
        }

        void OnCycleCompleted()
        {
            int cycles = pacer != null ? pacer.CompletedCycles : 0;
            float normalized = (float)cycles / Mathf.Max(1, _targetCycles);
            UpdateScore(normalized, cycles);

            if (cycles >= _targetCycles)
            {
                CompleteGame();
            }
        }
    }
}
