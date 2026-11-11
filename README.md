# DeskMate

An extensible desktop assistant that connects to AI agents and tools through an interactive, animated character.

DeskMate is a **Windows-native desktop assistant**. Its character is the user interface, not the product's purpose: the assistant should be able to converse, react to events, and invoke connected agents and tools. The project favors an excellent Windows experience over cross-platform compromises.

## Direction

- **Windows first, Windows only**: C# / .NET 10, WPF, and targeted Win32 interop.
- **Lightweight 2D presentation**: composable sprite animations; no game engine or browser shell as a default.
- **Modular character**: independently animated body, head, eyes, mouth, and accessories.
- **Extensible assistant**: integrate existing AI agents and local tools rather than reimplementing them.

## Decisions

The initial product and architecture decisions are recorded here:

1. [Assistant scope and positioning](docs/decisions/0001-assistant-scope.md)
2. [Windows-native desktop technology](docs/decisions/0002-windows-native-desktop.md)
3. [Composable 2D character and animation](docs/decisions/0003-layered-sprite-animation.md)

These documents distinguish **accepted decisions** from **future possibilities**, which remain subject to validation.

> Status: early design; no implementation is implied by this README.
