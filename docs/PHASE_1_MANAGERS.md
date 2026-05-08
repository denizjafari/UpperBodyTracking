# Phase 1 — Core scene & manager layer

## What landed in this phase

```
Assets/Scripts/Core/
├── Services.cs        # service-locator (statics) for the manager layer
├── SceneEntry.cs      # base class every additive scene's root must use
└── Bootstrapper.cs    # waits for all managers, then loads first additive scene

Assets/Scripts/Managers/
├── SceneFlowManager.cs    # additive scene load/unload, OnSceneReady event
├── UserManager.cs         # profile CRUD, per-user folder, ListAllUsers()
├── PreferenceManager.cs   # UserPreferences load/save, change events
├── SessionManager.cs      # session queue, AdvanceToNextExercise(), end hooks
├── CalibrationManager.cs  # calibration.json load/save, IsRomCalibrationRequired()
└── AudioBus.cs            # voice/sfx/ambience AudioSources behind one API

Assets/Scripts/Data/
├── UserProfile.cs
├── UserPreferences.cs
├── ROMProfile.cs       (also defines CalibrationState + JointKeys constants)
└── SessionConfig.cs
```

## Setting up the Core scene in the Editor

1. `File → New Scene → Basic (URP)` → save as `Assets/Scenes/Core.unity`.
2. Drop the `OVRCameraRig` prefab in. Configure `OVRManager`:
   - HandTrackingSupport = Controllers and Hands
   - TrackingOriginType  = Floor Level
   - Target Display Refresh Rate = 72
3. Create an empty GameObject named `[Managers]` and add these components:
   - `SceneFlowManager`
   - `UserManager`
   - `PreferenceManager`
   - `SessionManager`
   - `CalibrationManager`
   - `AudioBus`  (assign three AudioSource children: Voice / SFX / Ambience)
4. Create an empty GameObject named `[Bootstrapper]` and add `Bootstrapper`.
   - Set `firstSceneKey` to a debug scene key (e.g. `DebugCube`) for the gate.
5. Add Core to the very top of `Build Settings → Scenes In Build`. Mini-game scenes
   come below it. `_Bootstrap.unity` from Phase 0 can either (a) be replaced by
   `Core.unity` as the default scene, or (b) chain-load Core via `SceneManager.LoadScene("Core")`.

## API surface (what the rest of the codebase uses)

```csharp
// Anywhere:
Services.Scenes.LoadAdditive("Welcome");
Services.Users.CreateUser("Mary Smith");
Services.Preferences.SetHapticOn(false);

Services.Calibration.IsRomCalibrationRequired();   // bool
Services.Session.StartSession(SessionConfig.NewFor(userId));
```

## Verification gate 1
- [ ] Project compiles with no errors after dropping `Assets/` in.
- [ ] `Core.unity` plays in the Editor with all five required managers logging
      "[ManagerName] registered" lines (add a `Debug.Log` in each `Awake` if you want).
- [ ] `Bootstrapper.OnBooted` fires exactly once.
- [ ] Calling `Services.Scenes.LoadAdditive("DebugCube")` (a temporary scene
      with one cube and a `SceneEntry`) loads it, fires `OnSceneReady`, and
      `UnloadAdditive` removes it cleanly.
- [ ] Switching `UserManager.CurrentUserID` raises `OnUserChanged` exactly once.
- [ ] `git tag phase-1-managers && git push --tags`.
