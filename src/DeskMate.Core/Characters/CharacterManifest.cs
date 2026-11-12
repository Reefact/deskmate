using System.Text.Json;


namespace DeskMate.Core.Characters;

/// <summary>Description of a character's sprite frames, as stored in <c>character.json</c>.</summary>
public sealed record CharacterManifest
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public required int FrameWidth { get; init; }
    public required int FrameHeight { get; init; }

    /// <summary>Point between the feet, in frame pixels. The character's position refers to this anchor.</summary>
    public required FramePoint FeetAnchor { get; init; }

    /// <summary>Approximate centre of the head, in frame pixels. Used to aim the gaze at a screen point.</summary>
    public required FramePoint HeadAnchor { get; init; }

    public required string NeutralPose { get; init; }
    public required IReadOnlyList<PoseDefinition> Poses { get; init; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static CharacterManifest Parse(string json)
    {
        var manifest = JsonSerializer.Deserialize<CharacterManifest>(json, JsonOptions)
                       ?? throw new InvalidDataException("Empty character manifest.");
        manifest.Validate();
        return manifest;
    }

    public static CharacterManifest Load(string path) => Parse(File.ReadAllText(path));

    public PoseDefinition GetPose(string id) =>
        Poses.FirstOrDefault(p => p.Id == id) ?? throw new KeyNotFoundException($"Unknown pose '{id}'.");

    private void Validate()
    {
        if (FrameWidth <= 0 || FrameHeight <= 0)
            throw new InvalidDataException("Frame size must be positive.");
        if (Poses.Count == 0)
            throw new InvalidDataException("A character needs at least one pose.");
        if (Poses.Select(p => p.Id).Distinct().Count() != Poses.Count)
            throw new InvalidDataException("Pose identifiers must be unique.");
        if (Poses.All(p => p.Id != NeutralPose))
            throw new InvalidDataException($"Neutral pose '{NeutralPose}' is not defined.");
        if (Poses.Any(p => !double.IsFinite(p.Pitch) || p.Pitch is < -1 or > 1))
            throw new InvalidDataException("Pose pitch must be within [-1, 1].");
    }
}

public readonly record struct FramePoint(double X, double Y);

/// <summary>A full-character frame. <see cref="Pitch"/> is the vertical gaze: +1 straight up, -1 straight down.</summary>
public sealed record PoseDefinition(string Id, string File, double Pitch);
