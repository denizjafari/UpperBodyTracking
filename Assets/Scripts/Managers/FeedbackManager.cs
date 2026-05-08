using System.Collections.Generic;
using UnityEngine;
using VRRehab.Core;
using VRRehab.Data;
using VRRehab.Feedback;
using VRRehab.Input;

namespace VRRehab.Managers
{
    /// <summary>
    /// Receives every UDP packet from <see cref="SensorInputManager"/>,
    /// inspects its <c>compensation</c> field, and dispatches the right mix of
    /// channels (text / audio / avatar / haptic) gated by the patient's
    /// preferences and severity.
    /// </summary>
    /// <remarks>
    /// Cooldown: a held compensation re-fires at the packet rate (50 Hz)
    /// without protection. We suppress same-type events for
    /// <see cref="cooldownSeconds"/>.
    /// </remarks>
    public class FeedbackManager : MonoBehaviour
    {
        [SerializeField] TextFeedbackChannel    textChannel;
        [SerializeField] AudioFeedbackChannel   audioChannel;
        [SerializeField] AvatarHighlightChannel avatarChannel;
        [SerializeField] HapticChannel          hapticChannel;

        [Tooltip("Suppress repeats of the same compensation type for this many seconds.")]
        public float cooldownSeconds = 2f;

        readonly Dictionary<string, float> _lastFiredAt = new Dictionary<string, float>();

        // Compensation type → localized message. Populate in inspector or by editor script.
        [System.Serializable] public class MessageEntry { public string compensationType; public string en; public string fr; }
        [SerializeField] List<MessageEntry> messages = new List<MessageEntry>();
        readonly Dictionary<string, MessageEntry> _msgByType = new Dictionary<string, MessageEntry>();

        void Awake()
        {
            Services.Feedback = this;
            foreach (var m in messages)
                if (!string.IsNullOrEmpty(m.compensationType)) _msgByType[m.compensationType] = m;
        }

        void Start()
        {
            if (Services.Sensors != null) Services.Sensors.OnPacket += HandlePacket;
            if (Services.Preferences != null)
            {
                Services.Preferences.OnPreferencesChanged += LoadPreferences;
                LoadPreferences(Services.Preferences.Current);
            }
        }

        void OnDisable()
        {
            if (Services.Sensors != null) Services.Sensors.OnPacket -= HandlePacket;
            if (Services.Preferences != null) Services.Preferences.OnPreferencesChanged -= LoadPreferences;
        }

        public void LoadPreferences(UserPreferences prefs)
        {
            if (prefs == null) return;
            if (textChannel   != null) textChannel.Enabled   = prefs.textOn;
            if (audioChannel  != null) audioChannel.Enabled  = prefs.audioOn;
            if (avatarChannel != null) avatarChannel.Enabled = prefs.visualOn;
            if (hapticChannel != null) hapticChannel.Enabled = prefs.hapticOn;
        }

        void HandlePacket(UdpPacket pkt)
        {
            if (pkt?.compensation == null || !pkt.compensation.detected) return;
            var key = pkt.compensation.type ?? "unknown";

            // Cooldown
            if (_lastFiredAt.TryGetValue(key, out var t) && Time.time - t < cooldownSeconds) return;
            _lastFiredAt[key] = Time.time;

            float severity = Mathf.Clamp01(pkt.compensation.severity);
            string message = ResolveMessage(key);

            textChannel?.Fire(pkt, severity, message);
            audioChannel?.Fire(pkt, severity, message);
            avatarChannel?.Fire(pkt, severity, message);
            hapticChannel?.Fire(pkt, severity, message);
        }

        string ResolveMessage(string compensationType)
        {
            if (!_msgByType.TryGetValue(compensationType, out var m)) return compensationType;
            var lang = Services.Preferences?.Current?.language ?? "en";
            return lang == "fr" ? (m.fr ?? m.en) : m.en;
        }

        /// <summary>
        /// Test hook used by mini-game tests and the simulator: fire a synthetic
        /// compensation through the full pipeline.
        /// </summary>
        public void RaiseSyntheticCompensation(string type, float severity)
        {
            HandlePacket(new UdpPacket
            {
                ts_ms = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                joint = "synthetic",
                angle_deg = 0f,
                valid = true,
                compensation = new CompensationInfo { detected = true, type = type, severity = severity }
            });
        }
    }
}
