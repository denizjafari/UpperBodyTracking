using UnityEngine;
using VRRehab.Core;
using VRRehab.Data;

namespace VRRehab.Managers
{
    /// <summary>
    /// Listens to <see cref="SessionManager.OnExerciseStarted"/> and loads the
    /// matching mini-game scene additively. Acts as the bridge between the
    /// session queue and the per-game scene loading.
    /// </summary>
    /// <remarks>
    /// The contract: every mini-game scene name in Build Settings must equal
    /// the <c>ExerciseEntry.game</c> string. So an entry with
    /// <c>game = "GardenMeditation"</c> loads the scene named
    /// "GardenMeditation".
    /// </remarks>
    public class MiniGameRouter : MonoBehaviour
    {
        [Tooltip("Optional: scene to load between two mini-games (e.g. a 'rest' interstitial). Leave blank to load the next game directly.")]
        public string interstitialScene;

        void Start()
        {
            if (Services.Session != null)
            {
                Services.Session.OnExerciseStarted += HandleExerciseStarted;
                Services.Session.OnSessionEnded   += HandleSessionEnded;
            }
        }

        void OnDestroy()
        {
            if (Services.Session != null)
            {
                Services.Session.OnExerciseStarted -= HandleExerciseStarted;
                Services.Session.OnSessionEnded   -= HandleSessionEnded;
            }
        }

        void HandleExerciseStarted(ExerciseEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.game))
            {
                Debug.LogError("[MiniGameRouter] OnExerciseStarted with empty game key");
                return;
            }
            Services.Scenes.LoadAdditive(entry.game);
        }

        void HandleSessionEnded()
        {
            Services.Scenes.LoadAdditive("Welcome");
        }
    }
}
