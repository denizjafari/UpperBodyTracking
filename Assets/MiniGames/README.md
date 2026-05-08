# Mini-game module template

Every game in `Assets/MiniGames/<GameName>/` is **self-contained**. A bug in one
game cannot break another, because they share *only* the manager interfaces
defined in `Assets/Scripts/Core/`.

## Folder layout (every game uses exactly this)

```
Assets/MiniGames/<GameName>/
├── README.md                              # joint mapping, levers, gate
├── Scripts/
│   ├── <GameName>Controller.cs            # extends MiniGameSceneEntry
│   ├── <GameName>Config.cs                # JSON-backed config
│   └── …game-specific helpers
├── Prefabs/                               # game-specific prefabs only
├── Materials/                             # game-specific materials only
├── Audio/                                 # game-specific audio only
├── Settings/
│   └── <gamename>_config.json
└── Scenes/
    └── <GameName>.unity                   # additive scene
```

The scene name MUST equal the `gameKey` used in `SessionConfig.exercises[i].game`,
because `MiniGameRouter` looks up scenes by that key.

## Common contract — `MiniGameSceneEntry`

(`Assets/MiniGames/_Shared/Scripts/MiniGameSceneEntry.cs`)

Every game's root SceneEntry extends this. It standardises:

```csharp
protected ExerciseEntry  ActiveEntry      { get; }   // today's session row
protected ExerciseResult Result           { get; }   // accumulating result
protected float          ElapsedSeconds   { get; }
protected bool           IsRunning        { get; }

protected abstract void OnGameStarted();             // ← implement
protected virtual  void OnGameTeardown() { }         // ← optional override

// Helpers:
protected void UpdateScore(float normalized01, int rawScore);
protected void CompleteGame();                       // writes result + advances
```

Lifecycle:
1. `MiniGameRouter` listens to `Services.Session.OnExerciseStarted`.
2. It calls `Services.Scenes.LoadAdditive(entry.game)`.
3. Your scene's root has a `MiniGameSceneEntry` subclass — its `OnEnter`
   captures the active `ExerciseEntry` and calls `OnGameStarted()`.
4. Your code calls `UpdateScore(...)` as the player accumulates points.
5. The base ticks `ElapsedSeconds` and auto-calls `CompleteGame()` when
   `durationSeconds` elapses. You may also call `CompleteGame()` early
   (e.g. Garden Meditation completes on hitting target cycles).
6. `CompleteGame()` writes the result via `LoggingManager`, raises
   `MiniGameSceneEntry.OnGameCompleted`, asks `SessionManager` to advance,
   and unloads the scene.

## Per-game implementation order (always)

1. **Skeleton scene** — empty `[<GameName>]` root + Controller, builds and loads.
2. **Input mapping** — subscribe to `Services.Sensors.OnPacket` (or use
   `Services.Sensors.GetAngle(joint)` polled in Update). Compute the 1D control
   signal the game needs.
3. **Visual hookup** — primitive geometry first (sphere, cube). Get the input
   moving something on screen.
4. **Game loop** — spawn / score / win-or-lose state machine.
5. **Difficulty curves** — implement the levers in `<GameName>Config.cs`.
6. **Feedback wiring** — confirm `Services.Feedback` events come through.
7. **Logging** — confirm an `exercise_results.ndjson` line lands per session.
8. **Polish** — swap primitives for imported prefabs, add particles, audio.

## Per-game verification gate (apply to every future game too)

- [ ] Scene loads/unloads additively without GameObject leaks.
- [ ] Plays correctly with `Simulator` input (no IMU needed for testing).
- [ ] Plays correctly with real IMU input on hardware.
- [ ] Difficulty 1 vs 5 is noticeably different.
- [ ] All compensation events from RPi reach `FeedbackManager` during play.
- [ ] An exercise-result row is written at end-of-game (or aborted=true on quit).
- [ ] Frame rate budget held: 72 Hz for passthrough games, 90 Hz for action.

## Recommended build order (from the implementation plan)

1. **Garden Meditation** ✅ done — validates the template.
2. **Flappy Bird** — simplest single-joint mapping.
3. **Pump the Bellows** — first cycle-detection game.
4. **Whack-a-Mole** — first reaction-time game.
5. **Rock Climbing** — first multi-joint game.
6. **Blocking Game**.
7. **Steering Wheel** — motion-sickness risk; do later.
8. **Glassblowing** — most novel input mapping; last.

## Why the template matters

Three concrete benefits when troubleshooting:
1. **Isolate a bug to one game.** Load Core + just that game's scene
   additively; the rest of the project is irrelevant.
2. **Prove a bug is in the manager layer, not the game.** Reproduce in two
   different games — if it shows up in both, it's not game-specific.
3. **Prove a bug is in the input layer, not the game.** Reproduce with the
   `Simulator` adapter — if it goes away, the problem is in IMU/UDP.
