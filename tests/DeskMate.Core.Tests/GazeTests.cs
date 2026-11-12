using DeskMate.Core.Animation;

namespace DeskMate.Core.Tests;

public class GazeTests
{
    private readonly GazeSelector _gaze = new(TestCharacter.Load(), scale: 1);

    [Theory]
    [InlineData(1.0, "gaze-0")]
    [InlineData(5.0, "gaze-0")]
    [InlineData(0.0, "gaze-3")]
    [InlineData(-0.2, "gaze-4")]
    [InlineData(-1.0, "gaze-7")]
    public void Selects_closest_pose_for_pitch(double pitch, string expected) =>
        Assert.Equal(expected, _gaze.ForPitch(pitch).Id);

    [Fact]
    public void Rejects_non_finite_pitch() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => _gaze.ForPitch(double.NaN));

    [Fact]
    public void Looks_up_at_points_above_the_head_and_down_at_points_below()
    {
        var feet = new ScreenPoint(500, 800);
        var head = _gaze.HeadPosition(feet);

        Assert.True(_gaze.PitchTowards(feet, new ScreenPoint(head.X + 50, head.Y - 400)) > 0.8);
        Assert.True(_gaze.PitchTowards(feet, new ScreenPoint(head.X + 50, head.Y + 400)) < -0.8);
        Assert.InRange(_gaze.PitchTowards(feet, new ScreenPoint(head.X + 800, head.Y)), -0.01, 0.01);
    }

    [Fact]
    public void Silence_closes_the_mouth_and_open_vowels_open_it()
    {
        Assert.Equal(0, VisemeMouth.ForViseme(0));
        Assert.Equal(0, VisemeMouth.ForViseme(21)); // p/b/m
        Assert.Equal(1, VisemeMouth.ForViseme(2));  // aa
    }
}
