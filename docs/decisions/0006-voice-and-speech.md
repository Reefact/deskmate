# 0006 — Support voice as replaceable input and output capabilities

- **Status:** Accepted direction; provider and streaming details to validate
- **Date:** 2026-11-11

## Context

DeskMate should be able to speak and listen while remaining a **programmable puppet, not an intelligent assistant**. Text bubbles are straightforward, but audio must support playback, capture, potential streaming, and synchronization of the character's mouth. External controllers should be able to use their own speech/audio providers.

## Decision

Treat text, speech synthesis, audio playback and microphone capture as **separate capabilities**, behind ports defined by [0005](0005-ports-and-adapters.md).

### Output

- **ShowText**: display text (for example, a speech bubble); no sound required.
- **SpeakText**: optionally synthesize supplied text with a configured TTS adapter, play the resulting audio and animate the mouth.
- **PlayAudio**: play audio supplied by the external controller, potentially with optional timed visemes.
- These capabilities can be combined, such as a text bubble accompanying spoken words, but neither text nor audio is intrinsically required by the other.
- DeskMate does **not decide what to say**. TTS is a technical presentation service, not intelligence.
- Playback needs stop/cancel and usable completion/error feedback to external clients.

### Input

- Allow the user to submit text through the UI and emit an input event.
- A microphone adapter can **capture audio and forward it** to an authorized external controller, with a visible capture state and an explicit stop action.
- **Speech recognition, language understanding and decisions** are not DeskMate core responsibilities. The external controller chooses how to interpret microphone input.
- Microphone access and event subscriptions must respect user control and privacy.

### Lip synchronization

- Use the existing independent **mouth sprite layer**.
- Prefer **time-coded visemes** when the synthesizer or controller provides them.
- Otherwise, a simple **audio-amplitude-driven** mouth animation is an acceptable fallback.
- Synchronize against **actual audio playback position**, not network message arrival time, to avoid drift.

### Initial technology direction, not a hard dependency

- Prefer a **local Windows TTS implementation by default**, so basic speech can work without a cloud account or service.
- Optionally support higher-quality providers (e.g. Azure Speech) or fully external audio generation through replaceable adapters.
- Windows audio capture/playback via **WASAPI**, potentially using **NAudio**, is a practical starting candidate.
- Consider PCM samples as a simple baseline for local streaming, with format, channels and sample rate explicitly negotiated or declared.
- SignalR **streaming** is a candidate for incoming/outgoing audio frames (see [0004](0004-open-control-api.md)), but latency, buffering, binary framing, backpressure, cancellation and compatibility with non-.NET clients require validation.
- The exact TTS API, audio format, transport details, supported voices, and viseme mapping are **not yet fixed**.

## Consequences

- DeskMate can speak text locally without any AI model, while clients remain free to provide their own synthesized audio.
- A speech provider that lacks visemes must still work.
- Text UI, audio playback, audio capture, and lip sync can be tested independently through ports and adapters.
- Real-time duplex conversation with barge-in, acoustic echo cancellation and low-latency coordination is **a later capability**, not a prerequisite for first voice support.

## Open questions

- API shape and lifecycle for streamed audio and timed visemes.
- Whether the application or the external controller owns audio sessions, voices, microphone permissions and capture activation.
- How concurrent controllers arbitrate speech/playback and microphone access.
- Audio device changes, buffering, interruption and silence/error handling.
