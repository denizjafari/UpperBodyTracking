# VR Rehabilitation Platform — Phased Implementation Plan
**Target hardware:** Meta Quest 3S (Snapdragon XR2 Gen 2, 8 GB RAM, Fresnel optics)
**Engine:** Unity 6.2 (6000.2.8f1)
**SDK stack:** Meta XR All-in-One SDK v85, Meta XR Interaction SDK v85, Meta XR Movement SDK v85, OpenXR
**Source design:** `VR_Rehab_Final_v3.docx` (Scenes 1–8, Mini-Games §10)

---

## How to read this plan

The plan is organized as **Phases → Modules → Verification Gates**. Every module is self-contained: it has a single owner-folder under `Assets/Scripts/<Module>/`, a single test scene, and a checklist that must pass before the next module starts. Mini-games each get their own self-contained module so a bug in one game cannot block the others.

Each phase ends with a **Verification Gate**. Do not advance to the next phase until every box in the gate is checked on real Quest 3S hardware (not just the editor).

> **SDK-version pin:** All package references use Meta XR SDK **v85**. If you upgrade, re-run §0.3 (Project Setup Tool) and re-test §1 (Avatar) and §3 (Passthrough) — these are the modules most affected by SDK churn.

---

## Phase 0 — Project Foundation
**Goal:** A buildable, deployable empty Quest 3S app with all SDKs wired in correctly.
**Estimated time:** 1–2 days. **Owner:** lead engineer.

### 0.1  Repo & Unity project
- Unity Hub → install **6000.2.8f1** with the Android Build Support module (OpenJDK + Android SDK & NDK Tools).
- Create new 3D (URP) project. URP is required for the depth-aware passthrough shader graphs used later.
- Set color space to **Linear** (`Edit → Project Settings → Player → Other Settings → Color Space`). Required for correct passthrough compositing.
- Initialize git, add `.gitignore` for Unity, commit.

### 0.2  Install Meta XR SDK v85
Add scoped registry first:
```
Edit → Project Settings → Package Manager → Scoped Registries
  Name:  Meta XR
  URL:   https://npm.pkg.github.com
  Scope: com.meta.xr
```
Then install via Package Manager:
```
com.meta.xr.sdk.all          v85.x   ← All-in-One umbrella package
com.meta.xr.sdk.interaction  v85.x
com.meta.xr.sdk.movement     v85.x
com.unity.xr.management
com.unity.inputsystem
```
Disable the legacy Input Manager when prompted.

### 0.3  Build target & XR config
```
File → Build Settings → Switch Platform → Android
  Texture Compression: ASTC
  Target API Level:    32+ (Android 12L)
  Scripting Backend:   IL2CPP
  Target Architectures: ARM64 only  (uncheck ARMv7)
  Minimum API Level:   29

Edit → Project Settings → XR Plug-in Management → Android tab
  ☑ Meta XR (OpenXR)

OpenXR → Features → enable:
  ☑ Meta Quest Support
  ☑ Hand Tracking Subsystem
  ☑ Meta XR Passthrough
  ☑ Meta XR Scene
  ☑ Eye Gaze Interaction (only if you plan to use eye tracking later)
```
Run **Meta → Tools → Project Setup Tool** and resolve every red item before continuing.

### 0.4  Bootable empty scene
Create `Assets/Scenes/_Bootstrap.unity`:
- Drop the `OVRCameraRig` prefab.
- Add an `OVRManager` component:
  - HandTrackingSupport: **Controllers and Hands**
  - TrackingOriginType: **Floor Level**
  - Target Display Refresh Rate: **72** (locked because we use passthrough — see §3.1)
- Add a single ground plane and a debug TextMesh Pro world-space canvas at eye level so you can confirm rendering.

### Verification Gate 0
- [ ] APK builds without errors.
- [ ] Deploys via Meta Quest Developer Hub or `adb install -r`.
- [ ] On-device: you see the ground plane, the world-space text, and head-look responds.
- [ ] OVR Metrics Tool shows steady **72 Hz**, GPU < 8 ms idle.
- [ ] Commit tagged `phase-0-foundation`.

---

## Phase 1 — Core Scene & Manager Layer
**Goal:** A single persistent Core Scene that owns every cross-scene singleton, with mini-game and calibration scenes loaded *additively* on top.
**Estimated time:** 3–5 days.

### 1.1  Core Scene composition
`Assets/Scenes/Core.unity` contains exactly:
- `OVRCameraRig` (the only one in the project — never duplicate).
- `[Managers]` GameObject, parent of every singleton listed below. Marked `DontDestroyOnLoad` is **not** needed because the Core scene is never unloaded.
- An empty `[ActiveSceneRoot]` transform — additive scenes parent their root here so we can find them deterministically.

### 1.2  Manager layer (one C# class per file, one MonoBehaviour per GameObject)
| Manager | Responsibility | Key public API |
|---|---|---|
| `SceneFlowManager` | Loads/unloads additive scenes in a defined order. | `LoadAdditive(string)`, `UnloadAdditive(string)`, event `OnSceneReady` |
| `UserManager` | Active user profile, switches, per-user folders on disk. | `CurrentUserID`, `LoadUser(id)`, `CreateUser(...)` |
| `PreferenceManager` | Hands `user_preferences` JSON to whoever asks. | `Get<T>(key)`, `Save()` |
| `SessionManager` | Holds the in-flight session, pushes the routine queue, opens/closes SQLite session row. | `StartSession(config)`, `AdvanceToNextExercise()`, `EndSession()` |
| `CalibrationManager` | IMU bias, ROM, functional. Owns `calibration.json`. | `IsRomCalibrationRequired()`, `StartRomCalibration()`, `GetRomProfile()` |
| `SensorInputManager` | UDP listener + tracking-mode flag. (Implemented in Phase 2.) | `OnPacket`, `CurrentMode`, `LastJointAngles` |
| `AvatarDriver` | Maps joint angles to a humanoid rig. (Phase 3.) | `Drive(JointSnapshot)`, `SetMirror(bool)` |
| `FeedbackManager` | Dispatches text/audio/haptic/visual feedback per preferences. (Phase 4.) | `OnCompensation(packet)` |
| `LoggingManager` | SQLite + JSON + CSV writes. (Phase 11.) | `LogEvent`, `LogKinematicsFrame` |
| `AudioBus` | Centralized AudioMixer reference, voice/ambience/sfx groups. | `Play(clip, group)` |

### 1.3  Service-locator pattern (avoid Singletons-as-statics)
```csharp
public static class Services
{
    public static SceneFlowManager Scenes  { get; internal set; }
    public static UserManager      Users   { get; internal set; }
    // ...
}
```
Each manager registers itself in `Awake()`. This makes managers swappable in tests without `FindObjectOfType` chains.

### 1.4  Additive-scene contract
Every non-core scene must:
1. Contain a single root GameObject with a `SceneEntry` script that exposes `OnEnter()` / `OnExit()`.
2. Never instantiate cameras, OVRRig, or audio listeners.
3. Tear down all coroutines and event subscriptions in `OnExit()`.

This is the rule that keeps mini-game bugs from leaking into the Core.

### Verification Gate 1
- [ ] You can `Services.Scenes.LoadAdditive("DebugScene_Cube")`, see a cube appear, and `UnloadAdditive` removes it without console errors.
- [ ] Switching `UserManager.CurrentUserID` triggers `OnUserChanged` exactly once per call.
- [ ] No manager has a hard reference to another manager's *implementation type* — they only use interfaces or `Services.X`.
- [ ] Commit tagged `phase-1-managers`.

---

## Phase 2 — Sensor Input Layer
**Goal:** A unified `JointSnapshot` flowing from any source (RPi UDP, controllers, or simulated), with sub-50 ms latency.
**Estimated time:** 3–4 days.

### 2.1  Tracking-mode flag (per design §4.2)
```csharp
public enum TrackingMode { IMU_UDP, Controllers, Simulator }
```
Stored in `session_config.json` per game. Each game queries it at `Awake` and binds the matching input adapter.

### 2.2  UDP listener
- Use `System.Net.Sockets.UdpClient` on a background `Thread` (not `Task` — Quest's mono runtime handles threads better here).
- Marshal received packets onto the Unity main thread via `ConcurrentQueue<UdpPacket>` drained in `Update()`.
- Default port: **5005** (configurable in `network.json`).
- Packet schema (per design §4.1):
```json
{
  "ts_ms": 1745136540123,
  "joint": "shoulder_flexion",
  "angle_deg": 87.4,
  "valid": true,
  "compensation": { "detected": false, "type": null, "severity": 0 }
}
```
- Drop packets older than 100 ms (UDP can buffer a stale burst at startup).

### 2.3  Sensor-loss behaviour (design §4.1 "Sensor Data Lost")
- If no packet for any joint in **500 ms**: raise `OnSensorTimeout(joint)` and have FeedbackManager show a non-blocking "Sensor disconnected" toast.
- After 5 s without recovery, pause the active mini-game (do not abort) and prompt user to check sensors.

### 2.4  Controller fallback adapter
For dev work without IMUs, derive joint angles from controller pose:
- `shoulder_flexion`: angle between right-controller forward vector and world-down vector.
- `shoulder_abduction`: angle between right-controller position vector (relative to head) and world-down.
- `elbow_flexion`: hardcoded to 90° in fallback (cannot be derived from controllers alone).

This adapter is only enabled when `TrackingMode == Controllers` and is NEVER shipped to clinical builds (gate behind a `#if UNITY_EDITOR || DEV_BUILD`).

### 2.5  Simulator adapter
A `MonoBehaviour` that publishes a sine-wave `JointSnapshot` at 50 Hz. Used by automated tests of the FeedbackManager and mini-game logic.

### Verification Gate 2
- [ ] On-device: send 1000 UDP packets from a laptop, confirm < 5 dropped and < 50 ms median latency (timestamp comparison logged to disk).
- [ ] Yanking the Wi-Fi for 2 s causes the toast to appear, and reconnecting clears it within one packet.
- [ ] Switching `TrackingMode` at runtime cleanly swaps the adapter without GC spikes (verify via Profiler).
- [ ] Commit tagged `phase-2-input`.

---

## Phase 3 — Avatar & Passthrough Mirror
**Goal:** A humanoid avatar in front of the user that mirrors the live joint angles. Used by every calibration scene and as a coach in mini-games.
**Estimated time:** 2–3 days.

### 3.1  Refresh-rate decision
Per Meta's passthrough best practices, **lock to 72 Hz** whenever passthrough is on. We will use passthrough in Scene 1 (Welcome), Scene 2 (Preferences), Scene 3–4 (Calibration), and the meditative mini-games (Garden, Glassblowing). Action mini-games (Flappy Bird, Whack-a-Mole) can run fully immersed — we'll switch to 90 Hz at scene-load via `OVRManager.display.displayFrequency = 90f;`.

### 3.2  Humanoid rig
- Import a free humanoid (Mixamo Y-Bot or the Movement SDK sample avatar).
- Configure as **Humanoid** rig in the Model importer.
- Add `Animator` with empty controller — we drive bones procedurally, not via clips.

### 3.3  AvatarDriver
```csharp
public class AvatarDriver : MonoBehaviour
{
    [SerializeField] Animator anim;
    public void Drive(JointSnapshot s)
    {
        // LateUpdate so we override animator output
    }
    void LateUpdate()
    {
        // Apply latest snapshot to bones using HumanBodyBones references
        // Use Quaternion.Slerp with 0.6 factor to smooth jitter
    }
}
```
- Drive **only the right upper limb**: `RightUpperArm`, `RightLowerArm`, `RightHand`. Leave the rest in T-pose for the MVP (per design §3 — single-arm rehab).
- Smoothing: 1-frame Slerp at α=0.6 hides UDP jitter without adding perceptible lag.
- Mirror mode: when `mirror == true` we apply the inverse rotation to the *left* arm so the patient sees the avatar facing them and matching their right arm. Used in Calibration A.

### 3.4  Demo-loop animation
For ROM Calibration's instruction phase, each joint needs a looped demo. Bake five short clips (~3 s each) in the editor:
- `demo_shoulder_abduction.anim`
- `demo_shoulder_flexion.anim`
- `demo_elbow_flexion.anim`
- `demo_shoulder_rotation.anim`
- `demo_wrist_pronation.anim`

`AvatarDriver.PlayDemo(string jointKey)` swaps from procedural to the named clip, `StopDemo()` returns to procedural.

### Verification Gate 3
- [ ] Avatar arm tracks UDP input with ≤ 100 ms perceptible lag.
- [ ] Mirror mode produces a visually natural mirror (not inverted-up-down).
- [ ] Demo clips loop seamlessly with no animation pop on enter/exit.
- [ ] Frame budget: AvatarDriver costs < 0.4 ms CPU per frame (measure in Profiler).
- [ ] Commit tagged `phase-3-avatar`.

---

## Phase 4 — Feedback System
**Goal:** Every mini-game can call `Services.Feedback.RaiseCompensation(packet)` and have the right combination of text + audio + visual highlight + haptic fire, gated by the user's preference flags.
**Estimated time:** 2–3 days.

### 4.1  Channel modules
Each channel is a separate `MonoBehaviour` registered with `FeedbackManager`:
- `TextFeedbackChannel` — world-space TMP panel, severity color (green/yellow/red).
- `AudioFeedbackChannel` — TTS-pre-baked WAV per compensation message in the user's language.
- `AvatarHighlightChannel` — flashes the offending body segment red on the avatar via material property block (no per-frame allocations).
- `HapticChannel` — `OVRInput.SetControllerVibration` for 100 ms then auto-stop.

### 4.2  Preference gating
At session start, `FeedbackManager.LoadPreferences(prefs)` toggles channels on/off based on `text_on`, `audio_on`, `visual_on`, `haptic_on` from `user_preferences.json`.

### 4.3  Severity scaling
Compensation severity 0–1 from the RPi:
- 0.0–0.33: green text, no audio, no haptic.
- 0.33–0.66: yellow text, audio "gentle reminder", short haptic pulse.
- 0.66–1.0: red text, audio "strong correction", long haptic pulse, avatar highlight.

### 4.4  Cooldown
Suppress repeat-firing of the same compensation for 2 s — otherwise a held compensation re-triggers at packet rate (50 Hz) and overwhelms the user.

### Verification Gate 4
- [ ] Trigger each compensation type in the simulator and confirm exactly the configured channels fire.
- [ ] Toggling each preference flag at runtime takes effect on the next packet without restart.
- [ ] Cooldown is observable: a stuck compensation fires every 2 s, not every frame.
- [ ] Commit tagged `phase-4-feedback`.

---

## Phase 5 — Welcome & Preferences (Scenes 1 & 2)
**Goal:** A patient can launch the app, pick or create a profile, and adjust preferences without leaving the headset.
**Estimated time:** 3 days.

### 5.1  Scene 1 — Welcome
- Passthrough enabled, world-space TMP UI.
- Controls: profile dropdown (built from `UserManager.GetAllUsers()`), "Create new" button, "Continue" button.
- Once continued: `Services.Users.LoadUser(id)` → `SceneFlowManager.LoadAdditive("UserPreferences")` if first session, else `LoadAdditive("Calibration_A")`.

### 5.2  Scene 2 — Preferences
- Toggles for the four feedback channels.
- Language dropdown (en / fr — extensible).
- Buttons to relaunch each calibration scene independently.
- Preferences persist to `Users/<id>/user_preferences.json` and the SQLite `user_preferences` table.

### Verification Gate 5
- [ ] Creating a new user creates `Users/<id>/` with empty `calibration.json`, default `user_preferences.json`, empty `sessions.db`.
- [ ] Re-launching the app opens directly to the last-used profile.
- [ ] Preference changes survive an app restart.
- [ ] Commit tagged `phase-5-profile`.

---

## Phase 6 — Calibration A: IMU Bias (Scene 3)
**Goal:** Hold-pose-for-5-seconds flow that signals the RPi to start its bias estimation, then waits for a "ready" packet.
**Estimated time:** 1 day.

### 6.1  Flow
1. Show instruction: "Stand neutral, arms at sides."
2. 3-2-1 countdown (reuse `CalibrationAudioManager` from Phase 7).
3. 5-second hold; avatar shows desired pose.
4. Send a `start_bias` UDP message to RPi.
5. Wait up to 10 s for an `imu_bias_complete` UDP packet → write `calibration.json::imu_bias = true` and exit.
6. On timeout: prompt retry (max 1), then escalate to support screen.

### Verification Gate 6
- [ ] Whole flow completes inside 25 s on the happy path.
- [ ] Aborting (back button) cleanly returns to Scene 1 with no half-written calibration data.

---

## Phase 7 — Calibration B: ROM (Scene 4)
**Goal:** Per the design doc §3.4–3.7, the four-phase loop across all five joints with retry, sensor-health logging, and per-joint persistence.
**Estimated time:** 3–4 days.

### 7.1  State machine
Implement explicitly with a `RomPhase` enum (`Instruction → PrepCountdown → Recording → Transition`). One coroutine per phase keeps the logic readable and the cancellation semantics clean.

### 7.2  Per-joint loop (skeleton)
```csharp
foreach (var joint in JointOrder.All)
{
    yield return PhaseInstruction(joint);
    yield return PhasePrepCountdown(joint);
    yield return PhaseRecording(joint);          // updates runningMin/Max from UDP
    yield return PhaseTransition(joint);         // validate, persist or retry
}
```

### 7.3  Validity check (design §3.7)
```csharp
bool valid = (runningMax - runningMin) >= 5f
          && !float.IsPositiveInfinity(runningMin)
          && !float.IsNegativeInfinity(runningMax);
```
On invalid + retry < 1 → re-enter Phase 1 for same joint.
On invalid + retry == 1 → mark `complete=false`, log `sensor_health` event, advance.

### 7.4  Audio assets
Bake the WAV files exactly as listed in design §3.8. Place them under `Assets/Audio/Calibration/<lang>/`. Manifest each file in a `CalibrationAudioManifest` ScriptableObject so missing files are caught at build time, not runtime.

### 7.5  Configurable timings
`Assets/Settings/rom_calibration.json`:
```json
{ "prep_countdown_s": 5, "recording_window_s": 10, "transition_pause_s": 1.5, "max_retries": 1 }
```
Loaded by `CalibrationManager` at scene start. **Never** hardcode timings.

### Verification Gate 7
- [ ] All five joints run in order on a real session.
- [ ] Forcing an invalid recording (don't move) triggers exactly one retry.
- [ ] Forcing two invalid recordings writes one `sensor_health` event and continues.
- [ ] `calibration.json` after a complete run contains five entries, each with `min`, `max`, `complete`, `retries`, `recordedAt`.
- [ ] Replay button replays only the current joint's instruction audio.
- [ ] Commit tagged `phase-7-rom`.

---

## Phase 8 — Session Setup (Scene 6)
**Goal:** A clinician/patient picks today's exercises and difficulty; the result is `session_config.json` plus a fresh row in the SQLite `sessions` table.
**Estimated time:** 2 days.

### 8.1  UI
- List of available mini-games (filtered by which calibrations the user has completed).
- For each: pick difficulty 1–5 and duration in minutes.
- "Begin session" button writes config and pushes the routine onto `SessionManager`.

### 8.2  session_config.json schema
```json
{
  "session_id": "2026-05-02T14-21-09Z",
  "user_id": "u_001",
  "tracking_mode": "IMU_UDP",
  "exercises": [
    { "game": "FlappyBird",  "difficulty": 2, "duration_s": 180 },
    { "game": "RockClimbing","difficulty": 3, "duration_s": 240 }
  ]
}
```

### Verification Gate 8
- [ ] A configured session correctly drives `SessionManager.AdvanceToNextExercise()` through every exercise in order.
- [ ] Exiting mid-session writes `session_aborted=true` and never corrupts a row.
- [ ] Commit tagged `phase-8-session`.

---

## Phase 9 — Mini-Games (Modular)
**Each mini-game is its own additive scene + its own folder under `Assets/MiniGames/<GameName>/`.** No mini-game depends on another. A bug in Whack-a-Mole cannot break Flappy Bird because they share *only* the manager interfaces from Phase 1.

### 9.0  Per-game module template
Every game contains exactly this folder layout:
```
Assets/MiniGames/<GameName>/
├── Scenes/
│   └── <GameName>.unity            # additive scene, scene root has GameController
├── Scripts/
│   ├── <GameName>Controller.cs     # SceneEntry implementation
│   ├── <GameName>Input.cs          # subscribes to JointSnapshot, exposes 1D control signal
│   ├── <GameName>Difficulty.cs     # ScriptableObject of difficulty curves
│   └── <GameName>Score.cs          # writes to LoggingManager
├── Prefabs/
├── Materials/
├── Audio/
└── Settings/
    └── <gamename>_config.json
```
**Implementation order per game:** Skeleton scene → Input mapping → Visual hookup → Game loop → Difficulty curves → Feedback wiring → Logging → Polish.
**Verification gate per game** (apply to every game):
- [ ] Loads/unloads additively without leaking GameObjects (verify with `FindObjectsOfType<Transform>().Length` before/after).
- [ ] Plays correctly with simulator input (no IMU needed for unit testing).
- [ ] Plays correctly with real IMU input on hardware.
- [ ] Difficulty 1 and 5 are noticeably different.
- [ ] All compensation events from RPi reach FeedbackManager during play.
- [ ] Score row written to SQLite `exercise_results` at end-of-game.

### 9.1  Game A — Flappy Bird (Shoulder Flexion)
**Mechanic:** Shoulder flexion angle drives bird vertical position. 0° = floor, 90° = ceiling. Pipes scroll horizontally; bird must thread the gap.
**Input mapping:** `bird.y = Lerp(yMin, yMax, joint.shoulder_flexion / userROM.shoulder_flexion.max)`. Use the per-user max from ROM calibration so a patient with limited range can still reach the ceiling.
**Difficulty levers:** pipe spacing (vertical gap), pipe spawn rate, scroll speed.
**Failure modes to test:** What if user's ROM max is < 30°? Game should still be playable — clamp the gap larger.
**Estimated time:** 2 days.

### 9.2  Game B — Rock Climbing (Shoulder Abduction + Elbow Flexion)
**Mechanic:** Two reach-and-grab targets at varying positions. Reaching requires shoulder abduction; gripping animation triggers when elbow flexion crosses a per-user threshold.
**Input mapping:** Hand world-position synthesized from `(shoulder_abduction, elbow_flexion)` via a simple 2-bone IK forward solve. Grab "lock" fires when `elbow_flexion > userROM.elbow_flexion.max * 0.8`.
**Difficulty levers:** target separation, time-on-hold required, height of next target.
**Watch-out:** This is the only game requiring **two simultaneous joint angles**. Make sure both UDP packets arrive within the same frame before computing IK — buffer one frame if needed.
**Estimated time:** 3 days.

### 9.3  Game C — Whack-a-Mole (Shoulder Flexion/Abduction + Reaction)
**Mechanic:** Six holes arranged in a 2×3 grid in front of the user. Moles pop up; user reaches and "whacks" with the avatar's hand. Each hole maps to a specific (flexion, abduction) target zone.
**Input mapping:** Avatar hand position from current joint angles; collision with mole prefab via `OnTriggerEnter`.
**Difficulty levers:** mole show-time, simultaneous moles, hole spacing.
**Watch-out:** Don't allow the user to "park" their hand on a hole — require the hand to leave the zone between hits (track `lastHitHole` and require dwell-out of 200 ms).
**Estimated time:** 2 days.

### 9.4  Game D — Steering Wheel (Wrist Pronation + Shoulder Rotation)
**Mechanic:** First-person driving on a curved road. Wrist pronation/supination steers; shoulder rotation can be a secondary "lean" input that subtly biases the steering for accessibility.
**Input mapping:** `steering_angle = wrist_pronation_deg + 0.3 * shoulder_rotation_deg`. Clamped to [-90°, +90°].
**Difficulty levers:** road curvature frequency, vehicle speed, lane width.
**Watch-out:** Steering with a virtual wheel can induce motion sickness. Use a fixed cockpit (high-contrast steering wheel + dashboard) to anchor the user — Meta's recommendation for any vehicle game.
**Estimated time:** 3 days.

### 9.5  Game E — Garden Meditation (Breathing)
**Mechanic:** Passthrough-on, low-stakes. Patient sits, watches a CG flower bloom in front of them, and breathes with on-screen guidance (4-in / 7-out).
**Input mapping:** None from IMUs — purely time-based. Optional: use `OVRMicrophone` audio amplitude as a soft input for "breathe louder" feedback (Quest 3S has stereo mics).
**Difficulty levers:** session duration only.
**Estimated time:** 1 day. (Smallest game — start here if the team wants an easy first build.)

### 9.6  Game F — Blocking Game (Shoulder Rotation)
**Mechanic:** Soft objects fly toward the user from the side; user must rotate shoulder to "block" with the avatar's forearm.
**Input mapping:** `block_angle = joint.shoulder_rotation`. Successful block when projectile crosses the forearm volume during the rotation.
**Difficulty levers:** projectile speed, side-to-side variation, projectiles per minute.
**Estimated time:** 2 days.

### 9.7  Game G — Pump the Bellows (Shoulder Abduction)
**Mechanic:** Stylized blacksmith bellows. Patient performs shoulder abduction reps to "pump" — each full cycle (low → high → low) increases a fire intensity meter that lights a forge.
**Input mapping:** Detect cycles via Schmitt-trigger on `shoulder_abduction` (rising threshold = 70% of ROM max, falling = 20% of ROM max).
**Difficulty levers:** target reps per minute, total reps, abduction percentage required to count.
**Estimated time:** 2 days.

### 9.8  Game H — Glassblowing (Wrist Pronation)
**Mechanic:** Patient holds a virtual blowpipe with molten glass at the end. Wrist pronation/supination shapes the glass into target forms (vase, bowl).
**Input mapping:** Pronation angle drives a procedural mesh deformation parameter on the glass blob.
**Difficulty levers:** target shape complexity, time limit, required smoothness of motion (penalize jitter).
**Estimated time:** 3 days.

### Build order recommendation
1. **Garden Meditation** (Phase 9.5) — smallest, validates the additive-scene template end-to-end.
2. **Flappy Bird** (Phase 9.1) — simplest single-joint mapping, validates the input → game-state pipeline.
3. **Pump the Bellows** (Phase 9.7) — first cycle-detection game.
4. **Whack-a-Mole** (Phase 9.3) — first reaction-time game.
5. **Rock Climbing** (Phase 9.2) — first multi-joint game.
6. **Blocking Game** (Phase 9.6).
7. **Steering Wheel** (Phase 9.4) — motion-sickness risk, do after team has experience.
8. **Glassblowing** (Phase 9.8) — most novel input mapping, do last.

---

## Phase 10 — Questionnaire Overlay (Scene 8)
**Goal:** After every mini-game ends, show a 4-question Likert overlay (difficulty, enjoyment, fatigue, pain) before the next game loads.
**Estimated time:** 2 days.

### 10.1  Implementation
Don't build from scratch — evaluate the three options the design lists:
1. VRQuestionnaireToolkit (open-source, MIT, well-maintained).
2. QuestionnaireToolkit (paid asset).
3. Immersive Questionnaire Tool.

Recommendation: use **VRQuestionnaireToolkit** unless licensing issues — it integrates cleanly with OVRCameraRig and the data export is JSON-per-questionnaire.

### 10.2  Hook
`SessionManager` raises `OnExerciseCompleted(result)` → `QuestionnaireOverlay.Show(result)` → on submit, `LoggingManager.LogQuestionnaire(...)` → `SessionManager.AdvanceToNextExercise()`.

### Verification Gate 10
- [ ] Skipping the questionnaire (timeout 60 s) still advances to the next exercise.
- [ ] All four answers are persisted with the correct exercise_id foreign key.

---

## Phase 11 — Data, Logging & Export
**Goal:** Per design §8 — SQLite for structured data, JSON for config, CSV only for export.
**Estimated time:** 2 days.

### 11.1  SQLite schema (one DB per user at `Users/<id>/sessions.db`)
```
users(id, name, dob, dominant_hand, language, created_at)
user_preferences(user_id, text_on, audio_on, visual_on, haptic_on, ...)
sessions(id, user_id, started_at, ended_at, aborted)
exercise_results(id, session_id, game, difficulty, duration_s, score, ended_at)
compensation_events(id, exercise_id, ts_ms, type, severity)
kinematics_frames(id, exercise_id, ts_ms, joint, angle_deg)   -- high volume, batched insert
questionnaire_responses(id, exercise_id, q1, q2, q3, q4)
sensor_health_events(id, session_id, ts_ms, type, joint, payload_json)
```
Use `Mono.Data.Sqlite` (ships with Unity) or the `sqlite-net-pcl` package via NuGetForUnity.

### 11.2  Write strategy
- Kinematics frames at 50 Hz × 5 joints × 3 min = 45 000 rows per exercise. **Batch insert** every 100 frames inside a single transaction to keep write cost amortized.
- Compensation events fire-and-forget on a background thread (use a `BlockingCollection<LogEvent>` consumer pattern).

### 11.3  CSV export
A "Export last session" button on the Welcome scene writes `Users/<id>/exports/session_<id>.csv` with all kinematics frames flattened. Used by the clinician for offline analysis.

### Verification Gate 11
- [ ] A 3-minute exercise produces a valid SQLite row count of ~45 000 ± 100.
- [ ] No frame-time spike > 2 ms attributable to logging in the Profiler.
- [ ] CSV export parses cleanly in pandas (`pd.read_csv` with no warnings).

---

## Phase 12 — Performance, QA & Submission
**Goal:** Hit Quest 3S quality bar before any pilot use.
**Estimated time:** 3 days plus rolling.

### 12.1  Performance budget (per Meta's Quest 3S guidance)
| Metric | Target |
|---|---|
| Frame rate | 72 Hz locked (passthrough scenes), 90 Hz immersed |
| GPU frame time | < 13.8 ms @ 72 Hz, < 11 ms @ 90 Hz |
| CPU frame time | < 13 ms @ 72 Hz |
| Draw calls per eye | < 150 |
| Triangles per frame | < 100 K |
| Texture memory | < 1.5 GB |

Profile every game in **OVR Metrics Tool**. Any game that misses budget gets a perf-pass before pilot.

### 12.2  Lens-aware UI audit
Quest 3S Fresnel lenses blur the periphery. For every scene:
- All readable text within central 60° FOV.
- No critical UI elements within 10° of the screen edge.

### 12.3  Foveated rendering
Set `OVRManager.fixedFoveatedRenderingLevel = OVRManager.FixedFoveatedRenderingLevel.HighTop;` for action games. The 3S Fresnel vignette hides the FFR transition zone better than pancake lenses.

### 12.4  Permissions audit
Body / face / eye tracking each need:
- AndroidManifest entry (Movement SDK adds these via Project Setup Tool — confirm they're present).
- Runtime permission request on first session.

### 12.5  Test matrix
| | Sim input | Controller input | IMU input |
|---|---|---|---|
| Each calibration scene | ✓ | ✓ (limited) | ✓ |
| Each mini-game | ✓ | ✓ (limited) | ✓ |

### Verification Gate 12
- [ ] All eight games sustain target frame rate for full session.
- [ ] No GC.Alloc spikes > 1 KB/frame in any game (Deep Profiler).
- [ ] Two real patients can complete a 20-minute session without crash, glitch, or sensor desync.
- [ ] Build signed and side-loadable via MQDH; release notes drafted.

---

## Cross-cutting concerns

### Source-control hygiene
- One feature branch per phase (e.g. `phase-7-rom`). Squash-merge to `main` only after the verification gate passes.
- Tag every gate so you can roll back individual phases.

### Bug-isolation principle (the modular promise)
Because every mini-game is a self-contained module that depends only on the manager interfaces from Phase 1:
1. To debug a single game, load Core + just that game's scene additively.
2. To prove a bug is in the manager layer (not the game), reproduce in two different mini-games.
3. To prove a bug is in the input layer, reproduce with the simulator adapter — if it goes away, the bug is in the IMU/UDP path.

### Documentation discipline
Inside each `Assets/MiniGames/<GameName>/`, drop a `README.md` describing: the joint mapping, the difficulty levers, the known failure modes, and the verification-gate checklist. This is the single most effective troubleshooting accelerator for the team.

---

## Quick-reference: phase dependency graph
```
Phase 0 (Foundation)
   └─ Phase 1 (Managers)
        ├─ Phase 2 (Input)
        ├─ Phase 3 (Avatar)
        └─ Phase 4 (Feedback)
              ├─ Phase 5 (Welcome / Prefs)
              ├─ Phase 6 (Calib A)
              │     └─ Phase 7 (Calib B / ROM)
              │           └─ Phase 8 (Session Setup)
              │                 ├─ Phase 9 (Mini-games — parallelizable)
              │                 │     └─ Phase 10 (Questionnaire)
              │                 └─ Phase 11 (Logging)
              │                       └─ Phase 12 (Perf / QA)
```
Phases 9.1 through 9.8 can be worked in parallel by different developers — that is the central design decision behind the modular structure.
