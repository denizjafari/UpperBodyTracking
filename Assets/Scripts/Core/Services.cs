using VRRehab.Managers;

namespace VRRehab.Core
{
    /// <summary>
    /// Service locator for the manager layer. Each manager registers itself
    /// in <c>Awake()</c>; the rest of the codebase pulls dependencies by
    /// reading static properties here, so we never call <c>FindObjectOfType</c>
    /// from gameplay code.
    /// </summary>
    /// <remarks>
    /// This is deliberately not a singleton-as-MonoBehaviour pattern. Managers
    /// are normal MonoBehaviours that live on the Core scene and self-register;
    /// tests can substitute fakes by writing the property directly before the
    /// scene runs.
    /// </remarks>
    public static class Services
    {
        public static SceneFlowManager   Scenes        { get; internal set; }
        public static UserManager        Users         { get; internal set; }
        public static PreferenceManager  Preferences   { get; internal set; }
        public static SessionManager     Session       { get; internal set; }
        public static CalibrationManager Calibration   { get; internal set; }
        public static SensorInputManager Sensors       { get; internal set; }
        public static AvatarDriver       Avatar        { get; internal set; }
        public static FeedbackManager    Feedback      { get; internal set; }
        public static LoggingManager     Logging       { get; internal set; }
        public static AudioBus           Audio         { get; internal set; }

        /// <summary>
        /// Required for boot: the persistent set of managers every phase relies on.
        /// Phase-2/3/4 managers (Sensors, Avatar, Feedback, Logging) are optional
        /// at this point — Bootstrapper warns about missing ones rather than failing.
        /// </summary>
        public static bool IsBooted =>
            Scenes != null && Users != null && Preferences != null &&
            Session != null && Calibration != null;
    }
}
