using System.Collections.Generic;
using UnityEngine;
using VRRehab.Core;
using VRRehab.Input;
using VRRehab.Managers;

namespace VRRehab.Feedback
{
    /// <summary>
    /// Plays a pre-baked TTS clip per compensation type. Lookup is by
    /// <c>compensation.type</c> string against a serialized list assigned in
    /// the inspector.
    /// </summary>
    public class AudioFeedbackChannel : MonoBehaviour, IFeedbackChannel
    {
        [System.Serializable]
        public class Entry { public string compensationType; public AudioClip clip; }

        [SerializeField] List<Entry> clips = new List<Entry>();

        public bool Enabled { get; set; } = true;

        readonly Dictionary<string, AudioClip> _byType = new Dictionary<string, AudioClip>();

        void Start()
        {
            foreach (var e in clips)
                if (!string.IsNullOrEmpty(e.compensationType)) _byType[e.compensationType] = e.clip;
        }

        public void Fire(UdpPacket pkt, float severity, string message)
        {
            if (!Enabled || pkt?.compensation == null) return;
            if (!_byType.TryGetValue(pkt.compensation.type ?? "", out var clip) || clip == null) return;
            // Severity gates audio: skip the gentlest tier so we don't nag.
            if (severity < 0.33f) return;
            Services.Audio?.PlayOneShot(clip, AudioBus.Group.Voice);
        }
    }
}
