# 0004 — Expose an open bidirectional control API; start with SignalR

- **Status:** Accepted direction; protocol details to validate
- **Date:** 2026-11-11

## Context

DeskMate is a programmable desktop puppet. External programs—not DeskMate—interpret intentions, make decisions, invoke tools, and decide what the character should do. DeskMate therefore needs a **simple, bidirectional and language-independent control surface**.

Clients should include C#/.NET, JavaScript/Node.js, Python, PowerShell, AI agents, and ordinary local utilities. The protocol should be documented so that use of a proprietary SDK or linking against DeskMate assemblies is never required.

## Decision

1. Define **transport-independent commands, events, errors and versioned JSON payload contracts** first.
2. Use **ASP.NET Core SignalR as the initial real-time communication adapter**, hosted locally by the Windows DeskMate application.
3. Use SignalR for **client-to-puppet commands** and **puppet-to-client events** on a persistent bidirectional connection.
4. Treat SignalR as an **adapter**, not part of the puppet engine or its domain model. A .NET client SDK may be provided for convenience, but must remain optional.
5. Publish clear protocol documentation and examples for clients in different programming languages.
6. Keep other adapters possible without requiring them in the first implementation.

SignalR is open source and its wire protocol is documented, but **SignalR is not plain WebSocket or JSON-RPC 2.0**: non-.NET clients need to implement or use a compatible SignalR client. This interoperability tradeoff is accepted for the initial implementation.

### Illustrative API surface (not yet a specification)

**Commands** initiated by an external controller:
- `mate.move` — move to screen coordinates, potentially with duration/path.
- `mate.lookAt` — change head/eye gaze.
- `mate.playAnimation` / `mate.setState` — control movement and presentation.
- `mate.showMessage` — display a text bubble.
- `mate.playAudio` / `mate.say` — play supplied speech/audio, optionally with timing information for lip sync.

**Events** emitted by DeskMate:
- `input.clicked`, `input.messageSubmitted`, `input.fileDropped`.
- Microphone/audio input events if microphone support is enabled.
- Optional lifecycle/status notifications such as command completion or error.

The dotted names above describe concepts only, **not SignalR hub method names** or a frozen wire contract. In particular, distinguish **command accepted** from **operation finished** for long-running movements and animations.

### Voice and input/output

- **Output**: voice and/or speech bubbles; mouth animation can be driven by amplitude or timed visemes.
- **Input**: microphone and/or a text-input bubble.
- DeskMate transports input and presentation requests; **speech-to-text, text-to-speech, language understanding and AI processing are not intrinsic obligations of the puppet runtime**.
- Large or streamed audio may need a separate or specialized channel. The precise transport and ownership of audio processing are still undecided.

### Security and local deployment

- Start with **loopback-only binding** (`127.0.0.1`), not a network-exposed server.
- Design **client authentication / authorization**, validate browser origins where applicable, and avoid assuming that localhost alone grants adequate security.
- Define limits for payload size and command rates; validate coordinates, paths and other external inputs.
- Never assume the application controlling DeskMate should have unrestricted OS privileges.

## Alternatives and possible adapters

- **JSON-RPC 2.0 over WebSocket** offers particularly simple generic clients and an independently standardized RPC contract. It remains a plausible future alternative/adapter, rather than the first transport.
- **MCP** could be exposed as an optional integration layer for compatible AI agents, but **must not define DeskMate's internal API or make it AI-dependent**.
- A typed **.NET client library** can be a convenience, never a required client technology.

There is no commitment to ship multiple protocols immediately.

## Open decisions

- **One controller or multiple simultaneous controllers?** Ownership, permissions, arbitration, cancellation and priorities must be specified before relying on multi-client control.
- How clients subscribe to events, including privacy-sensitive input streams.
- How commands complete, fail, time out and are cancelled; connection/reconnection semantics.
- Stable JSON schemas, API/version negotiation and compatibility policy.
- Exact SignalR hosting, paths, authentication mechanism, and audio representation.
- Whether MCP or a plain WebSocket/JSON-RPC adapter is warranted by real integrations.

## Consequences

A simple non-AI program can control the same character as an AI agent. DeskMate stays a **Windows-native, light 2D puppet**, with no direct dependency on any given assistant or agent runtime. SignalR is the first communication choice, while the command model remains decoupled and intentionally open.
