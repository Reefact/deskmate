using DeskMate.Core.Characters;

namespace DeskMate.Core.Animation;

/// <summary>Chooses the character frame that best matches a requested gaze.</summary>
public sealed class GazeSelector(CharacterManifest character, double scale)
{
    private readonly IReadOnlyList<PoseDefinition> _byPitch = character.Poses.OrderByDescending(p => p.Pitch).ToArray();

    public PoseDefinition Neutral { get; } = character.GetPose(character.NeutralPose);

    /// <summary>Poses sorted from looking up to looking down.</summary>
    public IReadOnlyList<PoseDefinition> PosesByPitch => _byPitch;

    public PoseDefinition ForPitch(double pitch)
    {
        if (!double.IsFinite(pitch))
            throw new ArgumentOutOfRangeException(nameof(pitch), "Pitch must be a finite number.");
        pitch = Math.Clamp(pitch, -1, 1);
        return _byPitch.MinBy(p => Math.Abs(p.Pitch - pitch))!;
    }

    /// <summary>Vertical gaze pitch needed to look at <paramref name="target"/> from a character standing at <paramref name="feet"/>.</summary>
    public double PitchTowards(ScreenPoint feet, ScreenPoint target)
    {
        var head = HeadPosition(feet);
        var up = head.Y - target.Y; // screen Y grows downwards
        var horizontal = Math.Max(Math.Abs(target.X - head.X), 1);
        var angle = Math.Atan2(up, horizontal); // [-pi/2, pi/2]
        return Math.Clamp(angle / (Math.PI / 2), -1, 1);
    }

    public ScreenPoint HeadPosition(ScreenPoint feet) => new(
        feet.X + (character.HeadAnchor.X - character.FeetAnchor.X) * scale,
        feet.Y + (character.HeadAnchor.Y - character.FeetAnchor.Y) * scale);
}
