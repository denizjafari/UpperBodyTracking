using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRRehab.MiniGames.GardenMeditation
{
    /// <summary>
    /// Minimalist heads-up panel that calls out the current phase and shows
    /// a circular progress fill. World-space canvas only — never screen-space
    /// in VR. Place ~1.2 m in front of the patient at eye level.
    /// </summary>
    public class BreathingHud : MonoBehaviour
    {
        [SerializeField] BreathingPacer pacer;
        [SerializeField] TMP_Text phaseLabel;
        [SerializeField] TMP_Text instructionLabel;
        [SerializeField] Image progressRing;

        [TextArea] public string inhaleText  = "Breathe in";
        [TextArea] public string holdText    = "Hold";
        [TextArea] public string exhaleText  = "Breathe out";

        void OnEnable()
        {
            if (pacer != null) pacer.OnPhaseChanged += UpdatePhase;
        }
        void OnDisable()
        {
            if (pacer != null) pacer.OnPhaseChanged -= UpdatePhase;
        }

        void Update()
        {
            if (pacer == null) return;
            if (progressRing != null) progressRing.fillAmount = pacer.PhaseProgress;
        }

        void UpdatePhase(BreathingPacer.BreathPhase phase)
        {
            if (phaseLabel == null) return;
            switch (phase)
            {
                case BreathingPacer.BreathPhase.Inhale:
                    phaseLabel.text = "Inhale";
                    if (instructionLabel != null) instructionLabel.text = inhaleText;
                    break;
                case BreathingPacer.BreathPhase.Hold:
                    phaseLabel.text = "Hold";
                    if (instructionLabel != null) instructionLabel.text = holdText;
                    break;
                case BreathingPacer.BreathPhase.Exhale:
                    phaseLabel.text = "Exhale";
                    if (instructionLabel != null) instructionLabel.text = exhaleText;
                    break;
            }
        }
    }
}
