using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRRehab.UI;

namespace VRRehab.Scenes.SessionSetup
{
    /// <summary>
    /// Single row inside the session-setup list: a toggle, the game name, +/- buttons
    /// for difficulty (1..5) and a slider for duration (60..600 s).
    /// </summary>
    public class GameOptionRow : MonoBehaviour
    {
        [SerializeField] Toggle  selectToggle;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text difficultyLabel;
        [SerializeField] TMP_Text durationLabel;
        [SerializeField] Button   diffMinus;
        [SerializeField] Button   diffPlus;
        [SerializeField] Button   durMinus;
        [SerializeField] Button   durPlus;
        [SerializeField] TMP_Text unavailableHint;

        public SessionSetupSceneController.GameOption Option { get; private set; }
        public bool IsSelected => selectToggle != null && selectToggle.isOn;

        public int Difficulty { get; private set; } = 2;
        public int DurationSeconds { get; private set; } = 180;

        public void Bind(SessionSetupSceneController.GameOption opt, bool available)
        {
            Option = opt;
            if (nameLabel != null) nameLabel.text = opt.displayName;
            Difficulty = opt.defaultDifficulty;
            DurationSeconds = opt.defaultDurationSeconds;
            UpdateLabels();

            if (selectToggle != null)
            {
                selectToggle.interactable = available;
                selectToggle.isOn = false;
            }
            if (unavailableHint != null) unavailableHint.gameObject.SetActive(!available);

            diffMinus?.OnClick(() => { Difficulty = Mathf.Clamp(Difficulty - 1, 1, 5); UpdateLabels(); });
            diffPlus ?.OnClick(() => { Difficulty = Mathf.Clamp(Difficulty + 1, 1, 5); UpdateLabels(); });
            durMinus ?.OnClick(() => { DurationSeconds = Mathf.Clamp(DurationSeconds - 30, 60, 600); UpdateLabels(); });
            durPlus  ?.OnClick(() => { DurationSeconds = Mathf.Clamp(DurationSeconds + 30, 60, 600); UpdateLabels(); });
        }

        void UpdateLabels()
        {
            if (difficultyLabel != null) difficultyLabel.text = $"Lv {Difficulty}";
            if (durationLabel   != null) durationLabel.text   = $"{DurationSeconds / 60}:{DurationSeconds % 60:D2}";
        }
    }
}
