using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRRehab.Core;

namespace VRRehab.Managers
{
    /// <summary>
    /// Loads and unloads additive Unity scenes on top of the persistent Core scene.
    /// Exactly one of these lives on [Managers] in Core.
    /// </summary>
    public class SceneFlowManager : MonoBehaviour
    {
        /// <summary>Fires when an additive scene has finished loading and its
        /// SceneEntry.OnEnter has run.</summary>
        public event Action<string> OnSceneReady;

        /// <summary>Fires immediately before a scene's SceneEntry.OnExit runs.</summary>
        public event Action<string> OnSceneClosing;

        readonly HashSet<string> _loaded = new HashSet<string>();

        void Awake()
        {
            Services.Scenes = this;
        }

        public bool IsLoaded(string sceneKey) => _loaded.Contains(sceneKey);

        public void LoadAdditive(string sceneKey)
        {
            if (string.IsNullOrEmpty(sceneKey))
            {
                Debug.LogError("[SceneFlow] LoadAdditive called with null/empty key");
                return;
            }
            if (_loaded.Contains(sceneKey))
            {
                Debug.LogWarning($"[SceneFlow] '{sceneKey}' already loaded; ignoring.");
                return;
            }
            StartCoroutine(LoadRoutine(sceneKey));
        }

        public void UnloadAdditive(string sceneKey)
        {
            if (!_loaded.Contains(sceneKey))
            {
                Debug.LogWarning($"[SceneFlow] '{sceneKey}' not loaded; ignoring unload.");
                return;
            }
            StartCoroutine(UnloadRoutine(sceneKey));
        }

        IEnumerator LoadRoutine(string sceneKey)
        {
            var op = SceneManager.LoadSceneAsync(sceneKey, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[SceneFlow] LoadSceneAsync returned null for '{sceneKey}'. " +
                               "Did you add it to Build Settings?");
                yield break;
            }
            while (!op.isDone) yield return null;

            _loaded.Add(sceneKey);

            var scene = SceneManager.GetSceneByName(sceneKey);
            var entry = FindSceneEntry(scene);
            if (entry == null)
            {
                Debug.LogError($"[SceneFlow] Scene '{sceneKey}' has no SceneEntry root. " +
                               "Add a script that derives from VRRehab.Core.SceneEntry.");
            }
            else
            {
                entry.OnEnter();
            }
            OnSceneReady?.Invoke(sceneKey);
        }

        IEnumerator UnloadRoutine(string sceneKey)
        {
            var scene = SceneManager.GetSceneByName(sceneKey);
            var entry = FindSceneEntry(scene);
            OnSceneClosing?.Invoke(sceneKey);
            if (entry != null) entry.OnExit();

            var op = SceneManager.UnloadSceneAsync(sceneKey);
            while (op != null && !op.isDone) yield return null;

            _loaded.Remove(sceneKey);
        }

        static SceneEntry FindSceneEntry(Scene scene)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                var entry = go.GetComponentInChildren<SceneEntry>(true);
                if (entry != null) return entry;
            }
            return null;
        }

        /// <summary>
        /// Convenience: unload the current scene and load another in one call.
        /// Useful for "Welcome → Calibration A" style transitions.
        /// </summary>
        public void Replace(string fromKey, string toKey)
        {
            StartCoroutine(ReplaceRoutine(fromKey, toKey));
        }

        IEnumerator ReplaceRoutine(string fromKey, string toKey)
        {
            if (_loaded.Contains(fromKey))
            {
                yield return UnloadRoutine(fromKey);
            }
            yield return LoadRoutine(toKey);
        }
    }
}
