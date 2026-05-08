# Phase 4 — Feedback system

## What landed

```
Assets/Scripts/Feedback/
├── IFeedbackChannel.cs
├── TextFeedbackChannel.cs        # world-space TMP panel, severity colors, fade
├── AudioFeedbackChannel.cs       # pre-baked TTS clip per compensation type
├── AvatarHighlightChannel.cs     # MaterialPropertyBlock flash on avatar
└── HapticChannel.cs              # OVRInput vibration with self-cancel

Assets/Scripts/Managers/
└── FeedbackManager.cs            # subscribes to SensorInput, gates per prefs, cooldown, message resolution
```

## Severity tiers (matches plan §4.3)
| Severity | Text | Audio | Haptic | Avatar highlight |
|---|---|---|---|---|
| 0.00 – 0.33 | green | — | — | — |
| 0.33 – 0.66 | yellow | gentle | short | — |
| 0.66 – 1.00 | red | strong | long | flash |

## Setup in the Editor
1. On `[Managers]`, add `FeedbackManager`.
2. As children, create one GameObject per channel and attach:
   - `TextFeedbackChannel` — drag a world-space TMP_Text + CanvasGroup into its slots.
   - `AudioFeedbackChannel` — populate the `clips` list (compensationType → clip).
   - `AvatarHighlightChannel` — drag the avatar `Renderer` into its slot.
   - `HapticChannel` — no setup required.
3. Drag the four channel components into the `FeedbackManager` slots.
4. Populate the `messages` list with one entry per compensation type, with `en` and
   optional `fr` strings (per design §4.3 "Compensation Message Strings").

## Test hook
```csharp
Services.Feedback.RaiseSyntheticCompensation("trunk_lean", 0.8f);
```
Use this from the Editor or a debug button to verify each channel fires.

## Verification gate 4
- [ ] Each compensation type triggers exactly the channels enabled in preferences.
- [ ] Toggling `Services.Preferences.SetHapticOn(false)` immediately stops haptics
      on the next packet without scene reload.
- [ ] A "stuck" compensation (held for 10 s) fires once every 2 s, not every frame.
- [ ] Severity 0.2 produces only green text; 0.5 produces yellow + audio + short
      haptic; 0.9 produces red + audio + long haptic + avatar flash.
- [ ] `git tag phase-4-feedback && git push --tags`.
