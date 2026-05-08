# Free prefabs & assets — curated directory

Every asset listed here is free for commercial use. Always re-confirm the
license on the asset's page before shipping — terms can change.

> **License legend:** CC0 = no attribution required · CC-BY = credit author ·
> Mixamo = Adobe-account-required, free for any use including commercial ·
> Asset Store Free = Unity-account login, royalty-free.

---

## Avatars (Phase 3 — AvatarDriver)

Pick **one** as the primary patient-facing humanoid. All work as Unity Humanoid
rigs — that's the only requirement for `AvatarDriver` to find the bones.

| Source | What you get | License | Notes |
|---|---|---|---|
| **[Mixamo](https://www.mixamo.com)** (Adobe) | Auto-rigged humanoid characters + thousands of animations. Y-Bot is the de-facto free placeholder. | Mixamo | Best first pick — stable rig, clean T-pose, exports to FBX. Requires free Adobe account. |
| **[Quaternius — Animated Base Character](https://poly.pizza/m/cwYvO5UauX)** | Low-poly stylized humanoid + base anim set | CC0 | Friendlier visual style than Mixamo Y-Bot; good for the meditative games. |
| **[Ready Player Me](https://readyplayer.me)** | Customizable avatars, downloadable as glTF | RPM EULA (commercial OK) | Best if you want patients to make their own avatar — extra integration work. |
| **[Meta Movement SDK sample avatar](https://developers.meta.com/horizon/documentation/unity/move-overview/)** | Ships with the SDK; tested against Movement SDK retargeting | Meta SDK license | Use if/when you switch from UDP IMU to Quest body tracking. |

**Recommendation:** Mixamo Y-Bot for the MVP. Swap in a Quaternius character once
the visual polish phase begins.

---

## Demo animations (Phase 7 — ROM Calibration)

You need looped demos for: shoulder abduction, shoulder flexion, elbow flexion,
shoulder rotation, wrist pronation. Easiest path:

1. Open Mixamo, pick Y-Bot.
2. Search clip names like "Stretching Idle", "Side Arm Stretch", "Bicep Curl",
   "Wrist Twist".
3. Download "FBX for Unity" with "In Place" checked.
4. Trim to ~3 s in Unity, set `Loop Time = true`.
5. Drop into the Animator controller as named states matching `PlayDemo("...")`.

Alternative: record your own using a free WebcamMocap pipeline like **[Rokoko Vision](https://www.rokoko.com/products/vision)** (free tier).

---

## Mini-game environment / prop assets

### Game-by-game cheat-sheet

| Mini-game | Asset family | Best free source |
|---|---|---|
| Flappy Bird (9.1) | Bird, pipes, sky/clouds | [Sketchfab — low poly bird (Kyyy_24)](https://sketchfab.com/3d-models/low-poly-bird-7a0d16af3b9749018f32f698ecb0169d) · [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) for clouds/trees · build pipes from primitives. |
| Rock Climbing (9.2) | Climbing wall, holds, rocks | [Sketchfab — climbing-wall tag](https://sketchfab.com/tags/climbing-wall) · [Kenney Nature Kit rocks](https://kenney.nl/assets/nature-kit) · [Sketchfab Low Poly Rocks](https://sketchfab.com/3d-models/low-poly-rocks-9823ec262054408dbe26f6ddb9c0406e) |
| Whack-a-Mole (9.3) | Mole, mallet, holes | [Quaternius — Animated Animals Pack](https://quaternius.com/) (free CC0) · build holes from cylinder primitives. |
| Steering Wheel (9.4) | Cockpit, steering wheel, road, car | [Kenney — Racing Kit / Car Kit](https://kenney.nl/assets) · [Quaternius — Vehicles Ultimate](https://quaternius.com/) |
| Garden Meditation (9.5) | Garden, flower bloom, particles | [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) · [Quaternius — Stylized Nature MegaKit](https://quaternius.com/) |
| Blocking (9.6) | Soft projectiles, target dummies | Primitives + Kenney Particle Pack |
| Pump the Bellows (9.7) | Bellows, anvil, forge, fire | [Sketchfab — Forge (Low Poly) by Larolei](https://sketchfab.com/3d-models/forge-low-poly-09191a31600f484892e3ea8835a65a2a) · [Sketchfab — Low Poly Blacksmith Workshop by Yanez Designs](https://sketchfab.com/3d-models/low-poly-blacksmith-workshop-0b9365ca96f04faf860174ee180ec588) |
| Glassblowing (9.8) | Furnace, blowpipe, glass blob | Reuse the forge asset above for the furnace; build the pipe from a stretched cylinder; use a sphere with custom shader for molten-glass look. |

### Catch-all sources
- **[Kenney.nl assets](https://kenney.nl/assets)** — 30 000+ free CC0 game assets across 3D, 2D, audio, fonts. *Single most useful site for this project.*
- **[Quaternius](https://quaternius.com/)** — Large library of low-poly CC0 3D models, all optimized for Unity.
- **[Poly Pizza](https://poly.pizza)** — Curated portal for browsing Quaternius/CC0 models with one-click download.
- **[Sketchfab CC0 tag](https://sketchfab.com/tags/cc0)** — 100 000+ models marked CC0; downloads in glTF, FBX, OBJ.
- **[OpenGameArt.org](https://opengameart.org)** — Older, mixed quality, many CC0 models and audio.

---

## Audio (Phase 4 feedback channels + ambient mini-game music)

### TTS for compensation messages and calibration narration
- **[ElevenLabs Free](https://elevenlabs.io)** — 10 000 chars/month free tier; export 22 kHz WAV.
- **[Coqui TTS](https://github.com/coqui-ai/TTS)** — Self-hosted open-source. Best if you want to bake unlimited clips without per-call cost.
- **Alternative:** record clinician's voice in Audacity for warmer/more human delivery.

### SFX & ambient
- **[Pixabay Sounds](https://pixabay.com/sound-effects/)** — Royalty-free, no attribution required, MP3 download. Search "click", "success", "alert", "garden ambient".
- **[Freesound.org](https://freesound.org)** — Largest CC-licensed sound database. Filter by license = CC0 to skip attribution headaches.
- **[Kenney Audio](https://kenney.nl/assets?q=audio)** — UI Sounds, Game Sounds, Casino Sounds packs — all CC0.

### Music
- **[Free Music Archive](https://freemusicarchive.org)** — CC-BY ambient and meditation tracks.
- **[Pixabay Music](https://pixabay.com/music/)** — Royalty-free meditation / ambient loops for Garden Meditation and Glassblowing.

---

## UI / fonts

- **TextMeshPro** — bundled with Unity. Use it for *all* in-game text.
- **[Google Fonts](https://fonts.google.com)** — pick 1 humanist sans (e.g. *Inter*, *Source Sans 3*) for body and 1 contrasting display face for headings. Generate a TMP atlas in Unity once.
- **Iconography:** **[Lucide](https://lucide.dev)** (MIT) — clean SVG icons, render to texture or use as SVG-Importer.

---

## Meta SDK Building Blocks (drag-and-drop scene primitives)

These ship with the Meta XR SDK v85 and replace large amounts of bespoke code:

- **Camera Rig** — pre-configured `OVRCameraRig`.
- **Hand Tracking** — drop-in hand visuals + interaction.
- **Passthrough** — adds the layer composition for color passthrough in one click.
- **Real Hands**, **Occlusion**, **Anchor Prefab Spawner**, **Room Guardian**.
- Open via `Meta → Tools → Building Blocks` once SDK v85 is installed.

📚 [Building Blocks docs](https://developers.meta.com/horizon/documentation/unity/bb-overview/)
📚 [Mixed Reality Utility Kit (MRUK)](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-gs/) — high-level scene API for room-aware games.

---

## Workflow tips

1. **One pass per game.** When you start Game 9.1 (Flappy Bird), open this doc,
   pick the assets for that game, download them all once into
   `Assets/MiniGames/FlappyBird/Imported/`, then close the browser. Asset hunting
   is a giant context-switch cost — batch it.
2. **Store originals separately.** Keep raw downloaded FBX/OBJ files outside
   `Assets/` (e.g. in a `RawAssets/` folder at the project root) and import only
   what you actually use. Saves disk + commit weight.
3. **Compress everything.** On import, switch textures to ASTC (Quest 3S native)
   and meshes to no-read-write unless a script needs CPU access. The plan's
   draw-call and texture-memory budget (Phase 12) is tight.
4. **Audit licenses in one place.** Add a `THIRD_PARTY_LICENSES.md` at the repo
   root and append every asset you ship, with author + license + URL. Saves a
   panic later if Anthropic submission asks for it.
5. **Prefer CC0 where you have a choice.** Avoids the credit-list grind.

---

## Sources used in this document
- [Kenney free assets](https://kenney.nl/assets) (CC0)
- [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) (CC0)
- [Quaternius free game assets](https://quaternius.com/) (CC0)
- [Poly Pizza Quaternius browser](https://poly.pizza/m/cwYvO5UauX)
- [Sketchfab CC0 collection](https://sketchfab.com/tags/cc0)
- [Sketchfab — Low Poly Bird](https://sketchfab.com/3d-models/low-poly-bird-7a0d16af3b9749018f32f698ecb0169d)
- [Sketchfab — Forge (Low Poly)](https://sketchfab.com/3d-models/forge-low-poly-09191a31600f484892e3ea8835a65a2a)
- [Sketchfab — Low Poly Blacksmith Workshop](https://sketchfab.com/3d-models/low-poly-blacksmith-workshop-0b9365ca96f04faf860174ee180ec588)
- [Sketchfab — climbing-wall tag](https://sketchfab.com/tags/climbing-wall)
- [Sketchfab — Low Poly Rocks](https://sketchfab.com/3d-models/low-poly-rocks-9823ec262054408dbe26f6ddb9c0406e)
- [Mixamo](https://www.mixamo.com)
- [Pixabay Sound Effects](https://pixabay.com/sound-effects/)
- [Freesound.org](https://freesound.org)
- [Meta Building Blocks docs](https://developers.meta.com/horizon/documentation/unity/bb-overview/)
- [Meta MRUK getting started](https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-gs/)
