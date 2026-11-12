namespace DeskMate.Transport.SignalR;

// Wire contracts (JSON, camelCase). Versioned by ProtocolInfo.Version; see docs/protocol.md.

public static class ProtocolInfo
{
    public const string Version = "0.1";
    public const string HubPath = "/deskmate";
    public const int DefaultPort = 5117;
}

public static class ClientEvents
{
    public const string OperationCompleted = "operationCompleted";
    public const string Clicked = "clicked";
    public const string MessageSubmitted = "messageSubmitted";
}

public sealed record PointDto(double X, double Y);

public sealed record InfoDto(
    string ProtocolVersion,
    string CharacterId,
    string? CharacterName,
    IReadOnlyList<string> Poses,
    string CurrentPose,
    PointDto Position,
    bool CanSpeak);

public sealed record MoveRequest(double X, double Y, int DurationMs = 800);

public sealed record LookAtRequest(double Pitch);

public sealed record ShowMessageRequest(string Text, int? DurationMs = null);

public sealed record SpeakRequest(string Text, bool ShowMessage = true);

public sealed record OperationAccepted(string OperationId, string Kind);

public sealed record OperationCompletedEvent(string OperationId, string Kind, string Status, string? Error);

public sealed record ClickedEvent(string Button, double X, double Y);

public sealed record MessageSubmittedEvent(string Text);
