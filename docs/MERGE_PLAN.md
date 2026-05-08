# Merging UpperLimbRehabilitation into this repo

Goal: one git repo, one Unity project, zero data loss.

## What was actually in your UpperLimbRehabilitation/

After mounting and inspecting the folder, here is the real state:

| Item | Detail |
|---|---|
| Unity version | **6000.4.4f1** (per ProjectVersion.txt) |
| Render pipeline | URP 17.4.0 |
| Meta XR SDK | **201.0.0** via UPM (`com.meta.xr.sdk.all`) |
| OpenXR | `com.unity.xr.openxr` 1.17.0 |
| Input | `com.unity.inputsystem` 1.19.0 + an `InputSystem_Actions.inputactions` asset |
| AndroidManifest | already lists Quest 3S in `com.oculus.supportedDevices` |
| Library size | 9.0 GB (gitignored, never moved) |
| Source git state | `Initial check-in` — single commit on `main` |
| URP template assets | `TutorialInfo/`, `Readme.asset` — fine to keep, harmless |
| Sound pack | `Assets/HintsStarsLite/` — third-party SFX, ~50 WAVs (we'll re-use these for game cues) |
| ⚠ macOS noise | `Assets/XR 1` … `Assets/XR 7` are Finder duplicate folders. They will GUID-collide with `Assets/XR/` in Unity. **The merge script skips them.** |
| Unwanted bulk | 47 MB `upperLimbRehabGame.apk`, `upperLimbRehabGame_BackUpThisFolder_…/`, `_BurstDebugInformation_DoNotShip/`, `Logs/`, `Temp/`. **All skipped.** |

## Final layout (after merge)

```
UpperBodyTracking/                          ← existing private GitHub repo
├── .git/                                   ← single source of git truth
├── .gitignore                              ← canonical Unity template + project additions
├── README.md  STATUS.md  IMPLEMENTATION_PLAN.md
├── VR_Rehab_Final_v3 (3).docx
├── config/                                 ← existing
├── src/                                    ← existing Python (Raspberry Pi side)
├── scripts/
│   ├── commit_phase_progress.sh
│   └── merge_unity_project.sh              ← does this merge
└── unity/                                  ← THE Unity 6000.4 project root
    ├── Assets/
    │   ├── Scripts/                        ← from this repo (untouched)
    │   ├── MiniGames/                      ← from this repo (untouched)
    │   ├── StreamingAssets/                ← from this repo: network.json, rom_calibration.json
    │   ├── Scenes/                         ← merged: SampleScene.unity (theirs) + my new ones (you create)
    │   ├── Settings/                       ← from theirs (URP renderer assets)
    │   ├── Resources/                      ← from theirs (Meta runtime assets)
    │   ├── Plugins/Android/                ← from theirs (AndroidManifest.xml — Quest 3S configured)
    │   ├── Oculus/                         ← from theirs (OculusProjectConfig.asset)
    │   ├── XR/                             ← from theirs (OpenXRLoader.asset)
    │   ├── HintsStarsLite/                 ← from theirs (SFX pack — useful!)
    │   ├── TutorialInfo/                   ← from theirs (URP template tutorial — safe to delete later)
    │   ├── Readme.asset                    ← from theirs (URP template Readme)
    │   └── InputSystem_Actions.inputactions ← from theirs
    ├── Packages/
    │   ├── manifest.json                   ← Meta XR 201 + Unity packages
    │   └── packages-lock.json              ← exact pin
    ├── ProjectSettings/                    ← all 24 .asset files from theirs
    ├── Library/                            ← Unity-generated, gitignored
    └── docs/                               ← my per-phase docs
```

## What `merge_unity_project.sh` does (and doesn't)

**Brings in (rsync from source):**
- All of `Assets/` except `XR 1` … `XR 7` macOS-duplicate folders.
- `Packages/manifest.json` + `packages-lock.json`.
- `ProjectSettings/*`.

**Skips:**
- `.DS_Store`, `._*` macOS noise.
- `.git/`, `.utmp/`, `Library/`, `Logs/`, `Temp/`, `Obj/`, `build/`, `Build/`, `Builds/`, `UserSettings/`.
- `*_BurstDebugInformation_DoNotShip/`, `*_BackUpThisFolder_ButDontShipItWithYourGame/`.
- `*.apk`, `*.aab`, `*.unitypackage`.
- Source's top-level `IMPLEMENTATION_PLAN.md`, `STATUS.md`, `.gitignore` — we already have ours.
- The `Assets/XR [0-9]` Finder duplicates.

**Backs up first:**
- Tar+gzip of the entire source (minus the giants above) to
  `~/UpperLimbRehabilitation.backup-<timestamp>.tar.gz` before any move.

**My scaffolding is preserved:**
- `unity/Assets/Scripts/` (40 .cs files)
- `unity/Assets/MiniGames/` (Garden Meditation + shared template)
- `unity/Assets/StreamingAssets/` (network.json, rom_calibration.json — relocated from old `Assets/Settings/` so the URP renderer assets from theirs land cleanly).

## Run the merge

```bash
cd ~/Documents/code/UpperBodyTracking

# 1. DRY RUN — prints exactly what would move, touches nothing.
bash scripts/merge_unity_project.sh

# 2. APPLY — backs up source then merges with rsync.
bash scripts/merge_unity_project.sh --apply

# 3. Open the merged project in Unity Hub:
#    Add → ~/Documents/code/UpperBodyTracking/unity/
#    First open rebuilds Library/ (5–15 min).

# 4. In Unity, run Meta → Tools → Project Setup Tool. Resolve any red items.

# 5. Commit the result.
bash scripts/commit_phase_progress.sh
```

## Custom source path

```bash
bash scripts/merge_unity_project.sh --apply --src=/absolute/path/to/UpperLimbRehabilitation
```

## Post-merge cleanup checklist

- [ ] Project opens in Unity 6000.4.4f1 without errors.
- [ ] `Meta → Tools → Project Setup Tool` is all green.
- [ ] `Assets/Scenes/SampleScene.unity` still loads.
- [ ] My scaffolding scripts compile (no missing-namespace errors).
- [ ] APK builds. `adb install -r` works.
- [ ] On Quest 3S the app holds 72 Hz baseline.
- [ ] Verified — then you can delete the original folder
      (`rm -rf ~/Documents/code/UpperLimbRehabilitation`). The tar backup is
      still safe at `~/UpperLimbRehabilitation.backup-<ts>.tar.gz`.

## Optional: the `Assets/XR [N]` duplicates

These folders only contain `OpenXRLoader.asset` re-references — Unity created
them when the XR package was probably re-installed or the folder duplicated by
Finder. The single canonical `Assets/XR/` folder has the original. The merge
script skips the duplicates entirely; once Unity reopens the project it will
write fresh GUIDs and the project will be clean.

If you want to clean them up at the source (before merging) for hygiene:
```bash
cd ~/Documents/code/UpperLimbRehabilitation/Assets
rm -rf "XR "[1-9]
rm -f  "XR "[1-9].meta
```

## If you want history from UpperLimbRehabilitation preserved

The default merge throws history away (only one "Initial check-in" commit
anyway, so very little is lost). If you do want it:

```bash
cd /tmp
git clone --bare ~/Documents/code/UpperLimbRehabilitation/.git ulr.git

cd ~/Documents/code/UpperBodyTracking
git remote add ulr-history /tmp/ulr.git
git fetch ulr-history
git merge --allow-unrelated-histories -X ours ulr-history/main
git remote remove ulr-history
```
