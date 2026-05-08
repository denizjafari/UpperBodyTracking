using System.Collections;
using UnityEngine;
using VRRehab.Input;

namespace VRRehab.Feedback
{
    /// <summary>
    /// Right-controller haptic pulse via OVRInput. Always self-stops — the OVR
    /// haptics API does not auto-cancel.
    /// </summary>
    /// <remarks>
    /// We call OVRInput via reflection-friendly types so this file compiles
    /// even when the Meta SDK isn't installed (the call is wrapped in
    /// UNITY_ANDROID || OCULUS_XR). In a real build with v85, OVRInput is
    /// always available.
    /// </remarks>
    public class HapticChannel : MonoBehaviour, IFeedbackChannel
    {
        public bool Enabled { get; set; } = true;

        [Tooltip("Pulse duration for the strongest tier, in seconds.")]
        public float strongPulse = 0.18f;

        [Tooltip("Pulse duration for the medium tier, in seconds.")]
        public float mediumPulse = 0.08f;

        public void Fire(UdpPacket pkt, float severity, string message)
        {
            if (!Enabled) return;
            if (severity < 0.33f) return;
            float duration = severity >= 0.66f ? strongPulse : mediumPulse;
            float amp      = severity >= 0.66f ? 1f : 0.6f;
            StartCoroutine(Pulse(duration, amp));
        }

        IEnumerator Pulse(float seconds, float amp)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try { OVRInput.SetControllerVibration(1f, amp, OVRInput.Controller.RTouch); }
            catch { /* OVR not present */ }
            yield return new WaitForSeconds(seconds);
            try { OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch); }
            catch { }
#else
            yield return new WaitForSeconds(seconds);
#endif
        }
    }
}
