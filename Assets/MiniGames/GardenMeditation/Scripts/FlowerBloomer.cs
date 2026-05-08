using UnityEngine;

namespace VRRehab.MiniGames.GardenMeditation
{
    /// <summary>
    /// Visualises the breathing cycle as a flower that scales / rotates with
    /// breath tension and grows incrementally per completed cycle.
    /// </summary>
    /// <remarks>
    /// Designed to be primitive-friendly: drop a sphere in for `bloomTransform`
    /// and the game looks reasonable on day one. Swap in a real flower mesh
    /// (Quaternius nature kit etc) without changing the script.
    /// </remarks>
    public class FlowerBloomer : MonoBehaviour
    {
        [SerializeField] BreathingPacer pacer;
        [SerializeField] Transform bloomTransform;

        [Header("Tension → scale")]
        [Tooltip("Scale at zero tension (full exhale).")]
        public float minScale = 0.4f;
        [Tooltip("Scale at full tension (held inhale).")]
        public float maxScale = 1.0f;

        [Header("Cycle growth — flower keeps growing across the session.")]
        public float perCycleGrowth = 0.08f;
        public float maxOverallScale = 3.0f;

        [Header("Optional rotation (gentle ambient motion).")]
        public float rotateDegPerSec = 6f;

        float _baseScale = 1f;

        void OnEnable()
        {
            if (pacer != null) pacer.OnFullCycle += OnCycle;
        }

        void OnDisable()
        {
            if (pacer != null) pacer.OnFullCycle -= OnCycle;
        }

        void OnCycle()
        {
            _baseScale = Mathf.Min(maxOverallScale, _baseScale + perCycleGrowth);
        }

        void Update()
        {
            if (bloomTransform == null || pacer == null) return;
            float pulsing = Mathf.Lerp(minScale, maxScale, pacer.BreathTension);
            bloomTransform.localScale = Vector3.one * (_baseScale * pulsing);
            bloomTransform.Rotate(0f, rotateDegPerSec * Time.deltaTime, 0f, Space.Self);
        }
    }
}
