using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRRehab.Core;
using VRRehab.Data;

namespace VRRehab.Managers
{
    /// <summary>
    /// Owns the active user profile, the list of all users on disk, and the
    /// per-user folder layout (<c>Users/&lt;id&gt;/</c>).
    /// </summary>
    public class UserManager : MonoBehaviour
    {
        public event Action<UserProfile> OnUserChanged;

        public UserProfile Current { get; private set; }
        public string CurrentUserID => Current?.id;

        string UsersRoot => Path.Combine(Application.persistentDataPath, "Users");

        void Awake()
        {
            Services.Users = this;
            Directory.CreateDirectory(UsersRoot);
        }

        public IEnumerable<UserProfile> ListAllUsers()
        {
            if (!Directory.Exists(UsersRoot)) yield break;
            foreach (var dir in Directory.GetDirectories(UsersRoot))
            {
                var profilePath = Path.Combine(dir, "profile.json");
                if (!File.Exists(profilePath)) continue;
                UserProfile p = null;
                try { p = JsonUtility.FromJson<UserProfile>(File.ReadAllText(profilePath)); }
                catch (Exception e) { Debug.LogWarning($"[UserManager] Skipping '{dir}': {e.Message}"); }
                if (p != null) yield return p;
            }
        }

        public bool LoadUser(string id)
        {
            var path = ProfilePath(id);
            if (!File.Exists(path))
            {
                Debug.LogError($"[UserManager] No profile at {path}");
                return false;
            }
            Current = JsonUtility.FromJson<UserProfile>(File.ReadAllText(path));
            OnUserChanged?.Invoke(Current);
            Debug.Log($"[UserManager] Loaded user '{Current.id}' ({Current.displayName})");
            return true;
        }

        public UserProfile CreateUser(string displayName, string dateOfBirthIso = null,
                                      string dominantHand = "Right", string language = "en")
        {
            var id = Guid.NewGuid().ToString("N").Substring(0, 8);
            var p = UserProfile.NewWith(id, displayName);
            p.dateOfBirthIso = dateOfBirthIso;
            p.dominantHand   = dominantHand;
            p.language       = language;

            EnsureUserFolder(id);
            File.WriteAllText(ProfilePath(id), JsonUtility.ToJson(p, true));

            // Seed default preferences and an empty calibration file.
            File.WriteAllText(PreferencesPath(id),
                JsonUtility.ToJson(new UserPreferences { language = language }, true));
            File.WriteAllText(CalibrationPath(id),
                JsonUtility.ToJson(new CalibrationState(), true));

            Current = p;
            OnUserChanged?.Invoke(Current);
            return p;
        }

        public void SaveCurrent()
        {
            if (Current == null) return;
            Current.lastSessionIso = DateTime.UtcNow.ToString("o");
            File.WriteAllText(ProfilePath(Current.id), JsonUtility.ToJson(Current, true));
        }

        // ---- path helpers (also used by Preference/Calibration managers) -------

        public string UserFolder(string id) => Path.Combine(UsersRoot, id);
        public string ProfilePath(string id)     => Path.Combine(UserFolder(id), "profile.json");
        public string PreferencesPath(string id) => Path.Combine(UserFolder(id), "user_preferences.json");
        public string CalibrationPath(string id) => Path.Combine(UserFolder(id), "calibration.json");
        public string SessionDbPath(string id)   => Path.Combine(UserFolder(id), "sessions.db");

        public void EnsureUserFolder(string id)
        {
            Directory.CreateDirectory(UserFolder(id));
            Directory.CreateDirectory(Path.Combine(UserFolder(id), "exports"));
            Directory.CreateDirectory(Path.Combine(UserFolder(id), "logs"));
        }
    }
}
