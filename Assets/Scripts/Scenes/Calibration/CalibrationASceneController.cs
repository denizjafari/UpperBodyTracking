using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRRehab.Core;
using VRRehab.Input;
using VRRehab.UI;

namespace VRRehab.Scenes.Calibration
{
    /// <summary>
    /// Scene 3 — Calibration A (IMU bias). Patient holds neutral pose for 5 s
    /// while the Raspberry Pi estimates IMU offsets. Unity is purely a
    /// presenter here — it does not compute calibration values, only signals
    /// start and waits for the RPi's "bias_complete" reply.
    /// </summary>
    public class CalibrationASceneController : SceneEntry
    {
        public override string SceneKey => "Calibration_A";

        [Header("UI")]
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text countdownText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Button startButton;
        [SerializeField] Button skipButton;

        [Header("RPi handshake")]
        [SerializeField] UdpControlSender controlSender;

        [Header("Timing (seconds)")]
        public float prepCountdown = 3f;
        public float holdSeconds = 5f;
        public float waitForRpiAckSeconds = 10f;

        Coroutine _running;

        public override void OnEnter()
        {
            if (titleText != null) titleText.text = "IMU Bias Calibration";
            SetStatus("Stand neutrally with arms at your sides. Tap Start when ready.");
            if (countdownText != null) countdownText.text = "";

            startButton?.OnClick(BeginFlow);
            skipButton ?.OnClick(() => Services.Scenes.Replace(SceneKey, NextSceneKey()));

            // Mirror the avatar so the patient sees a face-on demo of neutral pose.
            Services.Avatar?.SetMirror(true);
        }

        public override void OnExit()
        {
            if (_running != null) StopCoroutine(_running);
            startButton?.onClick.RemoveAllListeners();
            skipButton ?.onClick.RemoveAllListeners();
            Services.Avatar?.SetMirror(false);
        }

        void BeginFlow()
        {
            if (_running != null) return;
            _running = StartCoroutine(FlowRoutine());
        }

        IEnumerator FlowRoutine()
        {
            // 1. Prep countdown
            for (int n = Mathf.RoundToInt(prepCountdown); n >= 1; n--)
            {
                if (countdownText != null) countdownText.text = n.ToString();
                yield return new WaitForSeconds(1f);
            }
            if (countdownText != null) countdownText.text = "Hold!";

            // 2. Tell the RPi to start estimating bias.
            controlSender?.SendStartBias();
            SetStatus("Hold pose. Estimating IMU bias…");

            // 3. Wait the hold period.
            yield return new WaitForSeconds(holdSeconds);

            // 4. Tell the RPi to stop, then wait for an ack packet.
            controlSender?.SendStopBias();
            SetStatus("Finalising…");

            bool ack = false;
            void OnPkt(UdpPacket p)
            {
                if (p?.joint == "_ack" && p.compensation != null && p.compensation.type == "bias_complete")
                    ack = true;
            }
            Services.Sensors.OnPacket += OnPkt;

            float waited = 0f;
            while (!ack && waited < waitForRpiAckSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Services.Sensors.OnPacket -= OnPkt;

            if (ack)
            {
                Services.Calibration.MarkImuBiasComplete();
                SetStatus("Calibration complete.");
                if (countdownText != null) countdownText.text = "✓";
                yield return new WaitForSeconds(1.5f);
                Services.Scenes.Replace(SceneKey, NextSceneKey());
            }
            else
            {
                SetStatus("No reply from sensors. Tap Start to retry, or Skip.");
                if (countdownText != null) countdownText.text = "";
            }

            _running = null;
        }

        string NextSceneKey()
        {
            // After bias, decide whether ROM is needed.
            return Services.Calibration.IsRomCalibrationRequired()
                ? "Calibration_B"
                : "SessionSetup";
        }

        void SetStatus(string msg) { if (statusText != null) statusText.text = msg; }
    }
}
