using System;
using System.IO;
using UnityEngine;
using VRRehab.Core;
using VRRehab.Data;

namespace VRRehab.Managers
{
    /// <summary>
    /// Loads / saves the active user's <see cref="UserPreferences"/>. Listens
    /// to UserManager.OnUserChanged so a profile switch triggers a reload.
    /// </summary>
    public class PreferenceManager : MonoBehaviour
    {
        public event Action<UserPreferences> OnPreferencesChanged;

        public UserPreferences Current { get; private set; } = new UserPreferences();

        void Awake() => Services.Preferences = this;

        void Start()
        {
            if (Services.Users != null)
                Services.Users.OnUserChanged += _ => LoadForCurrentUser();
        }

        public void LoadForCurrentUser()
        {
            var u = Services.Users?.Current;
            if (u == null) return;

            var path = Services.Users.PreferencesPath(u.id);
            if (File.Exists(path))
            {
                try
                {
                    Current = JsonUtility.FromJson<UserPreferences>(File.ReadAllText(path))
                              ?? new UserPreferences();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Preferences] Failed to read {path}: {e.Message}; using defaults.");
                    Current = new UserPreferences();
                }
            }
            else
            {
                Current = new UserPreferences { language = u.language };
                Save();
            }
            OnPreferencesChanged?.Invoke(Current);
        }

        public void Save()
        {
            var u = Services.Users?.Current;
            if (u == null) return;
            File.WriteAllText(Services.Users.PreferencesPath(u.id),
                              JsonUtility.ToJson(Current, true));
        }

        // Convenience setters that also trigger persistence.
        public void SetTextOn  (bool v) { Current.textOn   = v; Save(); OnPreferencesChanged?.Invoke(Current); }
        public void SetAudioOn (bool v) { Current.audioOn  = v; Save(); OnPreferencesChanged?.Invoke(Current); }
        public void SetVisualOn(bool v) { Current.visualOn = v; Save(); OnPreferencesChanged?.Invoke(Current); }
        public void SetHapticOn(bool v) { Current.hapticOn = v; Save(); OnPreferencesChanged?.Invoke(Current); }
        public void SetLanguage(string lang) { Current.language = lang; Save(); OnPreferencesChanged?.Invoke(Current); }
    }
}
