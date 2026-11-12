namespace DeskMate.Core;

public sealed record PuppetOptions
{
    /// <summary>Display scale of the sprite frames (1 = native frame pixels).</summary>
    public double Scale { get; init; } = 0.45;

    /// <summary>Refresh interval used to interpolate movements.</summary>
    public TimeSpan FrameInterval { get; init; } = TimeSpan.FromMilliseconds(16);

    /// <summary>How long an explicitly requested gaze is kept before idle behaviour may change it.</summary>
    public TimeSpan GazeHold { get; init; } = TimeSpan.FromSeconds(6);

    /// <summary>How long the speech bubble stays visible after speaking.</summary>
    public TimeSpan MessageLingerAfterSpeech { get; init; } = TimeSpan.FromSeconds(1.5);

    public TimeSpan MaxMoveDuration { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan MaxMessageDuration { get; init; } = TimeSpan.FromMinutes(10);
    public int MaxTextLength { get; init; } = 2000;

    /// <summary>Ambient behaviour: small, random glances when nothing else happens.</summary>
    public bool IdleEnabled { get; init; } = true;
    public TimeSpan IdleMinInterval { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan IdleMaxInterval { get; init; } = TimeSpan.FromSeconds(8);
    public TimeSpan IdleGlanceDuration { get; init; } = TimeSpan.FromSeconds(1.2);

    public int? RandomSeed { get; init; }
}
