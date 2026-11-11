# 0002 — Optimize for a native Windows desktop experience

- **Status:** Accepted
- **Date:** 2026-11-11

## Context

DeskMate's character should appear over ordinary applications without an opaque rectangular window and without interfering with normal desktop use. A lower-quality cross-platform compromise is not desirable.

## Decision

- Target **Windows exclusively** for the foreseeable future. Linux and macOS support are not project objectives.
- Use **C# / .NET 10 and WPF** for the desktop host.
- Use **targeted Win32 interop** wherever WPF alone does not provide the required window behavior.
- Do not choose Avalonia, Electron, a browser-based shell, or a game engine as the core desktop technology.
- Prefer simple WPF 2D rendering initially. Consider Direct2D or SkiaSharp only if concrete profiling or rendering requirements justify them.

### Required user experience

- A **borderless, visually transparent** character window.
- **Topmost** presentation when enabled.
- **Precise hit-testing**: the visible character is interactive, while transparent regions should allow interaction with underlying applications.
- The character can be **repositioned by dragging**.
- Its animations are lightweight, smooth, and do not unnecessarily consume CPU/GPU or interfere with other apps.

A transparent window is **not automatically click-through**. Accurate hit-testing / input passthrough is a platform-specific engineering concern, to be prototyped and validated (including WPF/Win32 interaction).

## Proposed logical boundaries

- **DeskMate.Core** — transport-independent puppet commands, interaction events, runtime state and coordination of presentation (not AI logic).
- **DeskMate.Animation** — sprites, compositing, state transitions, visemes.
- **DeskMate.Desktop** — WPF, rendering host, native window and input integration.
- **DeskMate.Transport.SignalR** (working name) — external control API adapter, hosted within the Windows desktop process; see [0004](0004-open-control-api.md).

These are logical responsibilities; they do not mandate exactly three assemblies or an elaborate abstraction hierarchy.

## To validate or explore

- Per-pixel hit-testing and passthrough in real Windows configurations.
- Multi-monitor behavior, DPI scaling, window focus, and always-on-top edge cases.
- Tray icon, context menus, keyboard shortcuts, autostart, notifications, and drag-and-drop.
- Whether optional native rendering acceleration is ever necessary.

These are **implementation questions or potential features**, not claims that they already work.
