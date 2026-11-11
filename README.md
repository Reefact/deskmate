# DeskMate

A programmable Windows desktop puppet: an animated character and interactive console for external assistants, agents, tools, and scripts.

DeskMate is **not an intelligent assistant or agent in itself**. It is a small, expressive **presentation and interaction surface**. External applications decide what to do; DeskMate animates the character, displays messages, plays audio when supplied, and passes user interactions back to its controllers. No AI runtime is required to use it.

## Direction

- **Windows-native**: C# / .NET 10, WPF, and targeted Win32 interop; prioritize the Windows experience over cross-platform compatibility.
- **Lightweight 2D character**: composable sprites (body, head, eyes, mouth, and accessories), independent animation tracks, and optional viseme-driven lip sync.
- **Programmable**: move, animate, look around, show text bubbles, and eventually speak via commands issued by external programs.
- **Interactive**: report clicks and text input; optionally support microphone input and audio output.
- **Open, bidirectional API**: transport-independent commands/events; **SignalR is the initial adapter**, with documented contracts for clients in many languages.
- **No embedded intelligence**: agent/tool orchestration, interpretation of user intent, and speech processing are external concerns.

## Architectural decisions

1. [DeskMate as a programmable puppet](docs/decisions/0001-assistant-scope.md)
2. [Windows-native desktop technology](docs/decisions/0002-windows-native-desktop.md)
3. [Layered 2D sprite animation](docs/decisions/0003-layered-sprite-animation.md)
4. [Open control API, SignalR first](docs/decisions/0004-open-control-api.md)

The decisions distinguish **accepted direction** from features and implementation details still under discussion, including voice pipelines, client permissions, and simultaneous control by multiple applications.

> **Project status:** early design; the documentation describes intended behavior, not implemented features.
