namespace DeskMate.Core;

/// <summary>A point in desktop coordinates (WPF device-independent units of the virtual screen in the PoC).</summary>
public readonly record struct ScreenPoint(double X, double Y)
{
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);

    public static ScreenPoint Lerp(ScreenPoint from, ScreenPoint to, double t) =>
        new(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t);

    public override string ToString() => $"({X:0.#}, {Y:0.#})";
}
