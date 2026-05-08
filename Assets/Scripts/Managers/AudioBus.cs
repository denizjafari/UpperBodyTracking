using UnityEngine;
using UnityEngine.Audio;
using VRRehab.Core;

namespace VRRehab.Managers
{
    /// <summary>
    /// Centralized audio access. Holds the project's AudioMixer and exposes
    /// per-group AudioSources so any system can play one-shots without
    /// instantiating its own listener / source pair.
    /// </summary>
    public class AudioBus : MonoBehaviour
    {
        [SerializeField] AudioMixer mixer;
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioSource ambienceSource;

        void Awake() => Services.Audio = this;

        public enum Group { Voice, Sfx, Ambience }

        public void PlayOneShot(AudioClip clip, Group group = Group.Sfx, float volume = 1f)
        {
            if (clip == null) return;
            var src = group switch
            {
                Group.Voice    => voiceSource,
                Group.Ambience => ambienceSource,
                _              => sfxSource,
            };
            if (src == null)
            {
                Debug.LogWarning($"[AudioBus] No AudioSource assigned for group {group}; falling back to PlayClipAtPoint.");
                AudioSource.PlayClipAtPoint(clip, Camera.main ? Camera.main.transform.position : Vector3.zero, volume);
                return;
            }
            src.PlayOneShot(clip, volume);
        }

        public void StopAmbience() { if (ambienceSource != null) ambienceSource.Stop(); }
    }
}
