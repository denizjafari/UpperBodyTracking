using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRRehab.Core;
using VRRehab.Data;
using VRRehab.Input;
using VRRehab.UI;

namespace VRRehab.Scenes.Calibration
{
    /// <summary>
    /// Scene 4 — Calibration B (Range of Motion). Implements the four-phase
    /// per-joint loop described in design §3.5–3.7:
    ///   1. Instruction (with avatar demo + replay-audio button)
    ///   2. Prep countdown (5..1, GO)
    ///   3. Recording window (10 s, captures min/max from UDP stream)
    ///   4. Transition (validate, persist or retry, then advance)
    /// Joints loop in <see cref="JointKeys.CalibrationOrder"/>:
    /// shoulder abduction → flexion → elbow flexion → shoulder rotation → wrist pronation.
    /// </summary>
    public class RomCalibrationController : SceneEntry
    {
        public override string SceneKey => "Calibration_B";

        public enum RomPhase { Instruction, PrepCountdown, Recording, Transition }

        [Header("UI")]
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text instructionText;
        [SerializeField] TMP_Text bigCenterText;     // shows countdown / "GO" / timer / "DONE"
        [SerializeField] Button replayAudioButton;
        [SerializeField] Button startButton;
        [SerializeField] Button skipJointButton;
        [SerializeField] Button cancelButton;

        [Header("Hooks")]
        [SerializeField] CalibrationAudioManager audio;
        [SerializeField] UdpControlSender controlSender;

        [Header("Per-joint instruction text (English)")]
        [SerializeField] InstructionEntry[] instructions = new[]
        {
            new InstructionEntry { joint = JointKeys.ShoulderAbduction, demoClip = "demo_shoulder_abduction",
                text = "Raise your arm out to the side as far as you can, then bring it back down." },
            new InstructionEntry { joint = JointKeys.ShoulderFlexion,   demoClip = "demo_shoulder_flexion",
                text = "Raise your arm forward as high as you can, then lower it back down." },
            new InstructionEntry { joint = JointKeys.ElbowFlexion,      demoClip = "demo_elbow_flexion",
                text = "Bend your elbow up toward your shoulder, then straighten it." },
            new InstructionEntry { joint = JointKeys.ShoulderRotation,  demoClip = "demo_shoulder_rotation",
                text = "Hold your elbow at 90°. Rotate your forearm out, then in." },
            new InstructionEntry { joint = JointKeys.WristPronation,    demoClip = "demo_wrist_pronation",
                text = "Hold your forearm steady. Rotate your palm up, then down." }
        };

        // ---- runtime state -------------------------------------------------
        RomCalibrationConfig _cfg;
        RomPhase _phase;
        int _jointIdx;
        float _runningMin, _runningMax;
        int _packetsReceivedThisWindow;
        int _retries;
        bool _isRecording;
        bool _lastPhaseSucceeded;
        Coroutine _flow;

        public override void OnEnter()
        {
            _cfg = RomCalibrationConfig.LoadOrDefault();
            if (titleText != null) titleText.text = "Range of Motion Calibration";
            replayAudioButton?.OnClick(() => audio?.ReplayInstruction());
            startButton     ?.OnClick(BeginFlow);
            skipJointButton ?.OnClick(SkipCurrentJoint);
            cancelButton    ?.OnClick(() => Services.Scenes.Replace(SceneKey, "Welcome"));

            Services.Sensors.OnPacket += HandlePacket;
            ShowInstructionForCurrent();
        }

        public override void OnExit()
        {
            if (_flow != null) StopCoroutine(_flow);
            Services.Sensors.OnPacket -= HandlePacket;
            replayAudioButton?.onClick.RemoveAllListeners();
            startButton     ?.onClick.RemoveAllListeners();
            skipJointButton ?.onClick.RemoveAllListeners();
            cancelButton    ?.onClick.RemoveAllListeners();
        }

        // ---- packet handling ------------------------------------------------

        void HandlePacket(UdpPacket pkt)
        {
            if (!_isRecording || pkt == null || string.IsNullOrEmpty(pkt.joint)) return;
            if (_jointIdx < 0 || _jointIdx >= JointKeys.CalibrationOrder.Length) return;
            string current = JointKeys.CalibrationOrder[_jointIdx];
            if (pkt.joint != current) return;
            _packetsReceivedThisWindow++;
            if (pkt.angle_deg < _runningMin) _runningMin = pkt.angle_deg;
            if (pkt.angle_deg > _runningMax) _runningMax = pkt.angle_deg;
        }

        // ---- main flow ------------------------------------------------------

        void BeginFlow()
        {
            if (_flow != null) return;
            _jointIdx = 0;
            _retries = 0;
            _flow = StartCoroutine(JointLoop());
        }

        IEnumerator JointLoop()
        {
            for (_jointIdx = 0; _jointIdx < JointKeys.CalibrationOrder.Length; _jointIdx++)
            {
                _retries = 0;
                bool jointDone = false;

                while (!jointDone)
                {
                    yield return PhaseInstruction();
                    yield return PhasePrepCountdown();
                    yield return PhaseRecording();
                    yield return PhaseTransition();

                    if (_lastPhaseSucceeded || _retries >= _cfg.max_retries) jointDone = true;
                    else _retries++;
                }
            }

            Services.Calibration.FinishRomCalibration();
            Services.Scenes.Replace(SceneKey, "SessionSetup");
            _flow = null;
        }

        // ---- Phase 1: Instruction ------------------------------------------

        IEnumerator PhaseInstruction()
        {
            _phase = RomPhase.Instruction;
            var entry = instructions[_jointIdx];
            ShowInstructionForCurrent();
            audio?.PlayInstruction(entry.joint);
            // Drive the demo loop on the avatar.
            Services.Avatar?.PlayDemo(entry.demoClip);

            // Wait briefly so the demo loops at least once before auto-advancing.
            // Per design §3.5: "user taps start" — we accept either the start
            // button OR an automatic countdown. Default = automatic 4 s wait.
            float autoAdvance = 4f;
            float t = 0f;
            bool advanced = false;
            void OnStart() { advanced = true; }
            startButton?.OnClick(OnStart);
            while (!advanced && t < autoAdvance)
            {
                t += Time.deltaTime;
                yield return null;
            }
            startButton?.onClick.RemoveListener(OnStart);

            Services.Avatar?.StopDemo();
        }

        void ShowInstructionForCurrent()
        {
            var entry = instructions[Mathf.Clamp(_jointIdx, 0, instructions.Length - 1)];
            if (instructionText != null) instructionText.text = entry.text;
            if (bigCenterText != null) bigCenterText.text = "";
        }

        // ---- Phase 2: Prep countdown ---------------------------------------

        IEnumerator PhasePrepCountdown()
        {
            _phase = RomPhase.PrepCountdown;
            int n = Mathf.Clamp(Mathf.RoundToInt(_cfg.prep_countdown_s), 3, 5);
            for (int i = n; i >= 1; i--)
            {
                if (bigCenterText != null) bigCenterText.text = i.ToString();
                audio?.PlayCountdownNumber(i);
                yield return new WaitForSeconds(1f);
            }
            if (bigCenterText != null) bigCenterText.text = "GO";
            audio?.PlayGoCue();
        }

        // ---- Phase 3: Recording --------------------------------------------

        IEnumerator PhaseRecording()
        {
            _phase = RomPhase.Recording;
            _runningMin = float.PositiveInfinity;
            _runningMax = float.NegativeInfinity;
            _packetsReceivedThisWindow = 0;
            _isRecording = true;

            string joint = JointKeys.CalibrationOrder[_jointIdx];
            controlSender?.SendBeginRecording(joint);

            float duration = _cfg.recording_window_s;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                if (bigCenterText != null)
                    bigCenterText.text = Mathf.CeilToInt(duration - t).ToString();
                yield return null;
            }

            _isRecording = false;
            controlSender?.SendEndRecording();
        }

        // ---- Phase 4: Transition + persist ---------------------------------

        IEnumerator PhaseTransition()
        {
            _phase = RomPhase.Transition;
            string joint = JointKeys.CalibrationOrder[_jointIdx];
            float range = _runningMax - _runningMin;

            bool valid = !float.IsInfinity(_runningMin) &&
                         !float.IsInfinity(_runningMax) &&
                         range >= _cfg.min_valid_range_deg;

            if (valid)
            {
                Services.Calibration.UpsertRomEntry(new RomJointEntry
                {
                    joint = joint,
                    min = _runningMin,
                    max = _runningMax,
                    complete = true,
                    retries = _retries
                });
                if (bigCenterText != null) bigCenterText.text = "DONE";
                audio?.PlayStopCue();
                _lastPhaseSucceeded = true;
            }
            else if (_retries < _cfg.max_retries)
            {
                audio?.PlayRetryPrompt();
                if (bigCenterText != null) bigCenterText.text = "Try again";
                _lastPhaseSucceeded = false;
            }
            else
            {
                // Persist a failure marker and a sensor-health entry.
                Services.Calibration.UpsertRomEntry(new RomJointEntry
                {
                    joint = joint,
                    min = float.IsInfinity(_runningMin) ? 0 : _runningMin,
                    max = float.IsInfinity(_runningMax) ? 0 : _runningMax,
                    complete = false,
                    retries = _retries + 1
                });
                SensorHealthLog.AppendRomFailure(
                    joint,
                    float.IsInfinity(range) ? 0f : range,
                    _packetsReceivedThisWindow,
                    _retries + 1);
                if (bigCenterText != null) bigCenterText.text = "Skipped";
                _lastPhaseSucceeded = true; // advance past this joint anyway
            }

            yield return new WaitForSeconds(_cfg.transition_pause_s);
        }

        void SkipCurrentJoint()
        {
            // User-initiated skip — useful for a clinician override.
            // We honour it by snapshotting whatever we have as incomplete
            // and advancing.
            if (_flow == null) return;
            _isRecording = false;
            string joint = JointKeys.CalibrationOrder[_jointIdx];
            Services.Calibration.UpsertRomEntry(new RomJointEntry
            {
                joint = joint, min = 0, max = 0, complete = false, retries = _retries
            });
        }

        [System.Serializable]
        public class InstructionEntry
        {
            public string joint;
            public string demoClip;
            [TextArea] public string text;
        }
    }
}
