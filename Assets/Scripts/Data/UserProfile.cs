using System;

namespace VRRehab.Data
{
    /// <summary>
    /// Patient identity. One per user folder under <c>Users/&lt;id&gt;/</c>.
    /// Persisted as <c>profile.json</c>.
    /// </summary>
    [Serializable]
    public class UserProfile
    {
        public string id;
        public string displayName;
        public string dateOfBirthIso;        // ISO-8601 date, e.g. "1962-03-14"
        public string dominantHand = "Right"; // "Left" | "Right"
        public string language = "en";        // BCP-47 short code
        public string createdAtIso;
        public string lastSessionIso;

        public static UserProfile NewWith(string id, string displayName)
        {
            return new UserProfile
            {
                id = id,
                displayName = displayName,
                createdAtIso = DateTime.UtcNow.ToString("o")
            };
        }
    }
}
