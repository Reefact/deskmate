using System.IO;
using System.Windows;
using DeskMate.Core;
using DeskMate.Core.Characters;
using DeskMate.Transport.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeskMate.Desktop;

/// <summary>Composition root: wires the WPF stage, Windows TTS and the SignalR adapter around the puppet core.</summary>
public partial class App : Application
{
    private WebApplication? _server;
    private Puppet? _puppet;
    private WindowsSpeechSynthesizer? _speech;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DesktopSettings settings;
        CharacterManifest character;
        string characterDirectory;
        try
        {
            settings = DesktopSettings.Parse(e.Args);
            characterDirectory = Path.Combine(AppContext.BaseDirectory, "characters", settings.Character);
            character = CharacterManifest.Load(Path.Combine(characterDirectory, "character.json"));
        }
        catch (Exception ex)
        {
            Fail($"DeskMate could not start:\n{ex.Message}");
            return;
        }

        var window = new CharacterWindow(character, characterDirectory, settings.Scale);
        var stage = new WpfCharacterStage(window);
        _speech = settings.SpeechEnabled ? WindowsSpeechSynthesizer.TryCreate(settings.Voice) : null;
        var options = new PuppetOptions { Scale = settings.Scale };

        _server = DeskMateControlServer.Build(
            settings.Port,
            sink => new Puppet(character, stage, sink, _speech, options: options),
            logging => logging.SetMinimumLevel(LogLevel.Warning));
        var puppet = _puppet = _server.Services.GetRequiredService<Puppet>();

        window.Clicked += puppet.ReportClick;
        window.MessageSubmitted += puppet.SubmitUserMessage;
        window.DragStarted += puppet.CancelMove;
        window.ResetGazeRequested += puppet.ResetGaze;

        var workArea = SystemParameters.WorkArea;
        window.SetFeetPosition(new ScreenPoint(workArea.Right - 180, workArea.Bottom - 8));
        MainWindow = window;
        window.Show();
        puppet.Start();

        try
        {
            await _server.StartAsync();
        }
        catch (Exception ex)
        {
            Fail($"DeskMate could not listen on 127.0.0.1:{settings.Port}.\n{ex.Message}\n\nTry --port <other>.");
            return;
        }

        var endpoint = $"http://127.0.0.1:{settings.Port}{ProtocolInfo.HubPath}";
        puppet.ShowMessage(
            $"Hi! I'm listening on {endpoint}" + (puppet.CanSpeak ? "" : "\n(no text-to-speech voice available)"),
            TimeSpan.FromSeconds(8));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Avoid blocking the dispatcher on async shutdown work.
        Task.Run(async () =>
        {
            if (_puppet is not null)
                await _puppet.DisposeAsync();
            if (_server is not null)
                await _server.DisposeAsync();
        }).Wait(TimeSpan.FromSeconds(3));
        _speech?.Dispose();
        base.OnExit(e);
    }

    private void Fail(string message)
    {
        MessageBox.Show(message, "DeskMate", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(1);
    }
}
