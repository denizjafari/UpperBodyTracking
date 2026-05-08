using System.Collections.Generic;
using UnityEngine;
using VRRehab.Core;
using VRRehab.Managers;

namespace VRRehab.Scenes.Calibration
{
    /// <summary>
    /// Plays pre-baked TTS clips for calibration: countdown numbers, GO cue,
    /// done beep, retry prompt, and per-joint instruction clips.
    /// File layout matches design §3.8:
    ///   Audio/Calibration/&lt;lang&gt;/numbers/{1..5,go}.wav
    ///   Audio/Calibration/&lt;lang&gt;/cues/{stop_beep,done,retry_prompt}.wav
    ///   Audio/Calibration/&lt;lang&gt;/instructions/&lt;jointKey&gt;.wav
    /// </summary>
    public class CalibrationAudioManager : MonoBehaviour
    {
        [System.Serializable] public class Clip { public string key; public AudioClip clip; }

        [Header("Numbers (1..5, go)")]
        [SerializeField] List<Clip> numberClips = new List<Clip>(); // keys: "1","2","3","4","5","go"

        [Header("Cues")]
        [SerializeField] AudioClip stopBeep;
        [SerializeField] AudioClip done;
        [SerializeField] AudioClip retryPrompt;

        [Header("Per-joint instructions")]
        [SerializeField] List<Clip> instructionClips = new List<Clip>(); // key=joint key

        readonly Dictionary<string, AudioClip> _numbers = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, AudioClip> _instructions = new Dictionary<string, AudioClip>();

        AudioClip _lastInstruction;

        void Awake()
        {
            foreach (var c in numberClips)      if (!string.IsNullOrEmpty(c.key)) _numbers[c.key] = c.clip;
            foreach (var c in instructionClips) if (!string.IsNullOrEmpty(c.key)) _instructions[c.key] = c.clip;
        }

        public void PlayInstruction(string jointKey)
        {
            if (!_instructions.TryGetValue(jointKey, out var clip) || clip == null)
            {
                Debug.LogWarning($"[CalibAudio] No instruction clip for '{jointKey}'");
                return;
            }
            _lastInstruction = clip;
            Services.Audio?.PlayOneShot(clip, AudioBus.Group.Voice);
        }

        public void ReplayInstruction()
        {
            if (_lastInstruction != null) Services.Audio?.PlayOneShot(_lastInstruction, AudioBus.Group.Voice);
        }

        public void PlayCountdownNumber(int number)
        {
            string key = number.ToString();
            if (_numbers.TryGetValue(key, out var c) && c != null)
                Services.Audio?.PlayOneShot(c, AudioBus.Group.Voice);
        }

        public void PlayGoCue()
        {
            if (_numbers.TryGetValue("go", out var c) && c != null)
                Services.Audio?.PlayOneShot(c, AudioBus.Group.Voice);
        }

        public void PlayStopCue()
        {
            if (stopBeep != null) Services.Audio?.PlayOneShot(stopBeep, AudioBus.Group.Sfx);
            if (done     != null) Services.Audio?.PlayOneShot(done,     AudioBus.Group.Voice);
        }

        public void PlayRetryPrompt()
        {
            if (retryPrompt != null) Services.Audio?.PlayOneShot(retryPrompt, AudioBus.Group.Voice);
        }
    }
}
