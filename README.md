# DeskMate

A programmable Windows desktop puppet: an animated character and interactive console for external assistants, agents, tools, and scripts.

DeskMate is **not an intelligent assistant or agent in itself**. It is a small, expressive **presentation and interaction surface**. External applications decide what to do; DeskMate animates the character, displays messages, plays audio when supplied, and passes user interactions back to its controllers. No AI runtime is required to use it.

## Direction

- **Windows-native**: C# / .NET 10, WPF, and targeted Win32 interop; prioritize the Windows experience over cross-platform compatibility.
- **Lightweight 2D character**: composable sprites (body, head, eyes, mouth, and accessories), independent animation tracks, and optional viseme-driven lip sync.
- **Programmable**: move, animate, look around, show text bubbles, and eventually speak via commands issued by external programs.
- **Interactive**: report clicks and text input; support optional microphone capture and speech/audio playback through independent capabilities.
- **Open, bidirectional API**: transport-independent commands/events; **SignalR is the initial adapter**, with documented contracts for clients in many languages.
- **Ports & Adapters**: isolate SignalR, WPF/Win32, TTS, audio playback and capture behind meaningful boundaries, without abstracting every class.
- **Text-to-speech (TTS)**: allow local Windows speech synthesis as a replaceable default and externally supplied audio; use visemes for lip sync when available.
- **No embedded intelligence**: agent/tool orchestration, speech recognition, and interpretation of user intent are external concerns; technical speech synthesis is allowed.

## Quick start (Windows, .NET 10 SDK)

```bash
dotnet run --project src/DeskMate.Desktop
```

```bash
dotnet run --project samples/DeskMate.Samples.Console
```

The first command shows the character on the desktop; the second drives it through the [control protocol](docs/protocol.md) (`http://127.0.0.1:5117/deskmate`). Tests run on any OS with `dotnet test DeskMate.slnx`.

## Architecture diagrams

All diagrams discussed during the design are collected in **[Architecture diagrams](docs/architecture.md)** (10 Mermaid diagrams covering the Windows overlay, external controllers, SignalR, logical modules, Ports & Adapters, sprite composition, movement, bidirectional audio, TTS, and the superseded cross-platform concept).

## Character asset production

The [AI-assisted sprite production guide](docs/art/ai-sprite-production-guide.md) (French) documents the reference-image workflow, canonical character, the five modular layers, transparent exports, quality checks, asset naming and reusable instructions/prompts for ChatGPT. It is intended for both human contributors and image-generating assistants. Reference photos are not published automatically.

## Architectural decisions

1. [DeskMate as a programmable puppet](docs/decisions/0001-assistant-scope.md)
2. [Windows-native desktop technology](docs/decisions/0002-windows-native-desktop.md)
3. [Layered 2D sprite animation](docs/decisions/0003-layered-sprite-animation.md)
4. [Open control API, SignalR first](docs/decisions/0004-open-control-api.md)
5. [Ports & Adapters at technical boundaries](docs/decisions/0005-ports-and-adapters.md)
6. [Voice and speech capabilities](docs/decisions/0006-voice-and-speech.md)

The decisions distinguish **accepted direction** from features and implementation details still under discussion, including voice pipelines, client permissions, and simultaneous control by multiple applications.

> **Project status:** proof of concept v0.1. A first vertical slice runs end to end (transparent window, movement, gaze, bubbles, text input, Windows TTS, SignalR API); see **[PoC v0.1](docs/poc.md)** for what is implemented, how to run it and its limitations. The rest of the documentation still describes intended behavior.
