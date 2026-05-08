# Unifying the project (final layout)

Goal: one folder, one git repo, no double-merge headaches. We keep
`UpperLimbRehabilitation/` as the root because:

- Unity expects `Assets/`, `Packages/`, `ProjectSettings/` at the project root —
  any nested layout (`unity/Assets/...`) means moving every asset and
  re-opening Unity rebuilds a 9 GB `Library/` from scratch.
- The AndroidManifest, Meta XR 201.0.0 packages, OpenXR config, URP renderer
  assets, and 50-WAV `HintsStarsLite/` SFX pack are all already in place here.
- All the Unity asset GUIDs in `.meta` files reference the existing folder
  paths. Moving things rewires those for free if Unity is closed; if not,
  it's an error.

## What's already pre-staged here

The sandbox already copied the scaffolding from `UpperBodyTracking/`:

```
UpperLimbRehabilitation/                       ← stays the working folder
├── .git/                                      ← will be re-pointed to GitHub
├── .gitignore                                 ← merged Unity + Python rules
├── IMPLEMENTATION_PLAN.md                     ← (already had it; identical)
├── STATUS.md                                  ← live tracker
├── VR_Rehab_Final_v3 (3).docx                 ← design doc
├── docs/                                      ← per-phase implementation guides
│   ├── PHASE_0_FOUNDATION.md
│   ├── PHASE_1_MANAGERS.md
│   ├── PHASE_2_INPUT.md
│   ├── PHASE_3_AVATAR.md
│   ├── PHASE_4_FEEDBACK.md
│   ├── PHASE_5_PROFILE.md
│   ├── PHASE_6_7_8_CALIBRATION_SESSION.md
│   ├── PHASE_9_5_GARDEN_MEDITATION.md
│   ├── FREE_PREFABS_AND_ASSETS.md
│   └── MERGE_PLAN.md                          ← obsolete (kept for reference)
├── scripts/
│   ├── commit_phase_progress.sh               ← stage + commit + push
│   └── unify_repo.sh                          ← one-time git remote re-point
├── src/                                       ← Python (Raspberry Pi IMU pipeline)
│   └── opensense_rt/
├── Assets/                                    ← UNITY PROJECT ASSETS (now at root!)
│   ├── Scripts/                               ← NEW — managers, input, avatar, feedback (43 .cs files)
│   ├── MiniGames/                             ← NEW — Garden Meditation + per-game template (10 files)
│   ├── StreamingAssets/                       ← NEW — network.json, rom_calibration.json
│   ├── Scenes/                                ← existing — SampleScene.unity
│   ├── Settings/                              ← existing — URP renderer
│   ├── Resources/                             ← existing — Meta runtime
│   ├── Plugins/Android/AndroidManifest.xml    ← existing — Quest 3S configured
│   ├── Oculus/                                ← existing — OculusProjectConfig
│   ├── XR/                                    ← existing — OpenXR loader
│   ├── HintsStarsLite/                        ← existing — SFX pack (~50 WAVs)
│   ├── TutorialInfo/                          ← existing — URP template (delete later if you want)
│   ├── Readme.asset                           ← existing
│   ├── InputSystem_Actions.inputactions       ← existing
│   └── XR 1/ … XR 7/                          ← existing macOS Finder dupes — see "Known issues"
├── Packages/                                  ← existing — Meta XR 201, OpenXR, URP, Input System
├── ProjectSettings/                           ← existing — Unity 6000.4.4f1 + XR + Player config
├── Library/                                   ← gitignored (9 GB, Unity-built — survives intact)
└── (gitignored) UserSettings/, Logs/, Temp/, build/,
              upperLimbRehabGame.apk, *_BurstDebugInformation_*, *_BackUpThisFolder_*
```

## Run the unification (one command, ~30 seconds)

```bash
cd ~/Documents/code/UpperLimbRehabilitation

# 1. Read the plan.
bash scripts/unify_repo.sh

# 2. Apply.
bash scripts/unify_repo.sh --apply
```

Default mode is **force-push**. The existing GitHub repo
`denizjafari/UpperBodyTracking` only has two commits at this point —
`Initial commit` (a README) and `Phase 0–9.5 scaffolding` (the same .cs files
you now have at `Assets/Scripts/` here). Both layouts contain the same content;
force-pushing replaces the old layout with the new clean layout. **No
content is lost** — the local `UpperBodyTracking/` folder still has
everything as a backup.

If you'd rather keep the existing GitHub history and merge, use:

```bash
bash scripts/unify_repo.sh --apply --merge
```

You'll likely need to resolve a small `unity/Assets/Scripts/` vs
`Assets/Scripts/` duplication afterwards — the `--merge` mode picks "ours"
(local) by default, but the old paths under `unity/Assets/...` may also
land. `git rm -rf unity/` then commit cleans that up.

## Known issues to clean up later (not blocking)

- **`Assets/XR 1` … `Assets/XR 7` are macOS Finder duplicate folders** of
  `Assets/XR/`. They will trigger Unity GUID-collision warnings when the
  project opens. Safe to delete:
  ```bash
  cd ~/Documents/code/UpperLimbRehabilitation/Assets
  rm -rf "XR "[1-9]
  rm -f  "XR "[1-9].meta
  ```
- **`Assets/TutorialInfo/` and `Assets/Readme.asset`** are leftovers from the
  Unity URP template. They don't hurt anything; remove when you feel like it.
- **`UpperLimbRehabilitation/.git/` had one commit** (`ade1891 Initial check-in`).
  After the unify, `git log` shows whatever the GitHub remote has — your old
  initial check-in commit is in the local reflog if you ever need it.

## Optional: rename the GitHub repo

The remote is named `UpperBodyTracking` but the project covers more than that.
Renaming on GitHub auto-redirects the old URL, so old clones still work.
Suggested names: `UpperLimbRehabilitation`, `VRRehab`, `RehabPlatform`. Your
call — purely cosmetic.

After renaming on GitHub, update the remote URL locally:
```bash
git remote set-url origin git@github.com:denizjafari/<new-name>.git
```

## After the unification, the old `UpperBodyTracking/` folder

Safe to archive once you've confirmed the unified repo works:
```bash
mv ~/Documents/code/UpperBodyTracking ~/Documents/code/.archive-UpperBodyTracking
# or just leave it — it's harmless
```
