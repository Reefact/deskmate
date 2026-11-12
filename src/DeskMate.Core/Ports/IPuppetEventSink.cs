namespace DeskMate.Core.Ports;

/// <summary>Driven port: forwards puppet events to whoever controls the puppet (e.g. SignalR clients).</summary>
public interface IPuppetEventSink
{
    void Publish(PuppetEvent puppetEvent);
}
