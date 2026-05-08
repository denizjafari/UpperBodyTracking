# VR Rehab — Implementation Status

Live tracker. Updated as each phase / module lands.
Plan source of truth: [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md).
Per-phase docs: [`unity/docs/`](unity/docs/).
Asset catalogue: [`unity/docs/FREE_PREFABS_AND_ASSETS.md`](unity/docs/FREE_PREFABS_AND_ASSETS.md).
Per-game template: [`unity/Assets/MiniGames/README.md`](unity/Assets/MiniGames/README.md).
**Merge plan (next step):** [`unity/docs/MERGE_PLAN.md`](unity/docs/MERGE_PLAN.md) — fold UpperLimbRehabilitation in.

| Phase | Module | Status | Notes |
|---|---|---|---|
| 0 | Project Foundation (Unity Editor) | ✅ mostly satisfied | Your `UpperLimbRehabilitation/` already has Unity 6000.4.4f1, Meta XR SDK 201.0.0, OpenXR 1.17.0, AndroidManifest configured for Quest 3S. Merge folds it in. |
| 1 | Core scene & manager layer | ✅ done | Updated: `LoggingManager` + `MiniGameRouter` added |
| 2 | Sensor input layer | ✅ done | UDP + Controller + Simulator adapters |
| 3 | Avatar driver | ✅ done | Humanoid bone map + procedural drive |
| 4 | Feedback system | ✅ done | 4 channels + severity tiers + cooldown |
| 5 | Welcome & Preferences (Scenes 1, 2) | ✅ done | UiBindings + 2 scene controllers |
| 6 | Calibration A — IMU bias (Scene 3) | ✅ done | Hold-pose flow + RPi handshake |
| 7 | Calibration B — ROM (Scene 4) | ✅ done | 4-phase state machine + retry + sensor health log |
| 8 | Session setup (Scene 6) | ✅ done | Routine builder + game option rows |
| **9.0** | **Per-game module template** | **✅ done** | `MiniGameSceneEntry` base + `MiniGameRouter` + `ExerciseResult` |
| **9.5** | **Mini-game — Garden Meditation** | **✅ done** | Breathing pacer, flower bloomer, HUD, optional mic |
| 9.1 | Mini-game — Flappy Bird | ⏳ pending | shoulder flexion |
| 9.2 | Mini-game — Rock Climbing | ⏳ pending | shoulder abduction + elbow flexion |
| 9.3 | Mini-game — Whack-a-Mole | ⏳ pending | shoulder flexion/abduction + reaction |
| 9.4 | Mini-game — Steering Wheel | ⏳ pending | wrist pronation + shoulder rotation |
| 9.6 | Mini-game — Blocking | ⏳ pending | shoulder rotation |
| 9.7 | Mini-game — Pump the Bellows | ⏳ pending | shoulder abduction |
| 9.8 | Mini-game — Glassblowing | ⏳ pending | wrist pronation |
| 10 | Questionnaire overlay (Scene 8) | ⏳ pending | recommend VRQuestionnaireToolkit |
| 11 | Logging & SQLite | 🔄 partial wedge | `LoggingManager` writes NDJSON; SQLite schema pending |
| 12 | Performance & QA | ⏳ pending | |

**Legend:** ✅ done · ⏳ pending · 🔄 in progress · 📋 manual (user action needed) · ⚠️ blocked

---

## Files added so far (50 .cs + 3 JSON + 11 docs)

```
unity/
├── docs/                                   ← read these in order
│   ├── PHASE_0_FOUNDATION.md
│   ├── PHASE_1_MANAGERS.md
│   ├── PHASE_2_INPUT.md
│   ├── PHASE_3_AVATAR.md
│   ├── PHASE_4_FEEDBACK.md
│   ├── PHASE_5_PROFILE.md
│   ├── PHASE_6_7_8_CALIBRATION_SESSION.md
│   ├── PHASE_9_5_GARDEN_MEDITATION.md
│   └── FREE_PREFABS_AND_ASSETS.md
└── Assets/
    ├── Scripts/
    │   ├── Core/        Services, SceneEntry, Bootstrapper
    │   ├── Managers/    SceneFlow, User, Preference, Session, Calibration,
    │   │                SensorInput, Avatar, Feedback, AudioBus,
    │   │                Logging, MiniGameRouter
    │   ├── Input/       TrackingMode, UdpPacket, JointSnapshot,
    │   │                ISensorInputAdapter, Udp/Controller/Simulator adapters,
    │   │                UdpControlSender
    │   ├── Avatar/      HumanoidJointMap
    │   ├── Feedback/    IFeedbackChannel + 4 channel impls
    │   ├── Data/        UserProfile, UserPreferences, ROMProfile,
    │   │                SessionConfig, ExerciseResult
    │   ├── UI/          UiBindings extensions
    │   └── Scenes/      Welcome, Preferences, CalibrationA, CalibrationB
    │                    (config + log + controller + audio mgr), SessionSetup
    ├── Settings/        network.json, rom_calibration.json
    └── MiniGames/       ← per-game module folder (one game complete)
        ├── README.md                       ← template + workflow
        ├── _Shared/Scripts/MiniGameSceneEntry.cs
        └── GardenMeditation/
            ├── README.md
            ├── Scripts/                    Controller, Config, BreathingPacer,
            │                               FlowerBloomer, BreathingHud,
            │                               MicAmplitudeProbe
            └── Settings/garden_meditation_config.json
```

## Manager wiring (drop these on `[Managers]` for the Core scene)
1. `SceneFlowManager`
2. `UserManager`
3. `PreferenceManager`
4. `SessionManager`
5. `CalibrationManager`
6. `SensorInputManager` (with adapter children, see Phase 2 doc)
7. `AvatarDriver` (on the humanoid GameObject, not on `[Managers]`)
8. `FeedbackManager` (with channel children, see Phase 4 doc)
9. `AudioBus`
10. `LoggingManager` ← **new in Phase 9**
11. `MiniGameRouter` ← **new in Phase 9**

Bootstrapper will warn rather than fail if any manager is missing.

## SceneEntry-bearing scenes wired
- `Welcome`, `UserPreferences`, `Calibration_A`, `Calibration_B`, `SessionSetup`
- **`GardenMeditation`** ← first mini-game

## What you (the user) need to do next

**Step 1 — Fold UpperLimbRehabilitation in (one-time merge):**
```bash
cd ~/Documents/code/UpperBodyTracking
bash scripts/merge_unity_project.sh           # DRY RUN — read the plan
bash scripts/merge_unity_project.sh --apply   # do it
```
The merge script was updated after inspecting your actual project — it
auto-skips the macOS Finder duplicate `Assets/XR 1` … `XR 7` folders, the
9 GB `Library/`, the 47 MB APK, and `_BurstDebugInformation_…` /
`_BackUpThisFolder_…` directories. My old `Assets/Settings/` JSONs were
relocated to `Assets/StreamingAssets/` (where my code actually reads them),
so the URP renderer assets in your `Assets/Settings/` land without collision.

Read [`unity/docs/MERGE_PLAN.md`](unity/docs/MERGE_PLAN.md) for the full
inventory.

**Step 2 — Open the merged project in Unity 6.2** (Add via Unity Hub).
Run `Meta → Tools → Project Setup Tool` and resolve any red items.

**Step 3 — Build `Core` + scaffolding scenes** per the per-phase docs in
`unity/docs/`. The C# scripts I delivered will compile against the Meta XR
SDK v85 packages already in your `Packages/manifest.json`.

**Step 4 — Build the GardenMeditation scene** per
`Assets/MiniGames/GardenMeditation/README.md` — verify the per-game template
end-to-end on hardware.

**Step 5 — Commit and push:**
```bash
bash scripts/commit_phase_progress.sh
```

Then ping me to start **Flappy Bird** (Phase 9.1) — simplest joint-driven
game; validates IMU → game state end to end.
