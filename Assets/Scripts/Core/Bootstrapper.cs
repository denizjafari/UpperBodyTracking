using UnityEngine;
using UnityEngine.SceneManagement;
using VRRehab.Managers;

namespace VRRehab.Core
{
    /// <summary>
    /// Single entry point for the application. Lives on a [Bootstrapper]
    /// GameObject in the Core scene and:
    ///   1. waits for every manager to self-register in <see cref="Services"/>,
    ///   2. raises <see cref="OnBooted"/> once everything is ready,
    ///   3. loads the first additive scene (Welcome).
    /// </summary>
    /// <remarks>
    /// The execution-order trick: this script's <c>Start()</c> runs after every
    /// MonoBehaviour's <c>Awake()</c>, which is exactly when self-registration
    /// has finished. We intentionally avoid <c>RuntimeInitializeOnLoadMethod</c>
    /// because we want managers to be inspectable in the Editor.
    /// </remarks>
    [DefaultExecutionOrder(10000)] // run after every manager's Awake
    public class Bootstrapper : MonoBehaviour
    {
        public static event System.Action OnBooted;

        [Tooltip("First additive scene to load once managers are ready.")]
        public string firstSceneKey = "Welcome";

        [Tooltip("Skip auto-loading the first scene (useful for tests).")]
        public bool autoLoadFirstScene = true;

        void Start()
        {
            if (!Services.IsBooted)
            {
                Debug.LogError("[Bootstrapper] One or more managers failed to register. " +
                               "Add the missing manager prefab to the Core scene.");
                LogMissing();
                return;
            }

            Debug.Log("[Bootstrapper] All managers registered. Booting.");
            OnBooted?.Invoke();

            if (autoLoadFirstScene && !string.IsNullOrEmpty(firstSceneKey))
            {
                Services.Scenes.LoadAdditive(firstSceneKey);
            }
        }

        void LogMissing()
        {
            // Required for boot
            if (Services.Scenes      == null) Debug.LogError("  missing (required): SceneFlowManager");
            if (Services.Users       == null) Debug.LogError("  missing (required): UserManager");
            if (Services.Preferences == null) Debug.LogError("  missing (required): PreferenceManager");
            if (Services.Session     == null) Debug.LogError("  missing (required): SessionManager");
            if (Services.Calibration == null) Debug.LogError("  missing (required): CalibrationManager");

            // Optional — added by later phases. Warn but don't block boot.
            if (Services.Sensors     == null) Debug.LogWarning("  not yet added (Phase 2): SensorInputManager");
            if (Services.Avatar      == null) Debug.LogWarning("  not yet added (Phase 3): AvatarDriver");
            if (Services.Feedback    == null) Debug.LogWarning("  not yet added (Phase 4): FeedbackManager");
            if (Services.Logging     == null) Debug.LogWarning("  not yet added (Phase 11): LoggingManager");
            if (Services.Audio       == null) Debug.LogWarning("  not yet added: AudioBus");
        }
    }
}
