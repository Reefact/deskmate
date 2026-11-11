# DeskMate — Architecture diagrams

> **Status: design documentation (2026-11-11).** These Mermaid diagrams consolidate the architecture and interaction diagrams discussed during the initial design. GitHub renders them directly. Except for the final historical diagram, they describe the **current intended direction**, not implemented software. The exact assembly layout, API methods, and audio wire format are not frozen.

DeskMate is a **programmable Windows-native desktop puppet**, not an intelligent agent: external clients decide **what** should happen; DeskMate knows **how to present it**, and returns user interactions.

## 1. The transparent Windows desktop overlay

```mermaid
flowchart TB
    subgraph Screen["Windows desktop"]
        APP["Ordinary application window"]
        OVERLAY["DeskMate borderless topmost WPF window<br/>Visually transparent background"]
        SPRITE["Visible character pixels<br/>Click, drag, contextual actions"]
        EMPTY["Transparent region<br/>Pointer input passes through"]
        OVERLAY --> SPRITE
        OVERLAY --> EMPTY
        EMPTY -.->|"Hit-test pass-through"| APP
    end
    APP -.->|"Visible behind the overlay"| OVERLAY
```

This is a conceptual sketch, not literal z-order wiring. The **visible character should receive input**, while its **transparent surroundings must not block normal applications**. Pixel-accurate hit-testing, focus, Win32 interoperability, DPI and multi-monitor behavior require prototype validation. See [ADR 0002](decisions/0002-windows-native-desktop.md).

## 2. System context: DeskMate is controlled from outside

```mermaid
flowchart TB
    subgraph Controllers["External clients — intelligence and decisions live here"]
        AGENT["AI assistants and agents<br/>Claude Code, Codex, etc."]
        SCRIPT["Desktop applications<br/>Scripts, automation, tools"]
    end
    API["Open bidirectional control API"]
    PUPPET["DeskMate<br/>Windows puppet and interactive console"]
    USER["User"]

    AGENT <-->|"Commands and events"| API
    SCRIPT <-->|"Commands and events"| API
    API <--> PUPPET
    PUPPET <-->|"Visuals, speech, user inputs"| USER
```

The external program decides why the character moves or what it says. DeskMate can still do **technical presentation work** such as motion interpolation, animation transitions, playback, blinking, and lip sync; this is not AI reasoning. See [ADR 0001](decisions/0001-assistant-scope.md).

## 3. SignalR-first communication and interactive console

```mermaid
flowchart TB
    subgraph Clients["Controllers in any compatible language"]
        NET[".NET"]
        JS["JavaScript / Node.js"]
        OTHER["Python, PowerShell, other clients"]
    end
    HUB["SignalR adapter<br/>Localhost, bidirectional"]
    CMD["Puppet commands<br/>Move, look, animate, show text, speak, play audio"]
    EVT["User and lifecycle events<br/>Click, text, microphone, completion"]
    ENGINE["DeskMate Runtime<br/>Windows"]
    VIEW["Character and text bubbles"]
    INPUT["Mouse, keyboard and microphone"]
    NET <--> HUB
    JS <--> HUB
    OTHER <--> HUB
    HUB --> CMD --> ENGINE
    ENGINE --> EVT --> HUB
    ENGINE --> VIEW
    INPUT --> ENGINE
```

SignalR is the **initial** adapter; the command/event contracts should not require a .NET client library. Optional future JSON-RPC/WebSocket and MCP adapters must not become part of the puppet core. Method names and the concurrency/control policy remain open. See [ADR 0004](decisions/0004-open-control-api.md).

## 4. Architectural boundaries: core, animation, Windows host

```mermaid
flowchart TB
    EXT["External controller"]
    COM["Transport adapter<br/>SignalR initially"]
    CORE["DeskMate.Core<br/>Commands, presentation use cases, state, events"]
    ANIM["DeskMate.Animation<br/>Sprites, composition, timelines, visemes"]
    DESK["DeskMate.Desktop<br/>WPF rendering, Windows integration"]
    WIN["Windows<br/>WPF / Win32 / WASAPI"]
    EXT <--> COM
    COM <--> CORE
    CORE <--> ANIM
    CORE --> DESK
    DESK --> WIN
```

This diagram presents **logical responsibilities and runtime collaboration**, not mandatory assembly dependencies. In source code, the implementation of outbound capabilities depends on core-owned ports, not the reverse. It is not necessary to create a separate project for each box.

## 5. Ports & Adapters — hexagonal architecture

```mermaid
flowchart TB
    CLIENT["External applications and AI agents"]
    ADAPTER_IN["Driving adapter<br/>SignalR hub / controller"]
    CORE["Core / application behavior<br/>Puppet state, commands, events, animation coordination"]
    subgraph PORTS["Driven ports — contracts owned by core"]
        SPEECH["Speech synthesis"]
        PLAY["Audio playback"]
        REC["Audio capture"]
        RENDER["Character rendering"]
        WINDOW["Character window"]
    end
    subgraph ADAPTERS["Driven adapters — infrastructure"]
        TTS["Windows local TTS<br/>Optional Azure / other"]
        NPLAY["WASAPI / NAudio playback"]
        NREC["WASAPI / NAudio microphone"]
        WPF["WPF sprite rendering"]
        NWIN["WPF and targeted Win32"]
    end
    CLIENT <-->|"Commands / events"| ADAPTER_IN
    ADAPTER_IN <--> CORE
    CORE --> SPEECH
    CORE --> PLAY
    CORE --> REC
    CORE --> RENDER
    CORE --> WINDOW
    TTS -.->|"implements"| SPEECH
    NPLAY -.->|"implements"| PLAY
    NREC -.->|"implements"| REC
    WPF -.->|"implements"| RENDER
    NWIN -.->|"implements"| WINDOW
```

**Solid arrows = runtime calls/data flow; dashed arrows = adapter implements port.** In terms of **source dependencies**, adapters depend inward on the ports/contracts defined by the core. No SignalR, WPF, Win32, NAudio, or speech-provider types leak into those contracts. Avoid speculative interfaces; the candidates above only become real ports when use cases justify them. See [ADR 0005](decisions/0005-ports-and-adapters.md).

## 6. Five composable sprite layers

```mermaid
flowchart TB
    STATE["Presentation state machine<br/>idle, listening, speaking, working, success, error"]
    subgraph LAYERS["Independent animation tracks"]
        BODY["Body<br/>Posture, gestures, movement"]
        HEAD["Head<br/>Orientation, tilt"]
        EYES["Eyes<br/>Gaze, blinking, expression"]
        MOUTH["Mouth<br/>Shape, expression, visemes"]
        ACC["Accessories<br/>Contextual objects"]
    end
    JOIN["Sprite composition<br/>Stable anchors, pivots, ordering"]
    DRAW["2D WPF renderer<br/>Transparent puppet window"]

    STATE --> BODY
    STATE --> HEAD
    STATE --> EYES
    STATE --> MOUTH
    STATE --> ACC
    BODY --> JOIN
    HEAD --> JOIN
    EYES --> JOIN
    MOUTH --> JOIN
    ACC --> JOIN
    JOIN --> DRAW
```

Body animation, gaze, blinking and mouth motion can run concurrently. Small functional, expressive, and ambient animations can be combined; there is no need to draw every possible whole-character combination. See [ADR 0003](decisions/0003-layered-sprite-animation.md).

## 7. A command can drive a complete movement

```mermaid
sequenceDiagram
    participant C as External client
    participant S as SignalR adapter
    participant P as Puppet core
    participant A as Animation engine
    participant W as WPF window
    C->>S: Move to (x, y, duration)
    S->>P: Move command
    P->>A: Plan motion and select walk animation
    P-->>S: Command accepted
    S-->>C: Accepted / operation identifier
    loop Until motion completes
        A->>W: Updated pose and window position
    end
    A-->>P: Motion completed
    P-->>S: Operation completed event
    S-->>C: Movement completed
```

The precise API method names, correlation/operation identifiers, cancellation, error handling and completion semantics are **illustrative**. This is how DeskMate can move without having to know its controller's intent.

## 8. Bidirectional voice: speakers and microphone

```mermaid
flowchart TB
    EXT["External assistant / application<br/>STT, decisions and optional TTS"]
    STREAM["SignalR transport<br/>Audio streaming is a candidate"]
    subgraph PUPPET["DeskMate on Windows"]
        OUT["Output<br/>Audio playback + mouth animation"]
        IN["Input<br/>Microphone capture"]
    end
    SPEAKER["Speakers"]
    MIC["Microphone"]
    EXT -->|"Audio, optional visemes"| STREAM
    STREAM --> OUT --> SPEAKER
    MIC --> IN
    IN -->|"Captured audio"| STREAM
    STREAM -->|"Audio frames / input events"| EXT
```

This is **input/output**, not a built-in speech assistant. Speech recognition and interpretation remain outside DeskMate. Microphone access, authorizations, buffering, interruption and latency require explicit design. See [ADR 0006](decisions/0006-voice-and-speech.md).

## 9. Three independent presentation capabilities: text, TTS and external audio

```mermaid
flowchart LR
    CLIENT["External controller"]
    SHOW["ShowText"]
    SPEAK["SpeakText"]
    AUDIO["PlayAudio"]
    BUBBLE["Text bubble"]
    TTS["Replaceable TTS port<br/>Windows local by default"]
    PLAYER["Audio player port"]
    MOUTH["Mouth layer<br/>Timed visemes if available<br/>Otherwise amplitude"]
    SPEAKER["Speakers"]

    CLIENT --> SHOW --> BUBBLE
    CLIENT --> SPEAK --> TTS --> PLAYER
    CLIENT --> AUDIO --> PLAYER
    PLAYER --> SPEAKER
    TTS -.->|"Optional visemes"| MOUTH
    AUDIO -.->|"Optional visemes"| MOUTH
    PLAYER -.->|"Playback timing / amplitude"| MOUTH
```

DeskMate can optionally synthesize **text supplied by a controller**, which is a presentation capability, not intelligence. Externally supplied audio remains a first-class alternative. Synchronize mouth sprites to **actual playback progress**, not to the arrival time of SignalR messages.

## 10. Historical sketch — cross-platform architecture **rejected**

The first exploratory diagram separated core, animation and host and suggested adapters for three operating systems. **This was superseded by the explicit Windows-only decision in ADR 0002.** It is retained only to preserve the design discussion, not as a roadmap.

```mermaid
flowchart TB
    CORE["Assistant / behavior core — original sketch"]
    ANIM["Animation engine"]
    HOST["Avalonia desktop host — rejected"]
    WIN["Windows"]
    LINUX["Linux"]
    MAC["macOS"]
    CORE --> ANIM --> HOST
    HOST --> WIN
    HOST --> LINUX
    HOST --> MAC
```

**Current target:** WPF + targeted Win32 interoperability on Windows only. Additionally, the original "assistant core" was corrected to **puppet core**; agent orchestration is external.

## Open questions

- Single controlling application versus multiple simultaneous controllers; arbitration, permissions and priorities.
- Stable command and event schemas, operation completion/cancellation and transport authentication.
- Audio streaming formats, buffering, privacy, microphone access, voice providers and viseme timing.
- Practical per-pixel click-through and interaction behavior in WPF/Win32.
- Exact source project structure: follow use cases, not the number of diagram boxes.

## Related decisions

1. [0001 — Programmable puppet](decisions/0001-assistant-scope.md)
2. [0002 — Windows-native desktop](decisions/0002-windows-native-desktop.md)
3. [0003 — Layered sprite animation](decisions/0003-layered-sprite-animation.md)
4. [0004 — Open control API, SignalR first](decisions/0004-open-control-api.md)
5. [0005 — Ports & Adapters](decisions/0005-ports-and-adapters.md)
6. [0006 — Voice, TTS and microphone](decisions/0006-voice-and-speech.md)
