# Proof of concept v0.1

The first runnable slice of DeskMate. It validates the architecture end to end — **external controller → SignalR → puppet core → WPF window / Windows TTS → user events back to the controller** — with the smallest useful feature set. It follows the PoC scope set by the [sprite production guide](art/ai-sprite-production-guide.md#12-ce-que-nous-ne-décidons-pas-encore) (standing pose, idle animation, short spoken sentence) and the accepted ADRs.

## What works

| Capability | Implementation | ADR |
| --- | --- | --- |
| Transparent, borderless, topmost character window | WPF layered window (`AllowsTransparency`): transparent pixels let the mouse through | [0002](decisions/0002-windows-native-desktop.md) |
| Drag to reposition, click / right-click events, context menu | `CharacterWindow` | 0002 |
| Text bubble, user text input (double-click → type → Enter) | `CharacterWindow` → `messageSubmitted` event | [0004](decisions/0004-open-control-api.md) |
| Eased movement with completion/cancellation | `Puppet.Move` | 0004 |
| Vertical gaze (8 frames, up → down), look at a screen point | `GazeSelector` | [0003](decisions/0003-layered-sprite-animation.md) |
| Idle behaviour: breathing + random glances, suspended by explicit gaze or speech | `Puppet` idle loop, WPF animation | 0003 |
| Local text-to-speech, mouth animation driven by playback visemes | `WindowsSpeechSynthesizer` (System.Speech / SAPI) → `VisemeMouth` | [0006](decisions/0006-voice-and-speech.md) |
| Local SignalR control API, `accepted` vs `completed` operations | `DeskMate.Transport.SignalR` — see [protocol](protocol.md) | 0004 |
| Ports & Adapters, tests with fake ports | `ICharacterStage`, `ISpeechSynthesizer`, `IPuppetEventSink` | [0005](decisions/0005-ports-and-adapters.md) |

## Source layout

```
src/DeskMate.Core               Puppet behaviour, animation policy, ports (no WPF, no SignalR)
src/DeskMate.Transport.SignalR  Driving adapter: hub, wire contracts, event broadcasting
src/DeskMate.Desktop            WPF host (DeskMate.exe): window, stage adapter, Windows TTS, composition root
tests/                          Core unit tests (fake time/ports) + SignalR end-to-end tests (fake stage)
samples/                        Controllers in C# and Node.js that only use the documented protocol
assets/characters/default       Sprite frames + character.json; source/ holds the original sheet
tools/extract_sprites.py        Chroma-key sheet → aligned transparent frames
```

`DeskMate.Animation` from the ADRs is, for now, the `Animation` folder of the core: it is too small to justify an assembly.

## Running it (Windows)

Requirements: Windows 10/11 and the .NET 10 SDK.

```bash
dotnet run --project src/DeskMate.Desktop
```

Options: `--port 5117`, `--scale 0.45`, `--voice "Microsoft Hortense Desktop"`, `--no-tts`. The character appears at the bottom-right of the screen and announces its endpoint. Then, in another terminal:

```bash
dotnet run --project samples/DeskMate.Samples.Console
```

Double-click the character, type a message and press Enter: the sample receives `messageSubmitted` and answers in the bubble. Right-click → *Quit DeskMate* to exit.

Tests run on any OS (the WPF host also builds on Linux/macOS thanks to `EnableWindowsTargeting`, but only runs on Windows):

```bash
dotnet test DeskMate.slnx
```

## Character assets

The PoC character comes from a single chroma-key sheet (8 full-body frames, gaze from up to down, on magenta). `tools/extract_sprites.py` detects the frames, keys out the magenta with soft edges and de-spill, and exports them on a common canvas aligned on the feet so that switching frames does not make the character jump:

```bash
python3 tools/extract_sprites.py assets/characters/default/source/gaze-sheet.webp assets/characters/default
```

`character.json` declares the frame size, the feet anchor (the point that `Move` positions), the head anchor (for `LookAtPoint`), the neutral pose and the pitch of each frame.

## Known limitations — not validated yet

- **Only one sprite layer.** Frames are whole characters: no separate head/eyes/mouth, so no blinking, no horizontal gaze, and lip sync is a **placeholder** (a small vertical bob driven by the viseme openness). The core already computes per-viseme mouth openness; real mouth sprites only need a renderer change.
- **Not run on Windows by the author of this PoC**: the code compiles and the core/transport are tested, but the WPF window, hit-testing and SAPI behaviour must be checked manually on Windows (see checklist below).
- Coordinates are WPF DIPs; per-monitor DPI with mixed scaling and multi-monitor edge cases are untested.
- No authentication, single-controller assumptions, events broadcast to all clients.
- No external audio (`PlayAudio`), no microphone, no tray icon, no persistence of position.

## Manual checklist on Windows

- [ ] The character is drawn without any rectangle; clicking next to it reaches the window behind.
- [ ] Dragging moves it; a simple click emits `clicked`; a drag does not.
- [ ] Double-click opens the input; Enter emits `messageSubmitted`; Esc closes it.
- [ ] `Move` is smooth, and dragging during a move cancels it.
- [ ] Idle glances and breathing are visible but discreet; CPU stays low at rest.
- [ ] `Speak` is audible, the bob follows the voice and stops with it; `StopSpeaking` interrupts.
- [ ] Behaviour at 125 % / 150 % display scaling.

## Next steps

1. Validate the checklist above and fix what the real desktop reveals.
2. Produce layered assets (head, eyes, mouths) following the sprite guide, then add blinking and real viseme mouths.
3. `PlayAudio` for externally synthesized audio, with amplitude-based mouth fallback.
4. Authentication / controller arbitration, then a published JSON schema of the protocol.
