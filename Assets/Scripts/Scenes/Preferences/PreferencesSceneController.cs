using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRRehab.Core;
using VRRehab.UI;

namespace VRRehab.Scenes.Preferences
{
    /// <summary>
    /// Scene 2 — User Preferences. Exposes the four feedback toggles, the
    /// language dropdown, and three "re-run calibration" buttons. Accessible
    /// at any time from the menu; does not interrupt an active session.
    /// </summary>
    public class PreferencesSceneController : SceneEntry
    {
        public override string SceneKey => "UserPreferences";

        [Header("Feedback toggles")]
        [SerializeField] Toggle textToggle;
        [SerializeField] Toggle audioToggle;
        [SerializeField] Toggle visualToggle;
        [SerializeField] Toggle hapticToggle;

        [Header("Locale")]
        [SerializeField] TMP_Dropdown languageDropdown;   // 0 = en, 1 = fr

        [Header("Recalibration")]
        [SerializeField] Button rerunBiasButton;
        [SerializeField] Button rerunRomButton;

        [Header("Navigation")]
        [SerializeField] Button doneButton;
        [SerializeField] TMP_Text statusText;

        public override void OnEnter()
        {
            BindFromCurrent();
            textToggle  ?.OnToggled(v => Services.Preferences.SetTextOn(v));
            audioToggle ?.OnToggled(v => Services.Preferences.SetAudioOn(v));
            visualToggle?.OnToggled(v => Services.Preferences.SetVisualOn(v));
            hapticToggle?.OnToggled(v => Services.Preferences.SetHapticOn(v));
            languageDropdown?.OnChanged(i => Services.Preferences.SetLanguage(i == 1 ? "fr" : "en"));

            rerunBiasButton?.OnClick(() => Services.Scenes.Replace(SceneKey, "Calibration_A"));
            rerunRomButton ?.OnClick(() => Services.Scenes.Replace(SceneKey, "Calibration_B"));
            doneButton     ?.OnClick(() => Services.Scenes.Replace(SceneKey, "Welcome"));

            SetStatus("Adjust preferences and tap Done.");
        }

        public override void OnExit()
        {
            // UiBindings.OnX above already calls RemoveAllListeners — explicit
            // cleanup here is paranoia in case someone wires up extra listeners.
            textToggle  ?.onValueChanged.RemoveAllListeners();
            audioToggle ?.onValueChanged.RemoveAllListeners();
            visualToggle?.onValueChanged.RemoveAllListeners();
            hapticToggle?.onValueChanged.RemoveAllListeners();
            languageDropdown?.onValueChanged.RemoveAllListeners();
            rerunBiasButton?.onClick.RemoveAllListeners();
            rerunRomButton ?.onClick.RemoveAllListeners();
            doneButton     ?.onClick.RemoveAllListeners();
        }

        void BindFromCurrent()
        {
            var p = Services.Preferences.Current;
            if (textToggle   != null) textToggle.isOn   = p.textOn;
            if (audioToggle  != null) audioToggle.isOn  = p.audioOn;
            if (visualToggle != null) visualToggle.isOn = p.visualOn;
            if (hapticToggle != null) hapticToggle.isOn = p.hapticOn;
            if (languageDropdown != null) languageDropdown.value = p.language == "fr" ? 1 : 0;
        }

        void SetStatus(string msg) { if (statusText != null) statusText.text = msg; }
    }
}
