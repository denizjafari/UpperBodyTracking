f# Phases 6, 7, 8 — Calibration A, Calibration B, Session Setup

## What landed

```
Assets/Scripts/Input/
└── UdpControlSender.cs                     # Unity → RPi control messages

Assets/Scripts/Scenes/Calibration/
├── CalibrationAudioManager.cs              # numbers, GO, done, retry, per-joint instructions
├── CalibrationASceneController.cs          # Scene 3 — IMU bias hold-pose flow
├── RomCalibrationConfig.cs                 # JSON-driven timings (with safe defaults)
├── SensorHealthLog.cs                      # NDJSON append-only failure log
└── RomCalibrationController.cs             # Scene 4 — full four-phase ROM loop

Assets/Scripts/Scenes/SessionSetup/
├── SessionSetupSceneController.cs          # Scene 6 — routine builder
└── GameOptionRow.cs                        # one row in the routine UI
```

---

## Phase 6 — Calibration A (IMU bias)

### Scene setup
1. New scene `Assets/Scenes/Calibration_A.unity`.
2. Root `[CalibA]` with `CalibrationASceneController` and a `UdpControlSender`.
3. World-space canvas with title, big countdown text, status text, Start, Skip.
4. Wire UdpControlSender's `targetHost` to your RPi's LAN IP, `targetPort` = 5006
   (or whatever you decide for the RPi listener).

### Flow (3-second prep + 5-second hold + ack wait, all configurable)
1. Show "Stand neutrally" + Start button.
2. On Start: countdown → send `start_bias` → 5 s hold → send `stop_bias`.
3. Wait up to 10 s for an ack packet — RPi sends a special datagram with
   `joint = "_ack"` and `compensation.type = "bias_complete"`.
4. On ack: `Services.Calibration.MarkImuBiasComplete()` → next scene.
5. On timeout: status "No reply from sensors. Retry or skip."

### RPi protocol (matches `UdpControlSender`)
```
Unity → RPi (port 5006):  {"cmd":"start_bias"}
Unity → RPi (port 5006):  {"cmd":"stop_bias"}
RPi   → Unity (port 5005, sensor stream):
   {"ts_ms":..., "joint":"_ack", "angle_deg":0, "valid":true,
    "compensation":{"detected":false,"type":"bias_complete","severity":0}}
```

### Verification gate 6
- [ ] On the happy path the whole flow finishes in ≤ 25 s.
- [ ] Aborting via the back button does not write a partial calibration.
- [ ] Without an RPi, the timeout path appears and "Skip" cleanly advances.

---

## Phase 7 — Calibration B (ROM)

### Scene setup
1. New scene `Assets/Scenes/Calibration_B.unity`.
2. Root `[CalibB]` with `RomCalibrationController` + `CalibrationAudioManager` + `UdpControlSender`.
3. Place `rom_calibration.json` into `Assets/StreamingAssets/`. Modify timings
   without rebuilding by editing the deployed file on-device.
4. Bake audio per design §3.8 into `Assets/Audio/Calibration/en/...` and assign
   them on `CalibrationAudioManager` (numbers 1-5+go, stop_beep, done,
   retry_prompt, instruction clip per joint).
5. Provide demo Animator states named `demo_shoulder_abduction`,
   `demo_shoulder_flexion`, `demo_elbow_flexion`, `demo_shoulder_rotation`,
   `demo_wrist_pronation` on the avatar's Animator (Phase 3 handed this off).
6. World-space canvas: title, instruction text, big center text, Replay-audio
   button (speaker icon), Start, Skip-joint, Cancel buttons.

### Joint loop
Joints are visited in `JointKeys.CalibrationOrder`:
abduction → flexion → elbow → rotation → wrist pronation.

For each joint: **Instruction → Prep countdown → Recording → Transition**.

| Phase | Duration | UI | Audio | Behaviour |
|---|---|---|---|---|
| Instruction | until Start (or 4 s auto-advance) | text + avatar demo loop | TTS instruction (auto, replayable) | — |
| Prep countdown | 5 s default (3-5 configurable) | "5..4..3..2..1..GO" | spoken numbers + GO | — |
| Recording | 10 s default (5-15 configurable) | live timer + avatar mirrors live UDP | silent | track running min/max from UDP |
| Transition | 1.5 s | "DONE" / "Try again" / "Skipped" | stop beep + done OR retry prompt | persist or retry |

### Validity check (design §3.7)
A recording is invalid if either:
1. `running_max - running_min < min_valid_range_deg` (default 5°), or
2. `running_min` is still +∞ / `running_max` still −∞ (no packets received for this joint).

On invalid + retry < `max_retries` (default 1): replay phase 1 for the same joint.
On invalid + max retries hit: write `complete=false` to calibration.json AND
append a `rom_calibration_failed` entry to
`Users/<id>/logs/sensor_health.json` (NDJSON).

### Verification gate 7
- [ ] Five joints visited in order on a real session.
- [ ] Forcing an invalid recording (don't move) triggers exactly one retry.
- [ ] Forcing two invalid recordings writes one sensor-health line and continues.
- [ ] `calibration.json` after a complete run has five entries with min/max/complete/retries.
- [ ] Replay-audio re-plays *only* the current joint's instruction.
- [ ] `git tag phase-7-rom && git push --tags`.

---

## Phase 8 — Session Setup (Scene 6)

### Scene setup
1. New scene `Assets/Scenes/SessionSetup.unity`.
2. Root `[SessionSetup]` with `SessionSetupSceneController`.
3. Build a row prefab (`Assets/Prefabs/UI/GameOptionRow.prefab`) with: Toggle,
   name TMP, +/- buttons for difficulty, +/- buttons for duration, an "unavailable"
   hint TMP. Attach `GameOptionRow` and wire the fields.
4. Drag the row prefab into `optionRowPrefab`, and a `VerticalLayoutGroup` into
   `optionListContainer`.
5. Populate the `games` list in inspector — one entry per mini-game:

| gameKey | displayName | requiredJoints |
|---|---|---|
| `FlappyBird` | Flappy Bird | shoulder_flexion |
| `RockClimbing` | Rock Climbing | shoulder_abduction, elbow_flexion |
| `WhackAMole` | Whack-a-Mole | shoulder_flexion, shoulder_abduction |
| `SteeringWheel` | Steering Wheel | wrist_pronation, shoulder_rotation |
| `GardenMeditation` | Garden Meditation | (none) |
| `Blocking` | Blocking | shoulder_rotation |
| `PumpBellows` | Pump the Bellows | shoulder_abduction |
| `Glassblowing` | Glassblowing | wrist_pronation |

Games whose required joints are not all `complete=true` in calibration.json show
the "needs ROM calibration" hint and are not selectable.

### Output: SessionConfig
On Begin, builds a `SessionConfig`, persists to `Users/<id>/session_config.json`,
and calls `Services.Session.StartSession(config)`. Phase 9 wires `OnExerciseStarted`
to the appropriate mini-game scene load.

### Verification gate 8
- [ ] Selecting two games + Begin produces a `session_config.json` with two entries.
- [ ] An incomplete-calibration user only sees `GardenMeditation` selectable.
- [ ] Mid-session abort writes `aborted=true` to the SQLite session row (Phase 11 wires this).
