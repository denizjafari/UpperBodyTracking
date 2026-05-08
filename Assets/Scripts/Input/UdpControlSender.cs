using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace VRRehab.Input
{
    /// <summary>
    /// One-shot UDP sender for control messages going Unity → Raspberry Pi.
    /// The RPi listens on a separate port from the data stream.
    /// </summary>
    /// <remarks>
    /// Sent commands so far:
    ///   { "cmd": "start_bias" }
    ///   { "cmd": "stop_bias" }
    ///   { "cmd": "begin_recording", "joint": "shoulder_flexion" }
    ///   { "cmd": "end_recording" }
    /// The RPi acknowledges via the regular data-stream UDP port using
    /// "joint" = "_ack" packets (or whichever convention you settle on).
    /// </remarks>
    public class UdpControlSender : MonoBehaviour
    {
        [Tooltip("Raspberry Pi IP. Use the RPi's LAN address.")]
        public string targetHost = "192.168.1.50";

        [Tooltip("Port on the RPi that listens for control messages.")]
        public int targetPort = 5006;

        UdpClient _client;
        IPEndPoint _endpoint;

        void Awake()
        {
            try
            {
                _client = new UdpClient();
                _endpoint = new IPEndPoint(IPAddress.Parse(targetHost), targetPort);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UdpControl] Init failed: {e.Message}");
            }
        }

        void OnDestroy()
        {
            try { _client?.Close(); } catch { }
            _client = null;
        }

        public void Send(string json)
        {
            if (_client == null) return;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                _client.Send(bytes, bytes.Length, _endpoint);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[UdpControl] Send failed: {e.Message}");
            }
        }

        // Convenience helpers
        public void SendStartBias()         => Send("{\"cmd\":\"start_bias\"}");
        public void SendStopBias()          => Send("{\"cmd\":\"stop_bias\"}");
        public void SendBeginRecording(string joint) => Send($"{{\"cmd\":\"begin_recording\",\"joint\":\"{joint}\"}}");
        public void SendEndRecording()      => Send("{\"cmd\":\"end_recording\"}");
    }
}
