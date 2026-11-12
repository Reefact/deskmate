namespace DeskMate.Core.Ports;

/// <summary>
/// Driven port: where the character is shown (desktop window + sprite renderer).
/// Implementations must be callable from any thread.
/// </summary>
public interface ICharacterStage
{
    /// <summary>Current position of the character's feet anchor (it may have been dragged by the user).</summary>
    ScreenPoint FeetPosition { get; }

    void SetFeetPosition(ScreenPoint position);

    void ShowPose(string poseId);

    void ShowMessage(string text);

    void HideMessage();

    /// <summary>Mouth openness in [0, 1] while speaking (0 = closed).</summary>
    void SetMouthOpenness(double openness);
}
