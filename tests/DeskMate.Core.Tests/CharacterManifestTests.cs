using DeskMate.Core.Characters;

namespace DeskMate.Core.Tests;

public class CharacterManifestTests
{
    [Fact]
    public void Default_character_manifest_is_valid()
    {
        var character = TestCharacter.Load();

        Assert.Equal("default", character.Id);
        Assert.Equal(8, character.Poses.Count);
        Assert.Equal("gaze-4", character.NeutralPose);
    }

    [Fact]
    public void Rejects_unknown_neutral_pose()
    {
        const string json = """
        { "id": "x", "frameWidth": 10, "frameHeight": 10,
          "feetAnchor": { "x": 5, "y": 10 }, "headAnchor": { "x": 5, "y": 2 },
          "neutralPose": "missing",
          "poses": [ { "id": "a", "file": "a.png", "pitch": 0 } ] }
        """;

        Assert.Throws<InvalidDataException>(() => CharacterManifest.Parse(json));
    }
}
