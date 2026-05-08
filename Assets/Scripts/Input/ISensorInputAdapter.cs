using System;

namespace VRRehab.Input
{
    /// <summary>
    /// Source-of-truth for joint angles. The active adapter pushes packets into
    /// <see cref="SensorInputManager"/> via the <see cref="OnPacket"/> event.
    /// </summary>
    public interface ISensorInputAdapter
    {
        TrackingMode Mode { get; }

        /// <summary>Fired on the main thread for each well-formed packet.</summary>
        event Action<UdpPacket> OnPacket;

        void Begin();
        void End();
    }
}
