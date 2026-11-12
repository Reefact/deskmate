namespace DeskMate.Core.Ports;

/// <summary>
/// Driven port: technical text-to-speech, including playback. This is presentation, not intelligence:
/// the text is always supplied by an external controller.
/// </summary>
public interface ISpeechSynthesizer
{
    /// <summary>
    /// Speaks <paramref name="text"/> and completes when playback ends.
    /// <paramref name="onViseme"/> is invoked when a viseme is reached <b>during playback</b>, if the provider supports it.
    /// Cancelling the token stops the speech; the task then completes as cancelled.
    /// </summary>
    Task SpeakAsync(string text, Action<SpeechViseme> onViseme, CancellationToken cancellationToken);
}

/// <summary>
/// A mouth shape reached during playback. <see cref="Id"/> follows the common 22-shape set
/// (0 = silence … 21) used by Windows SAPI and Azure Speech.
/// </summary>
public readonly record struct SpeechViseme(int Id, TimeSpan Duration);
