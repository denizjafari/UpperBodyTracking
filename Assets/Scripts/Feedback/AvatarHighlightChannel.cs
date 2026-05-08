using System.Collections;
using UnityEngine;
using VRRehab.Input;

namespace VRRehab.Feedback
{
    /// <summary>
    /// Flashes a body region red on the avatar by tweaking a MaterialPropertyBlock
    /// on its Renderer. No per-frame allocations — the block is reused.
    /// </summary>
    public class AvatarHighlightChannel : MonoBehaviour, IFeedbackChannel
    {
        [SerializeField] Renderer avatarRenderer;
        [SerializeField] string colorPropertyName = "_BaseColor"; // URP/Lit
        [SerializeField] float flashSeconds = 0.6f;

        public bool Enabled { get; set; } = true;

        MaterialPropertyBlock _block;
        int _propId;
        Coroutine _running;

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            _propId = Shader.PropertyToID(colorPropertyName);
        }

        public void Fire(UdpPacket pkt, float severity, string message)
        {
            if (!Enabled || avatarRenderer == null) return;
            if (severity < 0.66f) return; // only the loudest tier highlights
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            avatarRenderer.GetPropertyBlock(_block);
            _block.SetColor(_propId, new Color(1f, 0.25f, 0.2f, 1f));
            avatarRenderer.SetPropertyBlock(_block);
            yield return new WaitForSeconds(flashSeconds);
            avatarRenderer.GetPropertyBlock(_block);
            _block.SetColor(_propId, Color.white);
            avatarRenderer.SetPropertyBlock(_block);
            _running = null;
        }
    }
}
