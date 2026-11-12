using DeskMate.Core.Animation;
using DeskMate.Core.Characters;
using DeskMate.Core.Ports;

namespace DeskMate.Core;

/// <summary>
/// The puppet's application behaviour. It knows <b>how</b> to present things (motion, gaze, bubbles, speech, lip sync);
/// external controllers decide <b>what</b> to present. All public members are thread-safe.
/// </summary>
public sealed class Puppet : IAsyncDisposable
{
    public const string MoveKind = "move";
    public const string SpeakKind = "speak";

    private readonly ICharacterStage _stage;
    private readonly IPuppetEventSink _events;
    private readonly ISpeechSynthesizer? _speech;
    private readonly TimeProvider _time;
    private readonly PuppetOptions _options;
    private readonly GazeSelector _gaze;
    private readonly Random _random;

    private readonly CancellationTokenSource _lifetime = new();
    private readonly OperationSlot _moveSlot = new();
    private readonly OperationSlot _speechSlot = new();
    private readonly OperationSlot _messageSlot = new();

    private readonly Lock _state = new();
    private string _currentPose;
    private DateTimeOffset _gazeHeldUntil = DateTimeOffset.MinValue;
    private string? _speakingOperation;
    private long _operationSequence;
    private Task _idleTask = Task.CompletedTask;

    public Puppet(
        CharacterManifest character,
        ICharacterStage stage,
        IPuppetEventSink events,
        ISpeechSynthesizer? speech = null,
        TimeProvider? time = null,
        PuppetOptions? options = null)
    {
        Character = character;
        _stage = stage;
        _events = events;
        _speech = speech;
        _time = time ?? TimeProvider.System;
        _options = options ?? new PuppetOptions();
        _gaze = new GazeSelector(character, _options.Scale);
        _random = _options.RandomSeed is { } seed ? new Random(seed) : new Random();
        _currentPose = character.NeutralPose;
    }

    public CharacterManifest Character { get; }

    public PuppetOptions Options => _options;

    public bool CanSpeak => _speech is not null;

    public ScreenPoint Position => _stage.FeetPosition;

    public string CurrentPose
    {
        get { lock (_state) return _currentPose; }
    }

    public bool IsSpeaking
    {
        get { lock (_state) return _speakingOperation is not null; }
    }

    /// <summary>Shows the neutral pose and starts the ambient (idle) behaviour.</summary>
    public void Start()
    {
        ShowPose(_gaze.Neutral.Id);
        if (_options.IdleEnabled)
            _idleTask = RunIdleAsync(_lifetime.Token);
    }

    // ---- Movement -------------------------------------------------------------------------

    /// <summary>Moves the feet anchor to <paramref name="target"/>. A new move cancels the current one.</summary>
    public OperationHandle Move(ScreenPoint target, TimeSpan duration)
    {
        if (!target.IsFinite)
            throw new ArgumentException("Target coordinates must be finite numbers.", nameof(target));
        if (duration < TimeSpan.Zero || duration > _options.MaxMoveDuration)
            throw new ArgumentOutOfRangeException(nameof(duration), $"Duration must be between 0 and {_options.MaxMoveDuration.TotalMilliseconds} ms.");

        var cts = _moveSlot.Begin(_lifetime.Token);
        return Run(MoveKind, cts, _moveSlot, ct => MoveCoreAsync(target, duration, ct));
    }

    /// <summary>Stops the current movement, if any (it completes as cancelled).</summary>
    public void CancelMove() => _moveSlot.Cancel();

    private async Task MoveCoreAsync(ScreenPoint target, TimeSpan duration, CancellationToken ct)
    {
        var from = _stage.FeetPosition;
        if (duration == TimeSpan.Zero)
        {
            _stage.SetFeetPosition(target);
            return;
        }

        var start = _time.GetTimestamp();
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var progress = Math.Min(1, _time.GetElapsedTime(start) / duration);
            _stage.SetFeetPosition(ScreenPoint.Lerp(from, target, Easing.EaseInOutCubic(progress)));
            if (progress >= 1)
                return;
            await Task.Delay(_options.FrameInterval, _time, ct).ConfigureAwait(false);
        }
    }

    // ---- Gaze -----------------------------------------------------------------------------

    /// <summary>Looks up (+1) or down (-1). Returns the pose that was selected.</summary>
    public string LookAt(double pitch)
    {
        var pose = _gaze.ForPitch(pitch);
        lock (_state)
            _gazeHeldUntil = _time.GetUtcNow() + _options.GazeHold;
        ShowPose(pose.Id);
        return pose.Id;
    }

    /// <summary>Looks towards a point of the screen (vertical component only with the current sprites).</summary>
    public string LookAtPoint(ScreenPoint target)
    {
        if (!target.IsFinite)
            throw new ArgumentException("Target coordinates must be finite numbers.", nameof(target));
        return LookAt(_gaze.PitchTowards(_stage.FeetPosition, target));
    }

    public void ResetGaze()
    {
        lock (_state)
            _gazeHeldUntil = DateTimeOffset.MinValue;
        ShowPose(_gaze.Neutral.Id);
    }

    private void ShowPose(string poseId)
    {
        lock (_state)
            _currentPose = poseId;
        _stage.ShowPose(poseId);
    }

    // ---- Messages -------------------------------------------------------------------------

    /// <summary>Shows a text bubble, optionally hidden automatically after <paramref name="duration"/>.</summary>
    public void ShowMessage(string text, TimeSpan? duration = null)
    {
        ValidateText(text);
        if (duration is { } d && (d <= TimeSpan.Zero || d > _options.MaxMessageDuration))
            throw new ArgumentOutOfRangeException(nameof(duration), "Message duration is out of range.");

        var cts = _messageSlot.Begin(_lifetime.Token);
        _stage.ShowMessage(text);
        if (duration is { } delay)
            _ = HideMessageLaterAsync(delay, cts);
        else
            _messageSlot.End(cts);
    }

    public void HideMessage()
    {
        _messageSlot.Cancel();
        _stage.HideMessage();
    }

    private async Task HideMessageLaterAsync(TimeSpan delay, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(delay, _time, cts.Token).ConfigureAwait(false);
            _stage.HideMessage();
        }
        catch (OperationCanceledException)
        {
            // Replaced by another message or explicitly hidden.
        }
        finally
        {
            _messageSlot.End(cts);
        }
    }

    // ---- Speech ---------------------------------------------------------------------------

    /// <summary>Speaks the supplied text with the configured TTS adapter and animates the mouth.</summary>
    public OperationHandle Speak(string text, bool showMessage = true)
    {
        if (_speech is null)
            throw new NotSupportedException("No speech synthesizer is configured.");
        ValidateText(text);

        var cts = _speechSlot.Begin(_lifetime.Token);
        return Run(SpeakKind, cts, _speechSlot, (id, ct) => SpeakCoreAsync(id, text, showMessage, ct));
    }

    public void StopSpeaking()
    {
        _speechSlot.Cancel();
        _stage.HideMessage();
    }

    private async Task SpeakCoreAsync(string operationId, string text, bool showMessage, CancellationToken ct)
    {
        lock (_state)
            _speakingOperation = operationId;
        if (showMessage)
        {
            _messageSlot.Cancel();
            _stage.ShowMessage(text);
        }

        try
        {
            await _speech!.SpeakAsync(text, viseme =>
            {
                if (!ct.IsCancellationRequested)
                    _stage.SetMouthOpenness(VisemeMouth.ForViseme(viseme.Id));
            }, ct).ConfigureAwait(false);
        }
        finally
        {
            var stillCurrent = false;
            lock (_state)
            {
                if (_speakingOperation == operationId)
                {
                    _speakingOperation = null;
                    stillCurrent = true;
                }
            }

            if (stillCurrent)
                _stage.SetMouthOpenness(0);
            if (showMessage && stillCurrent && !ct.IsCancellationRequested)
            {
                var messageCts = _messageSlot.Begin(_lifetime.Token);
                _ = HideMessageLaterAsync(_options.MessageLingerAfterSpeech, messageCts);
            }
        }
    }

    // ---- User input (from the desktop adapter) --------------------------------------------

    public void ReportClick(string button, ScreenPoint position) =>
        Publish(new CharacterClicked(button, position));

    public void SubmitUserMessage(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
            return;
        if (text.Length > _options.MaxTextLength)
            text = text[.._options.MaxTextLength];
        Publish(new MessageSubmitted(text));
    }

    // ---- Infrastructure -------------------------------------------------------------------

    private OperationHandle Run(string kind, CancellationTokenSource cts, OperationSlot slot, Func<CancellationToken, Task> body) =>
        Run(kind, cts, slot, (_, ct) => body(ct));

    private OperationHandle Run(string kind, CancellationTokenSource cts, OperationSlot slot, Func<string, CancellationToken, Task> body)
    {
        var id = $"{kind}-{Interlocked.Increment(ref _operationSequence)}";
        return new OperationHandle(id, kind, RunOperationAsync(id, kind, cts, slot, body));
    }

    private async Task<OperationStatus> RunOperationAsync(
        string id, string kind, CancellationTokenSource cts, OperationSlot slot, Func<string, CancellationToken, Task> body)
    {
        // Let the caller report "accepted" before anything else happens.
        await Task.Yield();

        OperationStatus status;
        string? error = null;
        try
        {
            await body(id, cts.Token).ConfigureAwait(false);
            status = OperationStatus.Completed;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            status = OperationStatus.Cancelled;
        }
        catch (Exception ex)
        {
            status = OperationStatus.Failed;
            error = ex.Message;
        }
        finally
        {
            slot.End(cts);
        }

        Publish(new OperationCompleted(id, kind, status, error));
        return status;
    }

    private async Task RunIdleAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var span = _options.IdleMaxInterval - _options.IdleMinInterval;
                var wait = _options.IdleMinInterval + span * _random.NextDouble();
                await Task.Delay(wait, _time, ct).ConfigureAwait(false);

                if (!IdleMayAct())
                    continue;

                if (CurrentPose != _gaze.Neutral.Id)
                {
                    ShowPose(_gaze.Neutral.Id);
                    continue;
                }

                var glance = PickGlancePose();
                ShowPose(glance);
                await Task.Delay(_options.IdleGlanceDuration, _time, ct).ConfigureAwait(false);
                if (IdleMayAct() && CurrentPose == glance)
                    ShowPose(_gaze.Neutral.Id);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private bool IdleMayAct()
    {
        lock (_state)
            return _speakingOperation is null && _time.GetUtcNow() >= _gazeHeldUntil;
    }

    private string PickGlancePose()
    {
        var poses = _gaze.PosesByPitch;
        var index = poses.ToList().IndexOf(_gaze.Neutral);
        var candidates = new[] { index - 1, index + 1 }.Where(i => i >= 0 && i < poses.Count).ToArray();
        return candidates.Length == 0 ? _gaze.Neutral.Id : poses[candidates[_random.Next(candidates.Length)]].Id;
    }

    private void ValidateText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text must not be empty.", nameof(text));
        if (text.Length > _options.MaxTextLength)
            throw new ArgumentException($"Text must not exceed {_options.MaxTextLength} characters.", nameof(text));
    }

    private void Publish(PuppetEvent puppetEvent)
    {
        try
        {
            _events.Publish(puppetEvent);
        }
        catch
        {
            // An event consumer must never break the puppet.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_lifetime.IsCancellationRequested)
            return;
        _lifetime.Cancel();
        try
        {
            await _idleTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
