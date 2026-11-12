using System.Globalization;
using DeskMate.Transport.SignalR;

namespace DeskMate.Desktop;

/// <summary>Command-line settings: <c>--port 5117 --scale 0.45 --voice "Microsoft Hortense Desktop" --no-tts --character default</c>.</summary>
internal sealed record DesktopSettings(int Port, double Scale, string? Voice, bool SpeechEnabled, string Character)
{
    public static DesktopSettings Parse(string[] args)
    {
        var port = int.TryParse(Environment.GetEnvironmentVariable("DESKMATE_PORT"), out var envPort) ? envPort : ProtocolInfo.DefaultPort;
        var scale = 0.45;
        string? voice = null;
        var speech = true;
        var character = "default";

        for (var i = 0; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Missing value for {args[i]}.");
            switch (args[i])
            {
                case "--port": port = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                case "--scale": scale = double.Parse(Next(), CultureInfo.InvariantCulture); break;
                case "--voice": voice = Next(); break;
                case "--no-tts": speech = false; break;
                case "--character": character = Next(); break;
                default: throw new ArgumentException($"Unknown option '{args[i]}'.");
            }
        }

        if (port is < 1 or > 65535)
            throw new ArgumentException("--port must be between 1 and 65535.");
        if (scale is <= 0.05 or > 4)
            throw new ArgumentException("--scale must be between 0.05 and 4.");
        if (character.IndexOfAny(['/', '\\', '.']) >= 0)
            throw new ArgumentException("--character must be a folder name under 'characters'.");
        return new DesktopSettings(port, scale, voice, speech, character);
    }
}
