using VRRehab.Input;

namespace VRRehab.Feedback
{
    /// <summary>
    /// One feedback modality (text, audio, avatar-highlight, haptic).
    /// FeedbackManager calls every enabled channel for each compensation event.
    /// </summary>
    public interface IFeedbackChannel
    {
        /// <summary>
        /// Used by FeedbackManager.LoadPreferences to enable/disable per the
        /// patient's settings.
        /// </summary>
        bool Enabled { get; set; }

        /// <param name="pkt">Packet whose compensation field triggered this.</param>
        /// <param name="severity">0..1 from the RPi.</param>
        /// <param name="message">Localized message string already resolved by FeedbackManager.</param>
        void Fire(UdpPacket pkt, float severity, string message);
    }
}
