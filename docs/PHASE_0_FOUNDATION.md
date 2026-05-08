# Phase 0 — Foundation (Unity Editor checklist)

> **Status:** mostly DONE — your existing `UpperLimbRehabilitation` project
> already satisfies most of this. After merging it into `unity/` (see
> `MERGE_PLAN.md`), the only items left here are confirmation steps.

These steps must be done **inside the Unity Editor on your machine** — they cannot be automated from a sandboxed shell. Confirm each box.

## 0.1 Unity Editor version
- [x] Unity **6000.4.4f1** is what `UpperLimbRehabilitation/ProjectSettings/ProjectVersion.txt` pins.
- [ ] Unity Hub → Installs → confirm 6000.4.4f1 is installed with the Android Build Support module + OpenJDK + Android SDK & NDK.

## 0.2 Project root
- [x] After running `bash scripts/merge_unity_project.sh --apply`, the Unity project root is `unity/` inside this repo.
- [ ] Unity Hub → Add → `~/Documents/code/UpperBodyTracking/unity/`. First open will rebuild `Library/` (5–15 min).

## 0.3 Color space (required for passthrough)
- [ ] `Edit → Project Settings → Player → Other Settings → Color Space = Linear`

## 0.4 Add Meta scoped registry
- [ ] `Edit → Project Settings → Package Manager → Scoped Registries → +`
  - Name: `Meta XR`
  - URL: `https://npm.pkg.github.com`
  - Scope: `com.meta.xr`
- [ ] Save.

## 0.5 Meta XR SDK packages (already pinned in Packages/manifest.json)
- [x] `com.meta.xr.sdk.all` **v201.0.0** is already in your `Packages/manifest.json`. This is the modern UPM-distributed All-in-One package — replaces the older "v85" Asset Store SDK naming.
- [x] `com.unity.xr.openxr` 1.17.0
- [x] `com.unity.inputsystem` 1.19.0
- [x] `com.unity.render-pipelines.universal` 17.4.0 (URP)
- [ ] If you need to add the Interaction or Movement sub-packages explicitly: open Package Manager → My Registries → Meta XR and add
  - `com.meta.xr.sdk.interaction`
  - `com.meta.xr.sdk.movement`
  - (the All-in-One package may already include these as transitive deps — check before re-adding)

## 0.6 Build target — already configured
- [x] `Assets/Plugins/Android/AndroidManifest.xml` is already configured for Quest 3S:
  - `com.oculus.supportedDevices = quest|quest2|questpro|quest3|quest3s`
  - VR head-tracking required (`uses-feature android.hardware.vr.headtracking`)
  - Horizon OS SDK targetSdkVersion=201, minSdkVersion=60
- [ ] Confirm in Player Settings → Android:
  - Scripting Backend: IL2CPP
  - Target Architectures: ARM64 only
  - Minimum API Level: 29 / Target API Level: 32+
  - Texture Compression: ASTC

## 0.7 XR plug-in management
- [ ] `Edit → Project Settings → XR Plug-in Management`
- [ ] Android tab → ✅ **Meta XR** (or **OpenXR** with Meta features below)
- [ ] OpenXR → Features:
  - ✅ Meta Quest Support
  - ✅ Hand Tracking Subsystem
  - ✅ Meta XR Passthrough
  - ✅ Meta XR Scene
  - ✅ Eye Gaze Interaction (only if you'll use eye tracking)

## 0.8 Run the Project Setup Tool
- [ ] `Meta → Tools → Project Setup Tool` → fix every red item.

## 0.9 Create the Core scene
The merge brings in `Assets/Scenes/SampleScene.unity`. We can either reuse that
or create a fresh `Core.unity` next to it (recommended — keeps the URP template
sample untouched as a reference).

- [ ] `File → New Scene → Basic (URP)` → save as `Assets/Scenes/Core.unity`
- [ ] Drop the **OVRCameraRig** prefab into the scene (search Project window for `OVRCameraRig`).
- [ ] On the OVRCameraRig, find the `OVRManager` component (or add one):
  - HandTrackingSupport: **Controllers and Hands**
  - TrackingOriginType: **Floor Level**
  - Target Display Refresh Rate: **72**
- [ ] Add an empty GameObject `[Managers]` and attach all manager scripts from `Assets/Scripts/Managers/` per `PHASE_1_MANAGERS.md`.
- [ ] Add an empty GameObject `[Bootstrapper]` and attach `Bootstrapper.cs` (from `Assets/Scripts/Core/`).

## 0.10 First device build
- [ ] Connect Quest 3S over USB-C.
- [ ] Enable Developer Mode on the headset (via Meta Quest mobile app once).
- [ ] `File → Build Settings → Add Open Scenes → Build And Run`.

## Verification gate 0
- [ ] APK builds with no errors.
- [ ] On the headset you see the ground plane and "Hello Quest" text.
- [ ] OVR Metrics Tool reports steady **72 Hz** and GPU < 8 ms.
- [ ] `git tag phase-0-foundation && git push --tags`
