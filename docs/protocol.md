# DeskMate control protocol — v0.1 (PoC)

> **Status:** proof of concept. Names and payloads may change before 1.0; `GetInfo().protocolVersion` tells clients which version they talk to.

DeskMate hosts an [ASP.NET Core SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction) hub. Any SignalR client works (.NET, JavaScript/Node.js, Java, Python via third-party clients…); no DeskMate assembly is needed.

| | |
| --- | --- |
| Hub URL | `http://127.0.0.1:5117/deskmate` (port: `--port` or `DESKMATE_PORT`) |
| Binding | loopback only (`127.0.0.1`) |
| Protocol | SignalR JSON hub protocol, **camelCase** payloads |
| Authentication | none yet — see [Security](#security) |
| Coordinates | desktop coordinates in WPF device-independent units (1 unit = 1 px at 100 % scaling) |

`GET http://127.0.0.1:5117/` returns a small JSON description (useful to check that DeskMate is running).

## Commands (client → DeskMate)

Each command takes **one object argument** (fields not listed as required are optional).

| Method | Argument | Returns | Notes |
| --- | --- | --- | --- |
| `GetInfo` | — | `Info` | Character, poses, current pose and position, whether speech is available. |
| `Move` | `{ x, y, durationMs = 800 }` | `OperationAccepted` | Moves the **feet** of the character to `(x, y)` with ease-in/out. `durationMs` ∈ [0, 60000]; 0 = teleport. A new move cancels the current one. |
| `CancelMove` | — | — | The current move completes as `cancelled`. |
| `LookAt` | `{ pitch }` | pose id | Vertical gaze: `1` = up, `0` = straight, `-1` = down. Overrides idle glances for a few seconds. |
| `LookAtPoint` | `{ x, y }` | pose id | Looks towards a screen point (vertical component only with the current sprites). |
| `ResetGaze` | — | — | Back to the neutral pose; idle behaviour resumes. |
| `ShowMessage` | `{ text, durationMs? }` | — | Text bubble (≤ 2000 chars). Without `durationMs` it stays until replaced or hidden. |
| `HideMessage` | — | — | |
| `Speak` | `{ text, showMessage = true }` | `OperationAccepted` | Local Windows TTS + mouth animation synced to playback visemes. Fails if no voice is available (`GetInfo().canSpeak`). A new `Speak` interrupts the current one. |
| `StopSpeaking` | — | — | The current speech completes as `cancelled`. |

Invalid arguments (empty text, non-finite numbers, out-of-range durations, speech unavailable) are rejected with a hub error whose message explains the problem.

### Long-running operations

`Move` and `Speak` return immediately — this only means **accepted**:

```json
{ "operationId": "move-3", "kind": "move" }
```

When the operation ends, every client receives an `operationCompleted` event with the same `operationId`.

## Events (DeskMate → clients)

Events are broadcast to all connected clients.

| Event | Payload | When |
| --- | --- | --- |
| `operationCompleted` | `{ operationId, kind, status, error? }` | `status`: `completed`, `cancelled` or `failed`. |
| `clicked` | `{ button, x, y }` | The user clicked the character without dragging it (`left` / `right`). |
| `messageSubmitted` | `{ text }` | The user typed a message in the character's input bubble (double-click or *Write a message…*). |

## Payloads

```jsonc
// Info
{
  "protocolVersion": "0.1",
  "characterId": "default",
  "characterName": "DeskMate (PoC)",
  "poses": ["gaze-0", "…", "gaze-7"],
  "currentPose": "gaze-4",
  "position": { "x": 1740, "y": 1032 },
  "canSpeak": true
}
```

## Example (JavaScript)

```js
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder().withUrl("http://127.0.0.1:5117/deskmate").build();
connection.on("messageSubmitted", (e) => console.log("user said", e.text));
connection.on("operationCompleted", (e) => console.log(e.operationId, e.status));

await connection.start();
await connection.invoke("LookAt", { pitch: 0.8 });
const { operationId } = await connection.invoke("Move", { x: 800, y: 900, durationMs: 1200 });
await connection.invoke("Speak", { text: "Hello!" });
```

Complete samples: [`samples/node`](../samples/node) and [`samples/DeskMate.Samples.Console`](../samples/DeskMate.Samples.Console).

## Security

The PoC listens on `127.0.0.1` only and limits incoming messages to 64 KiB, but **any local process can control the puppet**. CORS is not enabled, but a web page could still open a WebSocket to the hub (WebSockets are not subject to CORS), and origins are not validated yet. Client authentication, origin validation, rate limits and multi-controller arbitration are open questions of [ADR 0004](decisions/0004-open-control-api.md).
