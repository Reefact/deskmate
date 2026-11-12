namespace DeskMate.Core.Animation;

/// <summary>
/// Maps the 22 SAPI/Azure visemes to a mouth openness in [0, 1].
/// Dedicated mouth sprites will replace this with real shapes once the mouth layer exists.
/// </summary>
public static class VisemeMouth
{
    // 0 silence, 1 ae/ax/ah, 2 aa, 3 ao, 4 ey/eh/uh, 5 er, 6 y/iy/ih/ix, 7 w/uw, 8 ow, 9 aw, 10 oy,
    // 11 ay, 12 h, 13 r, 14 l, 15 s/z, 16 sh/ch/jh/zh, 17 th/dh, 18 f/v, 19 d/t/n, 20 k/g/ng, 21 p/b/m
    private static readonly double[] Openness =
    [
        0.0, 0.8, 1.0, 0.85, 0.6, 0.5, 0.35, 0.3, 0.7, 0.9, 0.7,
        0.9, 0.5, 0.4, 0.45, 0.2, 0.3, 0.3, 0.15, 0.3, 0.4, 0.0,
    ];

    public static double ForViseme(int visemeId) =>
        visemeId >= 0 && visemeId < Openness.Length ? Openness[visemeId] : 0.3;
}
