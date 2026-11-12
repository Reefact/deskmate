namespace DeskMate.Core;

public abstract record PuppetEvent;

public enum OperationStatus
{
    Completed,
    Cancelled,
    Failed,
}

/// <summary>A long-running operation (move, speech…) previously accepted has finished.</summary>
public sealed record OperationCompleted(string OperationId, string Kind, OperationStatus Status, string? Error = null) : PuppetEvent;

/// <summary>The user clicked the character (without dragging it).</summary>
public sealed record CharacterClicked(string Button, ScreenPoint Position) : PuppetEvent;

/// <summary>The user typed a message in the character's input bubble.</summary>
public sealed record MessageSubmitted(string Text) : PuppetEvent;

/// <summary>Handle returned when a long-running command is accepted.</summary>
public sealed record OperationHandle(string Id, string Kind, Task<OperationStatus> Completion);
