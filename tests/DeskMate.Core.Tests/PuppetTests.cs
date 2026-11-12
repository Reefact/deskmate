namespace DeskMate.Core.Tests;

public class PuppetTests
{
    [Fact]
    public async Task Move_interpolates_to_the_target_then_reports_completion()
    {
        await using var f = new PuppetFixture(start: new ScreenPoint(0, 0));

        var op = f.Puppet.Move(new ScreenPoint(300, 100), TimeSpan.FromMilliseconds(500));
        var status = await f.AdvanceUntil(op.Completion);

        Assert.Equal(OperationStatus.Completed, status);
        Assert.Equal(new ScreenPoint(300, 100), f.Stage.FeetPosition);
        Assert.True(f.Stage.Positions.Count > 5, "Movement should be interpolated over several frames.");
        var completed = Assert.Single(f.Events.Events.OfType<OperationCompleted>());
        Assert.Equal(op.Id, completed.OperationId);
        Assert.Equal(Puppet.MoveKind, completed.Kind);
    }

    [Fact]
    public async Task Move_with_zero_duration_teleports()
    {
        await using var f = new PuppetFixture();

        var status = await f.Puppet.Move(new ScreenPoint(42, 24), TimeSpan.Zero).Completion;

        Assert.Equal(OperationStatus.Completed, status);
        Assert.Equal(new ScreenPoint(42, 24), f.Stage.FeetPosition);
    }

    [Fact]
    public async Task A_new_move_cancels_the_current_one()
    {
        await using var f = new PuppetFixture();

        var first = f.Puppet.Move(new ScreenPoint(1000, 0), TimeSpan.FromSeconds(10));
        f.Time.Advance(TimeSpan.FromMilliseconds(100));
        var second = f.Puppet.Move(new ScreenPoint(0, 500), TimeSpan.FromMilliseconds(200));

        Assert.Equal(OperationStatus.Cancelled, await f.AdvanceUntil(first.Completion));
        Assert.Equal(OperationStatus.Completed, await f.AdvanceUntil(second.Completion));
        Assert.Equal(new ScreenPoint(0, 500), f.Stage.FeetPosition);
    }

    [Theory]
    [InlineData(double.NaN, 0, 100)]
    [InlineData(0, 0, -1)]
    [InlineData(0, 0, 120_000)]
    public async Task Move_rejects_invalid_input(double x, double y, double durationMs)
    {
        await using var f = new PuppetFixture();

        Assert.ThrowsAny<ArgumentException>(() => f.Puppet.Move(new ScreenPoint(x, y), TimeSpan.FromMilliseconds(durationMs)));
    }

    [Fact]
    public async Task LookAt_shows_the_matching_pose()
    {
        await using var f = new PuppetFixture();

        Assert.Equal("gaze-0", f.Puppet.LookAt(1));
        Assert.Equal("gaze-0", f.Stage.LastPose);
        Assert.Equal("gaze-0", f.Puppet.CurrentPose);

        f.Puppet.ResetGaze();
        Assert.Equal("gaze-4", f.Stage.LastPose);
    }

    [Fact]
    public async Task Message_with_duration_is_hidden_automatically()
    {
        await using var f = new PuppetFixture();

        f.Puppet.ShowMessage("Hello", TimeSpan.FromSeconds(2));
        Assert.Equal("Hello", f.Stage.Message);

        f.Time.Advance(TimeSpan.FromSeconds(2));
        await f.Eventually(() => f.Stage.Message is null);
    }

    [Fact]
    public async Task A_new_message_cancels_the_pending_hide_of_the_previous_one()
    {
        await using var f = new PuppetFixture();

        f.Puppet.ShowMessage("First", TimeSpan.FromSeconds(1));
        f.Puppet.ShowMessage("Second");
        f.Time.Advance(TimeSpan.FromSeconds(5));
        await Task.Delay(50);

        Assert.Equal("Second", f.Stage.Message);
    }

    [Fact]
    public async Task Speak_shows_text_animates_mouth_from_visemes_and_completes()
    {
        await using var f = new PuppetFixture();

        var op = f.Puppet.Speak("Bonjour !");
        await f.Eventually(() => f.Speech.OnViseme is not null);

        Assert.True(f.Puppet.IsSpeaking);
        Assert.Equal("Bonjour !", f.Stage.Message);
        f.Speech.OnViseme!(new(2, TimeSpan.FromMilliseconds(80)));
        Assert.Equal(1.0, f.Stage.MouthOpenness.Last());

        f.Speech.Finish();
        Assert.Equal(OperationStatus.Completed, await op.Completion.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(0.0, f.Stage.MouthOpenness.Last());
        Assert.False(f.Puppet.IsSpeaking);

        // The bubble lingers briefly after speech.
        Assert.Equal("Bonjour !", f.Stage.Message);
        f.Time.Advance(TimeSpan.FromSeconds(2));
        await f.Eventually(() => f.Stage.Message is null);
    }

    [Fact]
    public async Task StopSpeaking_cancels_the_speech_operation()
    {
        await using var f = new PuppetFixture();

        var op = f.Puppet.Speak("A long sentence");
        await f.Eventually(() => f.Speech.OnViseme is not null);
        f.Puppet.StopSpeaking();

        Assert.Equal(OperationStatus.Cancelled, await op.Completion.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Null(f.Stage.Message);
        var completed = f.Events.Events.OfType<OperationCompleted>().Single();
        Assert.Equal(OperationStatus.Cancelled, completed.Status);
    }

    [Fact]
    public async Task Speak_without_synthesizer_is_not_supported()
    {
        var stage = new FakeStage();
        await using var puppet = new Puppet(TestCharacter.Load(), stage, new RecordingEvents());

        Assert.False(puppet.CanSpeak);
        Assert.Throws<NotSupportedException>(() => puppet.Speak("Hi"));
    }

    [Fact]
    public async Task User_interactions_are_published_as_events()
    {
        await using var f = new PuppetFixture();

        f.Puppet.ReportClick("left", new ScreenPoint(10, 20));
        f.Puppet.SubmitUserMessage("  what time is it?  ");
        f.Puppet.SubmitUserMessage("   ");

        Assert.Collection(f.Events.Events,
            e => Assert.Equal(new CharacterClicked("left", new ScreenPoint(10, 20)), e),
            e => Assert.Equal(new MessageSubmitted("what time is it?"), e));
    }

    [Fact]
    public async Task Idle_behaviour_glances_but_respects_an_explicit_gaze()
    {
        await using var f = new PuppetFixture(new PuppetOptions
        {
            Scale = 1,
            RandomSeed = 1,
            IdleMinInterval = TimeSpan.FromSeconds(1),
            IdleMaxInterval = TimeSpan.FromSeconds(1),
            GazeHold = TimeSpan.FromSeconds(30),
        });
        f.Puppet.Start();
        Assert.Equal("gaze-4", f.Stage.LastPose);

        f.Time.Advance(TimeSpan.FromSeconds(1));
        await f.Eventually(() => f.Stage.LastPose is "gaze-3" or "gaze-5");

        f.Puppet.LookAt(1);
        for (var i = 0; i < 10; i++)
        {
            f.Time.Advance(TimeSpan.FromSeconds(1));
            await Task.Delay(5);
        }
        Assert.Equal("gaze-0", f.Stage.LastPose);
    }
}
