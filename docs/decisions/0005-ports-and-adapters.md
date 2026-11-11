# 0005 — Use Ports & Adapters at technical boundaries

- **Status:** Accepted
- **Date:** 2026-11-11

## Context

DeskMate is a Windows-native, programmable 2D puppet controlled by external applications. Its presentation behavior must not be coupled to the transport used by controllers, to an audio device API, or to a speech provider. We also want meaningful automated tests without a real desktop, microphone, speech provider, or SignalR server.

That is **not** a request for cross-platform support or a generalized plugin framework.

## Decision

Use **Ports & Adapters (hexagonal architecture)** as a design principle:

- **Core**: puppet state, presentation commands, input events, behavior coordination, animation policy and application use cases. It contains **no AI reasoning or external agent orchestration**.
- **Driving/primary adapters**: translate external requests (initially SignalR) into application commands and publish events back through the external contract.
- **Driven/secondary ports**: define the capabilities required by use cases, independently of any library or vendor.
- **Driven adapters**: implement these capabilities using concrete Windows APIs, WPF, audio libraries or speech providers.
- **Composition root**: assemble the chosen ports/adapters in the Windows host, keeping infrastructure types out of core-facing contracts.

Dependencies point **toward** the core and its owned contracts. No SignalR hub types, WPF controls, Win32 handles, NAudio types, Windows speech types or Azure Speech types may leak into those contracts.

### Initial candidate ports

| Port (illustrative name) | Capability | Potential adapter |
| --- | --- | --- |
| `ISpeechSynthesizer` | Text to synthesized audio, with optional timed visemes | Windows local TTS by default; optional other engines |
| `IAudioPlayer` | Playback, streaming, stop/cancel, playback timing | WASAPI / NAudio |
| `IAudioCapture` | Record microphone audio and expose captured samples | WASAPI / NAudio |
| `ICharacterRenderer` | Render independently animated sprite layers | WPF |
| `ICharacterWindow` | Position and interact with the desktop window | WPF + targeted Win32 interop |

These names are **illustrative**, not mandatory interfaces or assembly boundaries. Split or combine ports based on tested use cases rather than copying the table mechanically.

The five sprite layers, anchors, independent animation tracks, state transitions and viseme-to-mouth presentation are **core DeskMate behavior**; their concrete WPF drawing is not.

### Rules

1. **Abstract real boundaries, not every class.** No speculative interface-per-class design.
2. **Keep contracts technology-neutral**, including audio buffers/timing, synthesis results and optional viseme data.
3. **No dependency on an adapter in the core.** For example, `Speak` invokes a speech port, not Windows or Azure APIs directly.
4. **Capabilities may be optional.** A TTS provider without visemes is usable; lip sync can fall back to an audio-amplitude animation. Some configurations can run without a speech provider.
5. **Do not confuse application orchestration with agent intelligence.** Coordinating audio playback with a speech bubble and sprites belongs in DeskMate; deciding what to say belongs to the external controller.
6. **Test application behavior using fake ports**, reserving actual WPF, device and networking integration for targeted integration tests.
7. Favor straightforward composition and **avoid overengineering**. Windows quality remains the priority; see [0002](0002-windows-native-desktop.md).

## Logical organization (provisional)

- `DeskMate.Core` — puppet commands, use cases, state, events, contracts/ports.
- `DeskMate.Animation` — 2D animation model and scheduling (could be part of Core if simpler).
- `DeskMate.Desktop` — Windows/WPF host and native integration.
- SignalR transport adapter and the audio/speech adapters, named according to their actual responsibility.

These are **logical boundaries, not a requirement for one assembly per entry**.

## Consequences and follow-ups

- A new speech provider or transport does not require changing the puppet's behavior model.
- The Windows-only desktop can still use Win32-specific functionality without leaking it into the core.
- Too many tiny interfaces would add maintenance cost; use case pressure should justify each one.
- The first implementation should validate these boundaries end-to-end, not build a general-purpose framework.

Related decisions: [programmable puppet](0001-assistant-scope.md), [open control API](0004-open-control-api.md), and [voice and speech capabilities](0006-voice-and-speech.md).
