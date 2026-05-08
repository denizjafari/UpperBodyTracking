namespace VRRehab.Input
{
    /// <summary>
    /// Per design §4.2 — every game queries the active tracking mode at
    /// <c>Awake</c> and binds the matching input adapter.
    /// </summary>
    public enum TrackingMode
    {
        /// <summary>Joint angles streamed from the Raspberry Pi over UDP. Clinical default.</summary>
        IMU_UDP,

        /// <summary>Synthesise joint angles from XR controller pose (dev only).</summary>
        Controllers,

        /// <summary>Sine-wave / scripted angles for unit tests.</summary>
        Simulator
    }
}
