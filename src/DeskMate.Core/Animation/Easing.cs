namespace DeskMate.Core.Animation;

public static class Easing
{
    /// <summary>Smooth acceleration and deceleration, t in [0, 1].</summary>
    public static double EaseInOutCubic(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
    }
}
