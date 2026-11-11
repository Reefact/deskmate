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

## Architectural decisions

1. [DeskMate as a programmable puppet](docs/decisions/0001-assistant-scope.md)
2. [Windows-native desktop technology](docs/decisions/0002-windows-native-desktop.md)
3. [Layered 2D sprite animation](docs/decisions/0003-layered-sprite-animation.md)
4. [Open control API, SignalR first](docs/decisions/0004-open-control-api.md)
5. [Ports & Adapters at technical boundaries](docs/decisions/0005-ports-and-adapters.md)
6. [Voice and speech capabilities](docs/decisions/0006-voice-and-speech.md)

The decisions distinguish **accepted direction** from features and implementation details still under discussion, including voice pipelines, client permissions, and simultaneous control by multiple applications.

> **Project status:** early design; the documentation describes intended behavior, not implemented features.
