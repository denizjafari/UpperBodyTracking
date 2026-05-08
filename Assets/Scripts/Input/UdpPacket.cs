using System;
using UnityEngine;

namespace VRRehab.Input
{
    /// <summary>
    /// Compensation block embedded in every UDP packet (design §4.3).
    /// </summary>
    [Serializable]
    public class CompensationInfo
    {
        public bool detected;
        public string type;      // e.g. "trunk_lean", "shoulder_shrug" (RPi enum)
        public float severity;   // 0..1
    }

    /// <summary>
    /// Wire format spoken by the Raspberry Pi pipeline (design §4.1).
    /// One JSON object per UDP datagram, one joint per packet.
    /// </summary>
    [Serializable]
    public class UdpPacket
    {
        public long ts_ms;
        public string joint;
        public float angle_deg;
        public bool valid;
        public CompensationInfo compensation;

        public static UdpPacket FromJson(string json)
        {
            try { return JsonUtility.FromJson<UdpPacket>(json); }
            catch (Exception) { return null; }
        }
    }
}
