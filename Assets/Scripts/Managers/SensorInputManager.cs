using System;
using System.Collections.Generic;
using UnityEngine;
using VRRehab.Core;
using VRRehab.Input;

namespace VRRehab.Managers
{
    /// <summary>
    /// Single point of access to joint angles. Owns the active
    /// <see cref="ISensorInputAdapter"/>, fans the packet stream out to
    /// listeners (FeedbackManager, AvatarDriver, mini-games), and detects
    /// per-joint sensor timeouts.
    /// </summary>
    public class SensorInputManager : MonoBehaviour
    {
        public event Action<UdpPacket> OnPacket;
        public event Action<string>    OnSensorTimeout;   // joint key
        public event Action<string>    OnSensorRecovered; // joint key

        public TrackingMode CurrentMode { get; private set; } = TrackingMode.IMU_UDP;
        public JointSnapshot LastJointAngles { get; } = new JointSnapshot();

        [Tooltip("Per-joint timeout (ms) — design says 500 ms to flag a disconnect.")]
        public int sensorTimeoutMs = 500;

        [Header("Adapter components on this GameObject")]
        [SerializeField] UdpSensorAdapter        udpAdapter;
        [SerializeField] ControllerSensorAdapter controllerAdapter;
        [SerializeField] SimulatorSensorAdapter  simulatorAdapter;

        ISensorInputAdapter _active;
        readonly HashSet<string> _timedOut = new HashSet<string>();

        void Awake() => Services.Sensors = this;

        void Start()
        {
            // Default: try UDP. If a session config later overrides this we'll switch.
            SwitchTo(TrackingMode.IMU_UDP);
        }

        void OnDisable() => DetachActive();

        public void SwitchTo(TrackingMode mode)
        {
            if (_active != null && _active.Mode == mode) return;
            DetachActive();

            ISensorInputAdapter next = mode switch
            {
                TrackingMode.IMU_UDP    => udpAdapter,
                TrackingMode.Controllers => controllerAdapter,
                TrackingMode.Simulator   => simulatorAdapter,
                _ => null
            };

            if (next == null)
            {
                Debug.LogError($"[SensorInput] No adapter component assigned for mode {mode}");
                return;
            }

            _active = next;
            _active.OnPacket += HandlePacket;
            _active.Begin();
            CurrentMode = mode;
            LastJointAngles.Clear();
            _timedOut.Clear();
            Debug.Log($"[SensorInput] Switched to {mode}");
        }

        void DetachActive()
        {
            if (_active == null) return;
            _active.OnPacket -= HandlePacket;
            _active.End();
            _active = null;
        }

        void HandlePacket(UdpPacket pkt)
        {
            if (pkt == null || string.IsNullOrEmpty(pkt.joint)) return;
            if (!pkt.valid) return;
            LastJointAngles.Set(pkt.joint, pkt.angle_deg, pkt.ts_ms);
            if (_timedOut.Remove(pkt.joint))
            {
                OnSensorRecovered?.Invoke(pkt.joint);
            }
            OnPacket?.Invoke(pkt);
        }

        void Update()
        {
            CheckTimeouts();
        }

        void CheckTimeouts()
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var joint in LastJointAngles.Joints)
            {
                long ts = LastJointAngles.TimestampOf(joint);
                if (ts == 0) continue;
                bool timedOut = nowMs - ts > sensorTimeoutMs;
                if (timedOut && _timedOut.Add(joint))
                {
                    OnSensorTimeout?.Invoke(joint);
                }
            }
        }

        // --- helpers used by mini-games --------------------------------------

        public float GetAngle(string joint, float fallback = 0f) =>
            LastJointAngles.Get(joint, fallback);

        public bool HasFreshFor(string joint, int maxAgeMs = 200)
        {
            long ts = LastJointAngles.TimestampOf(joint);
            return ts > 0 &&
                   DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - ts <= maxAgeMs;
        }
    }
}
