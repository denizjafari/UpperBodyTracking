using TMPro;
using UnityEngine;
using VRRehab.Input;

namespace VRRehab.Feedback
{
    /// <summary>
    /// World-space TMP panel. Severity colors:
    ///   0.0–0.33 = green
    ///   0.33–0.66 = yellow
    ///   0.66–1.0  = red
    /// </summary>
    public class TextFeedbackChannel : MonoBehaviour, IFeedbackChannel
    {
        [SerializeField] TMP_Text label;
        [SerializeField] CanvasGroup group;
        [SerializeField] float fadeOutAfterSeconds = 2.5f;

        public bool Enabled { get; set; } = true;

        float _hideAt;

        void Start()
        {
            if (group != null) group.alpha = 0f;
        }

        void Update()
        {
            if (group == null) return;
            if (Time.time > _hideAt && group.alpha > 0f)
            {
                group.alpha = Mathf.Max(0f, group.alpha - Time.deltaTime * 2f);
            }
        }

        public void Fire(UdpPacket pkt, float severity, string message)
        {
            if (!Enabled || label == null) return;
            label.text = message ?? string.Empty;
            label.color = ColorFor(severity);
            if (group != null) group.alpha = 1f;
            _hideAt = Time.time + fadeOutAfterSeconds;
        }

        static Color ColorFor(float severity)
        {
            if (severity >= 0.66f) return new Color(0.95f, 0.25f, 0.20f);
            if (severity >= 0.33f) return new Color(0.95f, 0.78f, 0.18f);
            return new Color(0.30f, 0.85f, 0.40f);
        }
    }
}
