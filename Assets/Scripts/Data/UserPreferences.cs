using System;

namespace VRRehab.Data
{
    /// <summary>
    /// Feedback channel toggles + locale. Persisted as <c>user_preferences.json</c>
    /// and mirrored to the SQLite <c>user_preferences</c> table (see Phase 11).
    /// All flags default to <c>true</c> so a fresh patient gets the full feedback bouquet.
    /// </summary>
    [Serializable]
    public class UserPreferences
    {
        public bool textOn   = true;
        public bool audioOn  = true;
        public bool visualOn = true;
        public bool hapticOn = true;

        public string language = "en";

        // Optional per-channel volume (0..1). Audio bus reads this at session start.
        public float audioVolume   = 0.85f;
        public float hapticIntensity = 0.7f;
    }
}
