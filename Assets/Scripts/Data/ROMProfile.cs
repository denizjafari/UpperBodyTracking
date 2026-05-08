using System;
using System.Collections.Generic;

namespace VRRehab.Data
{
    /// <summary>
    /// Per-joint ROM result. Mirrors the design doc's RomJointEntry.
    /// </summary>
    [Serializable]
    public class RomJointEntry
    {
        public string joint;        // e.g. "shoulder_flexion"
        public float min;
        public float max;
        public bool complete;
        public int retries;
        public string recordedAtIso;

        public float Range => max - min;
    }

    /// <summary>
    /// Full ROM calibration result for a user. Persisted inside
    /// <c>calibration.json</c> alongside the IMU bias flag.
    /// </summary>
    [Serializable]
    public class ROMProfile
    {
        public List<RomJointEntry> entries = new List<RomJointEntry>();
        public string calibratedAtIso;

        public RomJointEntry Get(string joint)
        {
            return entries.Find(e => e.joint == joint);
        }
    }

    /// <summary>
    /// Combined calibration state. Single JSON file, one per user.
    /// </summary>
    [Serializable]
    public class CalibrationState
    {
        public bool imuBiasComplete = false;
        public string imuBiasCompletedAtIso;
        public ROMProfile rom = new ROMProfile();
    }

    /// <summary>
    /// Canonical joint identifiers used in UDP packets, calibration files,
    /// and game input maps. Keep this list in sync with the Raspberry Pi.
    /// </summary>
    public static class JointKeys
    {
        public const string ShoulderFlexion   = "shoulder_flexion";
        public const string ShoulderAbduction = "shoulder_abduction";
        public const string ShoulderRotation  = "shoulder_rotation";
        public const string ElbowFlexion      = "elbow_flexion";
        public const string WristPronation    = "wrist_pronation";

        /// <summary>Order used by Calibration B (ROM scene).</summary>
        public static readonly string[] CalibrationOrder = new[]
        {
            ShoulderAbduction,
            ShoulderFlexion,
            ElbowFlexion,
            ShoulderRotation,
            WristPronation
        };
    }
}
