using DeskMate.Core;
using Microsoft.AspNetCore.SignalR;

namespace DeskMate.Transport.SignalR;

/// <summary>
/// Translates SignalR invocations into puppet commands. Contains no presentation logic.
/// Long-running commands return <see cref="OperationAccepted"/>; completion arrives as an
/// <c>operationCompleted</c> event.
/// </summary>
public sealed class DeskMateHub(Puppet puppet) : Hub
{
    public InfoDto GetInfo()
    {
        var position = puppet.Position;
        return new InfoDto(
            ProtocolInfo.Version,
            puppet.Character.Id,
            puppet.Character.DisplayName,
            puppet.Character.Poses.Select(p => p.Id).ToArray(),
            puppet.CurrentPose,
            new PointDto(position.X, position.Y),
            puppet.CanSpeak);
    }

    public OperationAccepted Move(MoveRequest request) => Guard(() =>
    {
        var op = puppet.Move(new ScreenPoint(request.X, request.Y), TimeSpan.FromMilliseconds(request.DurationMs));
        return new OperationAccepted(op.Id, op.Kind);
    });

    public void CancelMove() => puppet.CancelMove();

    public string LookAt(LookAtRequest request) => Guard(() => puppet.LookAt(request.Pitch));

    public string LookAtPoint(PointDto point) => Guard(() => puppet.LookAtPoint(new ScreenPoint(point.X, point.Y)));

    public void ResetGaze() => puppet.ResetGaze();

    public void ShowMessage(ShowMessageRequest request) => Guard(() =>
    {
        puppet.ShowMessage(request.Text, request.DurationMs is { } ms ? TimeSpan.FromMilliseconds(ms) : null);
        return true;
    });

    public void HideMessage() => puppet.HideMessage();

    public OperationAccepted Speak(SpeakRequest request) => Guard(() =>
    {
        var op = puppet.Speak(request.Text, request.ShowMessage);
        return new OperationAccepted(op.Id, op.Kind);
    });

    public void StopSpeaking() => puppet.StopSpeaking();

    /// <summary>Validation errors are reported to the caller; other exceptions stay opaque (SignalR default).</summary>
    private static T Guard<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            throw new HubException(ex.Message);
        }
    }
}
