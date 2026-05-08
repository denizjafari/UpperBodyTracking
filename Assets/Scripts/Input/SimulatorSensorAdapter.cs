using System;
using UnityEngine;
using VRRehab.Data;

namespace VRRehab.Input
{
    /// <summary>
    /// Emits sine-wave joint angles at a fixed rate. Used by editor / unit tests
    /// when neither IMU nor controllers are available — every game must remain
    /// playable on this adapter.
    /// </summary>
    public class SimulatorSensorAdapter : MonoBehaviour, ISensorInputAdapter
    {
        public TrackingMode Mode => TrackingMode.Simulator;
        public event Action<UdpPacket> OnPacket;

        [Tooltip("Hz — simulated emit rate.")]
        public float updateHz = 50f;

        [Tooltip("Hz of the sine wave driving each joint angle.")]
        public float waveHz = 0.3f;

        [Tooltip("Peak angle in degrees.")]
        public float amplitude = 80f;

        [Tooltip("Optional offset per joint (degrees) so they don't all overlap.")]
        public float perJointPhaseOffsetDeg = 30f;

        bool _running;
        float _accum;
        float _t;

        public void Begin() { _running = true; _t = 0; }
        public void End()   { _running = false; }

        void Update()
        {
            if (!_running) return;
            _t += Time.deltaTime;
            _accum += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, updateHz);
            if (_accum < interval) return;
            _accum = 0f;

            long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (int i = 0; i < JointKeys.CalibrationOrder.Length; i++)
            {
                var joint = JointKeys.CalibrationOrder[i];
                float phase = i * Mathf.Deg2Rad * perJointPhaseOffsetDeg;
                float angle = Mathf.Sin(2f * Mathf.PI * waveHz * _t + phase) * amplitude;
                OnPacket?.Invoke(new UdpPacket
                {
                    ts_ms = ts,
                    joint = joint,
                    angle_deg = angle,
                    valid = true,
                    compensation = new CompensationInfo()
                });
            }
        }
    }
}
