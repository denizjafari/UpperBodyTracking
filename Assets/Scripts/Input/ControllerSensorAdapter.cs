using System;
using UnityEngine;
using UnityEngine.XR;
using VRRehab.Data;

namespace VRRehab.Input
{
    /// <summary>
    /// Dev-only adapter that synthesises joint angles from the right XR
    /// controller's pose. Used when no Raspberry Pi / IMU is attached.
    /// NEVER ship this to clinical builds — the angles are approximate and
    /// elbow flexion cannot be derived from a single controller.
    /// </summary>
    public class ControllerSensorAdapter : MonoBehaviour, ISensorInputAdapter
    {
        public TrackingMode Mode => TrackingMode.Controllers;
        public event Action<UdpPacket> OnPacket;

        [Tooltip("Reference to the OVRCameraRig or its CenterEyeAnchor — used for shoulder origin.")]
        public Transform headRef;

        [Tooltip("Hz — how often to synthesise + emit packets.")]
        public float updateHz = 50f;

        bool _running;
        float _accum;
        InputDevice _right;

        public void Begin()
        {
            _running = true;
            _right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            Debug.Log("[ControllerSensor] DEV adapter active. Do not ship to clinical builds.");
        }

        public void End() { _running = false; }

        void Update()
        {
            if (!_running) return;
            _accum += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, updateHz);
            if (_accum < interval) return;
            _accum = 0f;

            if (!_right.isValid) _right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!_right.isValid) return;

            _right.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos);
            _right.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot);

            // Approximate shoulder origin: 25 cm below head, 18 cm to the right.
            Vector3 shoulder = headRef
                ? headRef.position + headRef.right * 0.18f - headRef.up * 0.25f
                : Vector3.zero;

            Vector3 armVec = (pos - shoulder).normalized;

            // Shoulder flexion: angle between arm vector and -up (raise forward).
            float flexion = Vector3.Angle(Vector3.down, Vector3.ProjectOnPlane(armVec, headRef ? headRef.right : Vector3.right));
            // Shoulder abduction: angle between arm vector and -up in frontal plane.
            float abduction = Vector3.Angle(Vector3.down, Vector3.ProjectOnPlane(armVec, headRef ? headRef.forward : Vector3.forward));
            // Wrist pronation: rotation around the controller's local forward axis.
            float pronation = NormalizeAngle(rot.eulerAngles.z);
            // Shoulder rotation: yaw of the controller relative to the head.
            float rotation = NormalizeAngle(rot.eulerAngles.y - (headRef ? headRef.eulerAngles.y : 0f));

            long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Emit(JointKeys.ShoulderFlexion,   flexion,   ts);
            Emit(JointKeys.ShoulderAbduction, abduction, ts);
            Emit(JointKeys.WristPronation,    pronation, ts);
            Emit(JointKeys.ShoulderRotation,  rotation,  ts);
            // Elbow flexion: not derivable from a single controller; pin to 90°.
            Emit(JointKeys.ElbowFlexion, 90f, ts);
        }

        void Emit(string joint, float deg, long ts)
        {
            OnPacket?.Invoke(new UdpPacket
            {
                ts_ms = ts,
                joint = joint,
                angle_deg = deg,
                valid = true,
                compensation = new CompensationInfo()
            });
        }

        static float NormalizeAngle(float deg)
        {
            deg %= 360f;
            if (deg > 180f) deg -= 360f;
            if (deg < -180f) deg += 360f;
            return deg;
        }
    }
}
