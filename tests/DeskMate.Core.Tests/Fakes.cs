using System.Collections.Concurrent;
using DeskMate.Core;
using DeskMate.Core.Characters;
using DeskMate.Core.Ports;
using Microsoft.Extensions.Time.Testing;

namespace DeskMate.Core.Tests;

internal sealed class FakeStage : ICharacterStage
{
    private readonly Lock _gate = new();
    private ScreenPoint _position;

    public ConcurrentQueue<ScreenPoint> Positions { get; } = new();
    public ConcurrentQueue<string> Poses { get; } = new();
    public ConcurrentQueue<double> MouthOpenness { get; } = new();
    public string? Message { get; private set; }

    public FakeStage(ScreenPoint start = default) => _position = start;

    public ScreenPoint FeetPosition
    {
        get { lock (_gate) return _position; }
    }

    public void SetFeetPosition(ScreenPoint position)
    {
        lock (_gate) _position = position;
        Positions.Enqueue(position);
    }

    public void ShowPose(string poseId) => Poses.Enqueue(poseId);
    public void ShowMessage(string text) => Message = text;
    public void HideMessage() => Message = null;
    public void SetMouthOpenness(double openness) => MouthOpenness.Enqueue(openness);
    public string? LastPose => Poses.LastOrDefault();
}

internal sealed class RecordingEvents : IPuppetEventSink
{
    public ConcurrentQueue<PuppetEvent> Events { get; } = new();
    public void Publish(PuppetEvent puppetEvent) => Events.Enqueue(puppetEvent);
}

internal sealed class FakeSpeech : ISpeechSynthesizer
{
    private TaskCompletionSource? _current;
    public Action<SpeechViseme>? OnViseme { get; private set; }
    public List<string> Spoken { get; } = [];

    public Task SpeakAsync(string text, Action<SpeechViseme> onViseme, CancellationToken cancellationToken)
    {
        Spoken.Add(text);
        OnViseme = onViseme;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        _current = tcs;
        return tcs.Task;
    }

    public void Finish() => _current!.TrySetResult();
}

internal static class TestCharacter
{
    public static CharacterManifest Load() =>
        CharacterManifest.Load(Path.Combine(AppContext.BaseDirectory, "Assets", "character.json"));
}

internal sealed class PuppetFixture : IAsyncDisposable
{
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 12, 9, 0, 0, TimeSpan.Zero));
    public FakeStage Stage { get; }
    public RecordingEvents Events { get; } = new();
    public FakeSpeech Speech { get; } = new();
    public Puppet Puppet { get; }

    public PuppetFixture(PuppetOptions? options = null, ScreenPoint start = default)
    {
        Stage = new FakeStage(start);
        Puppet = new Puppet(TestCharacter.Load(), Stage, Events, Speech, Time,
            options ?? new PuppetOptions { IdleEnabled = false, Scale = 1 });
    }

    /// <summary>Advances fake time until <paramref name="task"/> completes.</summary>
    public async Task<T> AdvanceUntil<T>(Task<T> task, TimeSpan? step = null)
    {
        for (var i = 0; i < 10_000 && !task.IsCompleted; i++)
        {
            Time.Advance(step ?? TimeSpan.FromMilliseconds(16));
            await Task.Delay(1);
        }
        return await task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    public async Task Eventually(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
            await Task.Delay(5);
        Assert.True(condition());
    }

    public ValueTask DisposeAsync() => Puppet.DisposeAsync();
}
