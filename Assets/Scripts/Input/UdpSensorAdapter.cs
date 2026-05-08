using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace VRRehab.Input
{
    /// <summary>
    /// Listens for JSON UDP packets from the Raspberry Pi pipeline.
    /// Receives on a background thread, marshals onto the main thread.
    /// </summary>
    /// <remarks>
    /// Threading model:
    ///   - <see cref="ReceiveLoop"/> runs on a dedicated <see cref="Thread"/>
    ///     (not <see cref="System.Threading.Tasks.Task"/>) — Quest's mono runtime
    ///     handles long-lived background threads more predictably than the thread
    ///     pool, especially across app pause/resume.
    ///   - Packets land in a <see cref="ConcurrentQueue{T}"/>; <see cref="Pump"/>
    ///     drains it on the main thread each <see cref="MonoBehaviour.Update"/>.
    ///   - We drop packets older than <see cref="staleMs"/> to avoid replaying
    ///     a startup buffer burst.
    /// </remarks>
    public class UdpSensorAdapter : MonoBehaviour, ISensorInputAdapter
    {
        public TrackingMode Mode => TrackingMode.IMU_UDP;
        public event Action<UdpPacket> OnPacket;

        [Tooltip("UDP port to bind. Default 5005 — override via network.json.")]
        public int port = 5005;

        [Tooltip("Drop packets older than this many ms when measured against system time.")]
        public int staleMs = 100;

        UdpClient _client;
        Thread _thread;
        volatile bool _running;

        readonly ConcurrentQueue<UdpPacket> _inbox = new ConcurrentQueue<UdpPacket>();

        public void Begin()
        {
            if (_running) return;
            try
            {
                _client = new UdpClient(port);
                _client.Client.ReceiveTimeout = 500; // ms
            }
            catch (SocketException e)
            {
                Debug.LogError($"[UdpSensor] Failed to bind UDP port {port}: {e.Message}");
                return;
            }

            _running = true;
            _thread = new Thread(ReceiveLoop) { IsBackground = true, Name = "UdpSensorAdapter" };
            _thread.Start();
            Debug.Log($"[UdpSensor] Listening on UDP {port}");
        }

        public void End()
        {
            _running = false;
            try { _client?.Close(); } catch { /* ignore */ }
            try { _thread?.Join(200); } catch { /* ignore */ }
            _client = null;
            _thread = null;
        }

        void OnDisable() => End();

        void Update() => Pump();

        void Pump()
        {
            while (_inbox.TryDequeue(out var pkt))
            {
                OnPacket?.Invoke(pkt);
            }
        }

        void ReceiveLoop()
        {
            var any = new IPEndPoint(IPAddress.Any, 0);
            while (_running)
            {
                try
                {
                    var bytes = _client.Receive(ref any);
                    if (bytes == null || bytes.Length == 0) continue;
                    var json = Encoding.UTF8.GetString(bytes);
                    var pkt = UdpPacket.FromJson(json);
                    if (pkt == null || string.IsNullOrEmpty(pkt.joint)) continue;

                    // Drop obviously stale packets (system clock comparison).
                    var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (pkt.ts_ms > 0 && nowMs - pkt.ts_ms > staleMs) continue;

                    _inbox.Enqueue(pkt);
                }
                catch (SocketException) { /* timeout — loop */ }
                catch (ObjectDisposedException) { break; }
                catch (Exception e) { Debug.LogWarning($"[UdpSensor] {e.Message}"); }
            }
        }
    }
}
