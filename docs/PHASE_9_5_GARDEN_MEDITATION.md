# Phase 9.5 — Garden Meditation

Garden Meditation is the **per-game template validation game** — the smallest
mini-game in the project, deliberately built first so we exercise every part of
the module template before reaching for joint angles.

## What landed

```
Assets/Scripts/Data/
└── ExerciseResult.cs                                # what every game writes at end

Assets/Scripts/Managers/
├── LoggingManager.cs                                # phase-11 wedge: NDJSON file per user
└── MiniGameRouter.cs                                # session entry → scene loader

Assets/MiniGames/_Shared/Scripts/
└── MiniGameSceneEntry.cs                            # base class for every mini-game

Assets/MiniGames/GardenMeditation/
├── README.md
├── Settings/garden_meditation_config.json           # 4-7-8 breathing default
└── Scripts/
    ├── GardenMeditationController.cs                # MiniGameSceneEntry subclass
    ├── GardenMeditationConfig.cs
    ├── BreathingPacer.cs                            # inhale → hold → exhale state
    ├── FlowerBloomer.cs                             # tension → scale + per-cycle growth
    ├── BreathingHud.cs                              # world-space HUD with fill ring
    └── MicAmplitudeProbe.cs                         # optional Quest 3S mic input
```

Total new code for this phase: 8 .cs files, 1 README, 1 JSON.

## How it integrates with everything that came before

- **Phase 1 (managers):** `LoggingManager` self-registers; `MiniGameRouter`
  added to `[Managers]` listens to `Services.Session.OnExerciseStarted` and
  loads the matching scene.
- **Phase 2 (input):** the game ignores joint packets entirely — proving
  `Services.Sensors` listeners are optional and don't crash unaware games.
- **Phase 3 (avatar):** unused (the patient sees a flower, not their avatar).
- **Phase 4 (feedback):** unused — Garden Meditation has no compensation events.
  This confirms FeedbackManager is dormant when no compensation packets arrive.
- **Phase 8 (session setup):** when "Garden Meditation" is in the routine, the
  `MiniGameRouter` loads its scene additively at the right time.

## Manager wiring update for Phase 1's Core scene

Add these new components on `[Managers]`:
- `LoggingManager` (no inspector setup needed).
- `MiniGameRouter` (no inspector setup needed unless you want an interstitial scene).

Re-run **Verification Gate 1** after adding them — Bootstrapper's
"missing (Phase 11)" warning for LoggingManager should now disappear.

## Key script — `MiniGameSceneEntry`

The base class every future game uses. Lifecycle and contract are documented
inline in the file; see also [`Assets/MiniGames/README.md`](../Assets/MiniGames/README.md)
for the full per-game template guide.

## Day-1 build (no asset imports required)

1. Create `Assets/Scenes/GardenMeditation.unity`.
2. Add to **Build Settings**.
3. Single root GameObject `[GardenMeditation]` with `GardenMeditationController`.
4. Children:
   - `[Pacer]` empty + `BreathingPacer`.
   - `[Flower]` empty + `FlowerBloomer`. As a child of `[Flower]`, add a sphere
     primitive — drag the sphere into `bloomTransform`.
   - `[HUD]` world-space Canvas at ~1.2 m / eye level + `BreathingHud`. Add a
     TMP_Text for `phaseLabel`, another for `instructionLabel`, and a UI Image
     with `Image Type = Filled` for `progressRing`.
   - `[Mic]` empty + `MicAmplitudeProbe` (leave `enableProbe` off for day 1).
   - `[Ambience]` empty + AudioSource, point at any 60-s ambient WAV.
5. Drag the four scene-wiring components into the Controller's slots.

## Day-2 polish (when the day-1 build passes the gate)

- Real flower mesh from [Quaternius nature kit](https://quaternius.com/) (CC0).
- Ground / trees from [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) (CC0).
- Ambient music: any [Pixabay meditation track](https://pixabay.com/music/search/meditation/).
- Particles: a short bloom burst on `BreathingPacer.OnFullCycle`.

## Score model

```
normalized01 = min(1, completedCycles / targetCycles)
targetCycles = max(1, round(durationSeconds / (inhale + hold + exhale)))
```

A 3-min session at the 4-7-8 default targets ≈ 9 cycles. Hitting the target
ends early with `score = 1.0`.

## Verification gate 9.5

- [ ] Scene loads/unloads additively with no GameObject leak.
- [ ] Without IMUs, the breathing cycle runs for the configured duration and
      writes one line to `Users/<id>/logs/exercise_results.ndjson`.
- [ ] Quitting mid-game writes a row with `aborted=true`.
- [ ] Frame rate locked at 72 Hz throughout (passthrough on).
- [ ] `MiniGameSceneEntry.OnGameCompleted` fires exactly once per game.
- [ ] `git tag phase-9.5-garden && git push --tags`.
