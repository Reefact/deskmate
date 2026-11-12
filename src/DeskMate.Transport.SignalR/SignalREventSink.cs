using DeskMate.Core;
using DeskMate.Core.Ports;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DeskMate.Transport.SignalR;

/// <summary>Broadcasts puppet events to all connected controllers.</summary>
public sealed class SignalREventSink(IHubContext<DeskMateHub> hub, ILogger<SignalREventSink> logger) : IPuppetEventSink
{
    public void Publish(PuppetEvent puppetEvent)
    {
        var (method, payload) = puppetEvent switch
        {
            OperationCompleted e => (ClientEvents.OperationCompleted,
                (object)new OperationCompletedEvent(e.OperationId, e.Kind, ToWire(e.Status), e.Error)),
            CharacterClicked e => (ClientEvents.Clicked, new ClickedEvent(e.Button, e.Position.X, e.Position.Y)),
            MessageSubmitted e => (ClientEvents.MessageSubmitted, new MessageSubmittedEvent(e.Text)),
            _ => (null, null),
        };
        if (method is null)
            return;

        _ = SendAsync(method, payload!);
    }

    private async Task SendAsync(string method, object payload)
    {
        try
        {
            await hub.Clients.All.SendAsync(method, payload).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send {Method} to clients", method);
        }
    }

    private static string ToWire(OperationStatus status) => status switch
    {
        OperationStatus.Completed => "completed",
        OperationStatus.Cancelled => "cancelled",
        _ => "failed",
    };
}
