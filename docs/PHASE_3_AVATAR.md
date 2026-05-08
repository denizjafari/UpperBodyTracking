# Phase 3 — Avatar driver

## What landed

```
Assets/Scripts/Avatar/
└── HumanoidJointMap.cs       # joint key → HumanBodyBones + local axis

Assets/Scripts/Managers/
└── AvatarDriver.cs           # procedural bone drive + mirror + demo-clip hook
```

## Setup in the Editor
1. Import a humanoid model (Mixamo Y-Bot is free; also works with the Movement SDK
   sample avatar).
2. In the Model importer: Rig → Animation Type **Humanoid**, Configure → Apply.
3. Drop the model into Core scene as `[Avatar]`.
4. Add an `Animator` (use any controller — it can be empty, we drive bones in LateUpdate).
5. Add `AvatarDriver` to the same GameObject. Drag the `Animator` into its slot.
6. Bake five short demo clips per design §3.8 and place under `Assets/Animations/Calibration/`:
   - `demo_shoulder_abduction`, `demo_shoulder_flexion`, `demo_elbow_flexion`,
     `demo_shoulder_rotation`, `demo_wrist_pronation`.
7. Wire those clips into the Animator controller as states whose names match the demo keys.

## Refresh-rate switching
Per the plan: lock to 72 Hz when passthrough is on, 90 Hz for fully immersed action games.
Switch at scene load with:
```csharp
OVRManager.display.displayFrequency = 90f;   // or 72f
```

## Mirror mode
`AvatarDriver.SetMirror(true)` swaps the right-arm bones for the left-arm bones
*and* negates the angle, so a patient watching head-on sees the avatar's arm
match theirs. Used by Calibration A.

## Verification gate 3
- [ ] With Simulator input, the right-arm bones visibly track the sine wave at ~50 Hz.
- [ ] Setting `mirror = true` produces a natural mirror image (no upside-down arm).
- [ ] `PlayDemo("demo_shoulder_abduction")` plays the clip; `StopDemo()` returns
      cleanly to procedural drive without an animation pop.
- [ ] Profiler: AvatarDriver costs < 0.4 ms CPU per frame on Quest 3S.
- [ ] `git tag phase-3-avatar && git push --tags`.
