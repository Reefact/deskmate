# 0001 — DeskMate is a programmable desktop puppet, not an intelligent agent

- **Status:** Accepted (revised)
- **Date:** 2026-11-11

## Context

The original description called DeskMate an extensible desktop assistant, but that wording can imply it makes decisions, reasons, or orchestrates agents by itself. The intended product is narrower and more reusable.

DeskMate is a **desktop character and interaction surface**: essentially a programmable, animated console, or a digital puppet. The intelligence (if any), intent interpretation, workflows, and decisions belong to **external applications and agents**.

## Decision

- DeskMate **does not reason, plan, or autonomously operate AI agents and tools**. It is not a chatbot, an LLM host, or an agent orchestrator.
- Provide an **external, bidirectional API** to control the puppet and receive interactions.
- The puppet executes **presentation commands** (movement, gaze, animation, state, text bubbles, audio playback, etc.) without needing to understand *why*.
- It captures **user input** (e.g. clicks, typed messages, microphone input) and sends corresponding events to external clients without needing to interpret *what the user means*.
- The UI is potentially multimodal: **speech and/or message bubbles for output**, and **microphone and/or text input for input**. The exact voice pipeline and UX remain to be designed.
- Name and personality of the character are independent of **DeskMate**, the project/repository name.
- Keep the product independent of a particular AI provider, AI CLI, or agent framework.

DeskMate may be used by Claude Code, Codex, a personal assistant, a script, or any other compatible program. **No AI or Internet connection is required to run the puppet.**

## Technical autonomy is not intelligence

DeskMate needs a small amount of local behavior **only to present instructions well**: timing frames, blinking, transitioning between animation states, computing a path from A to B, choosing the appropriate walking animation, or synchronizing mouth sprites to visemes. Such animation mechanics are part of its rendering engine, **not assistant-level decision-making**.

## Example interaction

An external client can request the puppet to move to a screen position, look at a point, wave, play audio, show a speech bubble, or adopt a status animation. Conversely, it can subscribe to a click, text submission, drag-and-drop, or microphone event. The exact method names and payloads are not settled.

## Consequences

- Keep the **puppet runtime and external communication contract** separate from the clients controlling it.
- Do **not** put AI-agent orchestration or tool integrations inside the DeskMate core.
- Provide transport-agnostic commands and events; transport choices are addressed in [0004](0004-open-control-api.md).
- Multiple unrelated applications should be able to integrate without linking against .NET-specific assemblies.

## Undecided

- Whether one application exclusively controls the character or several may do so simultaneously, and how conflicts are resolved.
- Which audio functions are native versus supplied by external components (recording, speech recognition, text-to-speech, visemes, streaming).
- The precise set of commands, events, lifecycle, and UI conventions.

The overall **Windows-native** strategy and **five-layer sprite composition** are recorded in [0002](0002-windows-native-desktop.md) and [0003](0003-layered-sprite-animation.md).
