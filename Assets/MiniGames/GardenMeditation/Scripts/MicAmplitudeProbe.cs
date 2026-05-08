using UnityEngine;

namespace VRRehab.MiniGames.GardenMeditation
{
    /// <summary>
    /// Optional input: samples the headset microphone and exposes a smoothed
    /// 0..1 amplitude. Lets the game softly reward audible exhalations.
    /// Quest 3S has stereo mics.
    /// </summary>
    /// <remarks>
    /// On Android, <c>Permission.RequestUserPermission</c> for RECORD_AUDIO is
    /// required at runtime (in addition to the manifest entry). We request it
    /// in <see cref="Start"/> for simplicity; production code may want a more
    /// graceful pre-flight UX.
    /// </remarks>
    public class MicAmplitudeProbe : MonoBehaviour
    {
        public bool   enableProbe = false;
        public float  smoothingHz = 6f;
        public int    sampleWindow = 256;

        AudioClip _clip;
        string _device;
        float[] _samples;

        public float Amplitude { get; private set; }

        void Start()
        {
            if (!enableProbe) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            }
#endif
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[MicProbe] No microphone available.");
                return;
            }
            _device = Microphone.devices[0];
            _clip = Microphone.Start(_device, true, 1, 16000);
            _samples = new float[sampleWindow];
        }

        void OnDestroy()
        {
            if (_clip != null && _device != null) Microphone.End(_device);
        }

        void Update()
        {
            if (!enableProbe || _clip == null) return;
            int pos = Microphone.GetPosition(_device);
            if (pos < sampleWindow) return;
            _clip.GetData(_samples, pos - sampleWindow);
            float sum = 0f;
            for (int i = 0; i < _samples.Length; i++) sum += _samples[i] * _samples[i];
            float instantaneous = Mathf.Sqrt(sum / _samples.Length);

            // Exponential smoothing toward the new sample.
            float a = 1f - Mathf.Exp(-smoothingHz * Time.deltaTime);
            Amplitude = Mathf.Lerp(Amplitude, instantaneous, a);
        }
    }
}
