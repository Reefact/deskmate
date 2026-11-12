using System.Globalization;
using System.Speech.Synthesis;
using DeskMate.Core.Ports;

namespace DeskMate.Desktop;

/// <summary>
/// Driven adapter: local Windows text-to-speech (SAPI via System.Speech), played on the default audio device.
/// Visemes are raised by SAPI as playback reaches them, which keeps the mouth in sync with the audio.
/// </summary>
internal sealed class WindowsSpeechSynthesizer : ISpeechSynthesizer, IDisposable
{
    private readonly SpeechSynthesizer _synth;

    private WindowsSpeechSynthesizer(SpeechSynthesizer synth) => _synth = synth;

    public static WindowsSpeechSynthesizer? TryCreate(string? voiceName)
    {
        try
        {
            var synth = new SpeechSynthesizer();
            var voices = synth.GetInstalledVoices().Where(v => v.Enabled).Select(v => v.VoiceInfo).ToList();
            if (voices.Count == 0)
            {
                synth.Dispose();
                return null;
            }

            var preferred = voiceName is not null
                ? voices.FirstOrDefault(v => v.Name.Equals(voiceName, StringComparison.OrdinalIgnoreCase))
                : voices.FirstOrDefault(v => v.Culture.Equals(CultureInfo.CurrentUICulture))
                  ?? voices.FirstOrDefault(v => v.Culture.TwoLetterISOLanguageName == CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            if (preferred is not null)
                synth.SelectVoice(preferred.Name);

            synth.SetOutputToDefaultAudioDevice();
            return new WindowsSpeechSynthesizer(synth);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Task SpeakAsync(string text, Action<SpeechViseme> onViseme, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var prompt = new Prompt(text);

        void OnViseme(object? sender, VisemeReachedEventArgs e)
        {
            if (ReferenceEquals(e.Prompt, prompt))
                onViseme(new SpeechViseme(e.Viseme, e.Duration));
        }

        void OnCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            if (!ReferenceEquals(e.Prompt, prompt))
                return;
            _synth.VisemeReached -= OnViseme;
            _synth.SpeakCompleted -= OnCompleted;
            if (e.Cancelled)
                tcs.TrySetCanceled(cancellationToken);
            else if (e.Error is not null)
                tcs.TrySetException(e.Error);
            else
                tcs.TrySetResult();
        }

        _synth.VisemeReached += OnViseme;
        _synth.SpeakCompleted += OnCompleted;
        var registration = cancellationToken.Register(() => _synth.SpeakAsyncCancel(prompt));
        _ = tcs.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);

        _synth.SpeakAsync(prompt);
        return tcs.Task;
    }

    public void Dispose() => _synth.Dispose();
}
