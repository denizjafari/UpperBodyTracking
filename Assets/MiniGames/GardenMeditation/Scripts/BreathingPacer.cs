using System;
using UnityEngine;

namespace VRRehab.MiniGames.GardenMeditation
{
    /// <summary>
    /// Drives the breathing cycle. Exposes a normalised <see cref="Phase"/> +
    /// <see cref="PhaseProgress"/> (0..1 within the current phase) and a
    /// <see cref="BreathTension"/> value (0..1) that climbs during inhale,
    /// holds during hold, and falls during exhale — used by the visual layer
    /// (flower scale) and the audio layer (ambient swell).
    /// </summary>
    public class BreathingPacer : MonoBehaviour
    {
        public enum BreathPhase { Inhale, Hold, Exhale }

        public event Action<BreathPhase> OnPhaseChanged;
        public event Action OnFullCycle;

        public BreathPhase Phase { get; private set; } = BreathPhase.Inhale;
        public float PhaseProgress { get; private set; }
        public float BreathTension { get; private set; }
        public int CompletedCycles { get; private set; }

        public float inhaleSeconds = 4f;
        public float holdSeconds   = 7f;
        public float exhaleSeconds = 8f;

        public bool IsRunning { get; private set; }
        float _t;

        public void Begin()
        {
            IsRunning = true;
            _t = 0f;
            CompletedCycles = 0;
            Phase = BreathPhase.Inhale;
            PhaseProgress = 0f;
            BreathTension = 0f;
            OnPhaseChanged?.Invoke(Phase);
        }

        public void End() { IsRunning = false; }

        void Update()
        {
            if (!IsRunning) return;
            _t += Time.deltaTime;
            float dur = CurrentPhaseDuration();
            PhaseProgress = Mathf.Clamp01(_t / Mathf.Max(0.01f, dur));

            switch (Phase)
            {
                case BreathPhase.Inhale: BreathTension = PhaseProgress; break;
                case BreathPhase.Hold:   BreathTension = 1f; break;
                case BreathPhase.Exhale: BreathTension = 1f - PhaseProgress; break;
            }

            if (_t >= dur) AdvancePhase();
        }

        float CurrentPhaseDuration() => Phase switch
        {
            BreathPhase.Inhale => inhaleSeconds,
            BreathPhase.Hold   => holdSeconds,
            BreathPhase.Exhale => exhaleSeconds,
            _ => 0f,
        };

        void AdvancePhase()
        {
            _t = 0f;
            switch (Phase)
            {
                case BreathPhase.Inhale: Phase = BreathPhase.Hold; break;
                case BreathPhase.Hold:   Phase = BreathPhase.Exhale; break;
                case BreathPhase.Exhale:
                    Phase = BreathPhase.Inhale;
                    CompletedCycles++;
                    OnFullCycle?.Invoke();
                    break;
            }
            OnPhaseChanged?.Invoke(Phase);
        }
    }
}
