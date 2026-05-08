using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRRehab.Core;
using VRRehab.Data;
using VRRehab.UI;

namespace VRRehab.Scenes.Welcome
{
    /// <summary>
    /// Scene 1 — Welcome &amp; User Profile. Lists existing profiles, lets the
    /// patient pick one or create a new one, then routes to either Calibration A
    /// (first session) or directly to Session Setup.
    /// </summary>
    /// <remarks>
    /// Routing rule (per design):
    ///   - First session for this user → load <c>Calibration_A</c>.
    ///   - Otherwise → load <c>SessionSetup</c>.
    /// </remarks>
    public class WelcomeSceneController : SceneEntry
    {
        public override string SceneKey => "Welcome";

        [Header("UI references")]
        [SerializeField] TMP_Dropdown profileDropdown;
        [SerializeField] TMP_InputField newProfileNameField;
        [SerializeField] TMP_Dropdown   newProfileLanguage; // 0=en, 1=fr
        [SerializeField] Button createButton;
        [SerializeField] Button continueButton;
        [SerializeField] TMP_Text statusText;

        readonly List<UserProfile> _profiles = new List<UserProfile>();

        public override void OnEnter()
        {
            RefreshProfiles();
            createButton?.OnClick(OnCreateClicked);
            continueButton?.OnClick(OnContinueClicked);
            profileDropdown?.OnChanged(OnProfileSelected);
            SetStatus(_profiles.Count == 0
                ? "No profile yet — type a name and tap Create."
                : "Pick a profile or create a new one.");
        }

        public override void OnExit()
        {
            createButton?.onClick.RemoveAllListeners();
            continueButton?.onClick.RemoveAllListeners();
            profileDropdown?.onValueChanged.RemoveAllListeners();
        }

        void RefreshProfiles()
        {
            _profiles.Clear();
            _profiles.AddRange(Services.Users.ListAllUsers().OrderBy(p => p.displayName));

            if (profileDropdown != null)
            {
                profileDropdown.ClearOptions();
                if (_profiles.Count == 0)
                {
                    profileDropdown.AddOptions(new List<string> { "(no profiles)" });
                    profileDropdown.interactable = false;
                }
                else
                {
                    profileDropdown.AddOptions(_profiles.Select(p => p.displayName).ToList());
                    profileDropdown.interactable = true;
                }
            }
        }

        void OnProfileSelected(int idx)
        {
            if (idx < 0 || idx >= _profiles.Count) return;
            Services.Users.LoadUser(_profiles[idx].id);
            SetStatus($"Loaded profile '{_profiles[idx].displayName}'.");
        }

        void OnCreateClicked()
        {
            var name = newProfileNameField != null ? newProfileNameField.text?.Trim() : null;
            if (string.IsNullOrEmpty(name))
            {
                SetStatus("Enter a name first.");
                return;
            }
            string lang = (newProfileLanguage != null && newProfileLanguage.value == 1) ? "fr" : "en";
            var p = Services.Users.CreateUser(name, language: lang);
            Services.Preferences.LoadForCurrentUser();
            Services.Calibration.LoadForCurrentUser();
            RefreshProfiles();
            SetStatus($"Created profile '{p.displayName}'. Tap Continue to begin calibration.");
        }

        void OnContinueClicked()
        {
            if (Services.Users.Current == null)
            {
                SetStatus("Pick or create a profile first.");
                return;
            }

            // First-session detection: an empty calibration state means we go to Calib A.
            string nextScene = Services.Calibration.State.imuBiasComplete
                               && !Services.Calibration.IsRomCalibrationRequired()
                ? "SessionSetup"
                : "Calibration_A";

            Services.Scenes.Replace(SceneKey, nextScene);
        }

        void SetStatus(string msg) { if (statusText != null) statusText.text = msg; }
    }
}
