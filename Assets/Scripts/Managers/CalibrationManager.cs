using System;
using System.IO;
using UnityEngine;
using VRRehab.Core;
using VRRehab.Data;

namespace VRRehab.Managers
{
    /// <summary>
    /// Owns the per-user <c>calibration.json</c>. Provides:
    /// <list type="bullet">
    ///   <item>IMU-bias state (Phase 6)</item>
    ///   <item>ROM profile load/save (Phase 7)</item>
    ///   <item>Trigger-condition evaluation (whether ROM scene should run)</item>
    /// </list>
    /// The actual ROM state machine lives in <c>RomCalibrationController</c>
    /// inside the Calibration B scene; this manager only persists the result.
    /// </summary>
    public class CalibrationManager : MonoBehaviour
    {
        public event Action OnCalibrationLoaded;
        public event Action<ROMProfile> OnRomCalibrationCompleted;

        public CalibrationState State { get; private set; } = new CalibrationState();

        void Awake() => Services.Calibration = this;

        void Start()
        {
            if (Services.Users != null)
                Services.Users.OnUserChanged += _ => LoadForCurrentUser();
        }

        public void LoadForCurrentUser()
        {
            var u = Services.Users?.Current;
            if (u == null) return;
            var path = Services.Users.CalibrationPath(u.id);
            if (File.Exists(path))
            {
                try
                {
                    State = JsonUtility.FromJson<CalibrationState>(File.ReadAllText(path))
                            ?? new CalibrationState();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Calibration] Failed to read {path}: {e.Message}; resetting.");
                    State = new CalibrationState();
                }
            }
            else State = new CalibrationState();
            OnCalibrationLoaded?.Invoke();
        }

        public void Save()
        {
            var u = Services.Users?.Current;
            if (u == null) return;
            File.WriteAllText(Services.Users.CalibrationPath(u.id),
                              JsonUtility.ToJson(State, true));
        }

        // ---------- IMU bias (Phase 6) ----------

        public void MarkImuBiasComplete()
        {
            State.imuBiasComplete = true;
            State.imuBiasCompletedAtIso = DateTime.UtcNow.ToString("o");
            Save();
        }

        // ---------- ROM (Phase 7) ----------

        /// <summary>
        /// Per design §3.4.1: the ROM scene runs on first session, on explicit
        /// recalibration request, or if the previous calibration is missing /
        /// marked invalid. Otherwise we reuse stored ROM values.
        /// </summary>
        public bool IsRomCalibrationRequired(bool explicitRequest = false)
        {
            if (explicitRequest) return true;
            if (State?.rom?.entries == null || State.rom.entries.Count == 0) return true;
            // Require all five canonical joints to be present and complete.
            foreach (var key in JointKeys.CalibrationOrder)
            {
                var entry = State.rom.Get(key);
                if (entry == null || !entry.complete) return true;
            }
            return false;
        }

        /// <summary>Phase 7's RomCalibrationController calls this once per joint.</summary>
        public void UpsertRomEntry(RomJointEntry entry)
        {
            if (entry == null) return;
            entry.recordedAtIso = DateTime.UtcNow.ToString("o");
            var existing = State.rom.Get(entry.joint);
            if (existing != null) State.rom.entries.Remove(existing);
            State.rom.entries.Add(entry);
            Save();
        }

        /// <summary>Phase 7 calls this when all joints are done.</summary>
        public void FinishRomCalibration()
        {
            State.rom.calibratedAtIso = DateTime.UtcNow.ToString("o");
            Save();
            OnRomCalibrationCompleted?.Invoke(State.rom);
        }

        public ROMProfile GetRomProfile() => State.rom;
    }
}
