using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRRehab.Core;
using VRRehab.Data;
using VRRehab.UI;

namespace VRRehab.Scenes.SessionSetup
{
    /// <summary>
    /// Scene 6 — Session Setup. Lets the clinician/patient choose today's
    /// exercise routine: which mini-games to run, in what order, at what
    /// difficulty, and for how long. Output is a <see cref="SessionConfig"/>
    /// passed to <see cref="Managers.SessionManager"/> via <c>StartSession</c>.
    /// </summary>
    public class SessionSetupSceneController : SceneEntry
    {
        public override string SceneKey => "SessionSetup";

        [System.Serializable]
        public class GameOption
        {
            public string gameKey;       // matches mini-game scene key, e.g. "FlappyBird"
            public string displayName;
            public int defaultDifficulty = 2;
            public int defaultDurationSeconds = 180;
            [Tooltip("Joint keys this game requires from ROM calibration to be available.")]
            public string[] requiredJoints;
        }

        [Header("Available games (assign in inspector)")]
        [SerializeField] List<GameOption> games = new List<GameOption>();

        [Header("UI")]
        [SerializeField] Transform optionListContainer;  // VerticalLayoutGroup parent
        [SerializeField] GameObject optionRowPrefab;     // a row with toggle + name + difficulty + duration TMPs
        [SerializeField] Button beginButton;
        [SerializeField] Button cancelButton;
        [SerializeField] TMP_Text statusText;

        readonly List<GameOptionRow> _rows = new List<GameOptionRow>();

        public override void OnEnter()
        {
            BuildRows();
            beginButton ?.OnClick(OnBeginClicked);
            cancelButton?.OnClick(() => Services.Scenes.Replace(SceneKey, "Welcome"));
            SetStatus("Pick exercises for today's session, then tap Begin.");
        }

        public override void OnExit()
        {
            beginButton ?.onClick.RemoveAllListeners();
            cancelButton?.onClick.RemoveAllListeners();
            ClearRows();
        }

        void BuildRows()
        {
            ClearRows();
            if (optionListContainer == null || optionRowPrefab == null) return;
            foreach (var g in games)
            {
                var go = Instantiate(optionRowPrefab, optionListContainer);
                var row = go.GetComponent<GameOptionRow>();
                if (row == null)
                {
                    Debug.LogError("[SessionSetup] optionRowPrefab missing GameOptionRow component");
                    Destroy(go);
                    continue;
                }
                row.Bind(g, IsAvailable(g));
                _rows.Add(row);
            }
        }

        void ClearRows()
        {
            foreach (var r in _rows) if (r != null) Destroy(r.gameObject);
            _rows.Clear();
        }

        bool IsAvailable(GameOption g)
        {
            if (g.requiredJoints == null || g.requiredJoints.Length == 0) return true;
            var rom = Services.Calibration?.GetRomProfile();
            if (rom == null) return false;
            foreach (var j in g.requiredJoints)
            {
                var entry = rom.Get(j);
                if (entry == null || !entry.complete) return false;
            }
            return true;
        }

        void OnBeginClicked()
        {
            var u = Services.Users?.Current;
            if (u == null)
            {
                SetStatus("No user loaded.");
                return;
            }
            var config = SessionConfig.NewFor(u.id);
            foreach (var row in _rows)
            {
                if (!row.IsSelected) continue;
                config.exercises.Add(new ExerciseEntry
                {
                    game = row.Option.gameKey,
                    difficulty = row.Difficulty,
                    durationSeconds = row.DurationSeconds
                });
            }
            if (config.exercises.Count == 0)
            {
                SetStatus("Pick at least one exercise.");
                return;
            }
            Services.Session.StartSession(config);
            // SessionManager raises OnExerciseStarted — wire that to scene loading
            // in Phase 9 (mini-games). For now we just unload ourselves.
            Services.Scenes.UnloadAdditive(SceneKey);
        }

        void SetStatus(string s) { if (statusText != null) statusText.text = s; }
    }
}
