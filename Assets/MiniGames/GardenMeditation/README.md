# Garden Meditation (mini-game 9.5)

**Joint mapping:** none — purely time-based, optionally microphone amplitude.
**Difficulty levers:** session duration only (per design §10.1).
**Why first:** smallest game, validates the per-game module template before
multi-joint and reaction-time games are built on top.

## Scripts (`Scripts/`)
| File | Role |
|---|---|
| `GardenMeditationController.cs` | `MiniGameSceneEntry` — entry point, score model, cleanup |
| `GardenMeditationConfig.cs` | JSON-backed config (inhale / hold / exhale seconds, mic enable, ambience clip name) |
| `BreathingPacer.cs` | State machine for inhale → hold → exhale; emits `OnPhaseChanged` and `OnFullCycle` events |
| `FlowerBloomer.cs` | Visualises tension as scale + rotation; grows over completed cycles |
| `BreathingHud.cs` | World-space TMP HUD: phase label, instruction text, fill ring |
| `MicAmplitudeProbe.cs` | Optional Quest 3S mic input — smoothed amplitude 0..1 |

## Scene setup
1. New scene `Assets/Scenes/GardenMeditation.unity` (also acceptable: place under
   `Assets/MiniGames/GardenMeditation/Scenes/GardenMeditation.unity`). The scene
   *name* must equal `"GardenMeditation"` because that's the `gameKey` the
   `MiniGameRouter` uses.
2. Add to **Build Settings → Scenes In Build**.
3. Single root GameObject `[GardenMeditation]`. Attach
   `GardenMeditationController`. Children:
   - `[Pacer]` empty + `BreathingPacer`.
   - `[Flower]` GameObject — drop a sphere primitive here for the MVP build.
     Attach `FlowerBloomer`, drag `BreathingPacer` into its `pacer` slot, drag
     a child Transform (the sphere) into `bloomTransform`.
   - `[HUD]` world-space Canvas at ~1.2 m / eye level + `BreathingHud` script.
     Slot in a TMP_Text for `phaseLabel`, another for `instructionLabel`, and
     a UI `Image` with `Image Type = Filled` for `progressRing`.
   - `[Mic]` empty with `MicAmplitudeProbe` (optional).
   - `[Ambience]` empty with an `AudioSource`. Point it at a long ambient WAV.
4. On `GardenMeditationController`, drag the four scene-wiring components into
   their slots.

## Day-1 prefab plan (no imports needed)
- Flower: a Unity primitive sphere with a green emissive URP material.
- Garden: a single ground plane with a soft tree silhouette billboard.
- Audio: any 60-second ambient loop from
  [Pixabay nature sounds](https://pixabay.com/sound-effects/search/nature/)
  (no attribution).

This lets you verify the whole pipeline (SessionSetup picks GardenMeditation →
MiniGameRouter loads scene → BreathingPacer cycles → ExerciseResult logged)
before importing real assets.

## Day-2 polish (optional, when validation works)
- **Flower mesh:** [Quaternius Stylized Nature MegaKit](https://quaternius.com/) (CC0).
- **Ground / trees:** [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) (CC0).
- **Ambient music:** [Pixabay Music — meditation tag](https://pixabay.com/music/search/meditation/).
- **Particle bloom:** Unity's built-in particle system; trigger one short burst
  on each `OnFullCycle` event for a subtle "you completed a breath" reward.

## Score model
```
normalized = min(1, completedCycles / targetCycles)
targetCycles = max(1, round(durationSeconds / (inhale + hold + exhale)))
```
A 3-minute session at the 4-7-8 default pattern targets ~9 full cycles
(180 / 19 ≈ 9.4 → 9). Hitting the target ends the game early with score = 1.0.

## Verification gate (per-game template, applies to every future game too)
- [ ] Scene loads/unloads additively without leaking GameObjects (compare
      `FindObjectsOfType<Transform>().Length` immediately before LoadAdditive
      and after UnloadAdditive — should match).
- [ ] Plays correctly without IMU input.
- [ ] Plays correctly with simulator input (which Garden Meditation ignores —
      good baseline check that ignoring sensor stream doesn't crash).
- [ ] An `exercise_results.ndjson` line is appended to
      `Users/<id>/logs/exercise_results.ndjson` at end-of-game with `score`,
      `rawScore` (cycles), and `aborted=false`.
- [ ] Quitting mid-game writes a record with `aborted=true`.
- [ ] Frame rate sustained at 72 Hz throughout (Quest 3S Fresnel + passthrough).
- [ ] `git tag phase-9.5-garden && git push --tags`.
