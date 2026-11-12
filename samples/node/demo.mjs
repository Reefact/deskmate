// Minimal DeskMate controller in Node.js.
//   npm install && node demo.mjs [http://127.0.0.1:5117/deskmate]
import * as signalR from "@microsoft/signalr";

const url = process.argv[2] ?? "http://127.0.0.1:5117/deskmate";
const connection = new signalR.HubConnectionBuilder()
  .withUrl(url)
  .configureLogging(signalR.LogLevel.Warning)
  .build();

const pending = new Map();
connection.on("operationCompleted", (e) => {
  console.log(`<- operationCompleted ${e.operationId} (${e.kind}): ${e.status}`);
  pending.get(e.operationId)?.(e.status);
  pending.delete(e.operationId);
});
connection.on("clicked", (e) => console.log(`<- clicked ${e.button} at (${e.x}, ${e.y})`));
connection.on("messageSubmitted", async (e) => {
  console.log(`<- messageSubmitted "${e.text}"`);
  await connection.invoke("ShowMessage", { text: `Node heard: ${e.text}`, durationMs: 4000 });
});

/** Starts a long-running command and resolves when its completion event arrives. */
async function run(method, request) {
  const { operationId } = await connection.invoke(method, request);
  return new Promise((resolve) => pending.set(operationId, resolve));
}

await connection.start();
const info = await connection.invoke("GetInfo");
console.log(`Connected: protocol ${info.protocolVersion}, character ${info.characterId}, speech ${info.canSpeak}`);

const home = info.position;
await connection.invoke("LookAt", { pitch: 0.8 });
await run("Move", { x: home.x - 250, y: home.y, durationMs: 1200 });
if (info.canSpeak) {
  await run("Speak", { text: "Hello from Node!" });
} else {
  await connection.invoke("ShowMessage", { text: "Hello from Node!", durationMs: 3000 });
}
await run("Move", { x: home.x, y: home.y, durationMs: 1200 });
await connection.invoke("ResetGaze");
console.log("Listening for events (Ctrl+C to quit)…");
