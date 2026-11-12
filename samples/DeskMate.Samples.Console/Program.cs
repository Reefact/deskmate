// Minimal DeskMate controller. It deliberately does not reference DeskMate assemblies:
// the protocol (docs/protocol.md) is the only contract.
//
//   dotnet run --project samples/DeskMate.Samples.Console [-- http://127.0.0.1:5117/deskmate]

using Microsoft.AspNetCore.SignalR.Client;

var url = args.FirstOrDefault() ?? "http://127.0.0.1:5117/deskmate";
await using var connection = new HubConnectionBuilder().WithUrl(url).WithAutomaticReconnect().Build();

var pending = new Dictionary<string, TaskCompletionSource<string>>();
connection.On<OperationCompleted>("operationCompleted", e =>
{
    Console.WriteLine($"<- operationCompleted {e.OperationId} ({e.Kind}): {e.Status} {e.Error}");
    lock (pending)
        if (pending.Remove(e.OperationId, out var tcs))
            tcs.TrySetResult(e.Status);
});
connection.On<Clicked>("clicked", e => Console.WriteLine($"<- clicked {e.Button} at ({e.X:0}, {e.Y:0})"));
connection.On<MessageSubmitted>("messageSubmitted", async e =>
{
    Console.WriteLine($"<- messageSubmitted \"{e.Text}\"");
    // A real controller (an AI agent, a script…) would decide what to answer. Here we just echo.
    await connection.InvokeAsync("ShowMessage", new { text = $"You said: {e.Text}", durationMs = 4000 });
});

await connection.StartAsync();
var info = await connection.InvokeAsync<Info>("GetInfo");
Console.WriteLine($"Connected to DeskMate {info.ProtocolVersion}, character '{info.CharacterId}', at {info.Position}, speech: {info.CanSpeak}");

// Start an operation and wait for its completion event.
async Task<string> Run(string method, object request)
{
    var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    var accepted = await connection.InvokeAsync<Accepted>(method, request);
    Console.WriteLine($"-> {method}: accepted as {accepted.OperationId}");
    lock (pending)
        pending[accepted.OperationId] = tcs;
    return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(60));
}

var home = info.Position;

await connection.InvokeAsync("ShowMessage", new { text = "Hello! I'm being controlled by the sample console.", durationMs = 3000 });
await Task.Delay(1500);

await connection.InvokeAsync<string>("LookAt", new { pitch = 1.0 });
await Task.Delay(800);
await connection.InvokeAsync<string>("LookAt", new { pitch = -1.0 });
await Task.Delay(800);
await connection.InvokeAsync("ResetGaze");

await Run("Move", new { x = home.X - 300, y = home.Y, durationMs = 1500 });
await connection.InvokeAsync<string>("LookAtPoint", new { x = home.X - 300, y = 0 });

if (info.CanSpeak)
    await Run("Speak", new { text = "Hello, I am DeskMate. Double-click me to send a message." });
else
    await connection.InvokeAsync("ShowMessage", new { text = "No TTS voice here, but I can still show text.", durationMs = 3000 });

await Run("Move", new { x = home.X, y = home.Y, durationMs = 1500 });
await connection.InvokeAsync("ResetGaze");
await connection.InvokeAsync("ShowMessage", new { text = "Double-click me and type something!", durationMs = 5000 });

Console.WriteLine("Listening for clicks and messages. Press Enter to quit.");
Console.ReadLine();

internal sealed record Point(double X, double Y)
{
    public override string ToString() => $"({X:0}, {Y:0})";
}

internal sealed record Info(string ProtocolVersion, string CharacterId, Point Position, bool CanSpeak);

internal sealed record Accepted(string OperationId, string Kind);

internal sealed record OperationCompleted(string OperationId, string Kind, string Status, string? Error);

internal sealed record Clicked(string Button, double X, double Y);

internal sealed record MessageSubmitted(string Text);
