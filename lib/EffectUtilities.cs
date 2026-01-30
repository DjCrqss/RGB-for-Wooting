using System.Windows.Media;

namespace WootingRGB.lib;

/// <summary>
/// Shared utility functions for RGB effects
/// </summary>
public static class EffectUtilities
{
    #region Color Interpolation

    /// <summary>
    /// Linearly interpolates between two colors
    /// </summary>
    /// <param name="start">Starting color</param>
    /// <param name="end">Ending color</param>
    /// <param name="t">Interpolation factor (0-1)</param>
    /// <returns>Interpolated color</returns>
    public static Color LerpColor(Color start, Color end, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromRgb(
            (byte)(start.R + (end.R - start.R) * t),
            (byte)(start.G + (end.G - start.G) * t),
            (byte)(start.B + (end.B - start.B) * t)
        );
    }

    /// <summary>
    /// Linearly interpolates between two colors and returns RGB tuple
    /// </summary>
    public static (byte R, byte G, byte B) LerpColorTuple(Color start, Color end, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return (
            (byte)(start.R + (end.R - start.R) * t),
            (byte)(start.G + (end.G - start.G) * t),
            (byte)(start.B + (end.B - start.B) * t)
        );
    }

    #endregion

    #region HSV Color Conversion

    /// <summary>
    /// Converts RGB color to HSV (Hue, Saturation, Value)
    /// </summary>
    /// <param name="color">RGB color</param>
    /// <returns>Tuple of (hue: 0-360, saturation: 0-1, value: 0-1)</returns>
    public static (double hue, double saturation, double value) RgbToHsv(Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double h = 0;
        if (delta != 0)
        {
            if (max == r)
                h = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                h = 60 * (((b - r) / delta) + 2);
            else
                h = 60 * (((r - g) / delta) + 4);
        }
        if (h < 0) h += 360;

        double s = max == 0 ? 0 : delta / max;
        double v = max;

        return (h, s, v);
    }

    /// <summary>
    /// Converts HSV (Hue, Saturation, Value) to RGB color
    /// </summary>
    /// <param name="hue">Hue (0-360)</param>
    /// <param name="saturation">Saturation (0-1)</param>
    /// <param name="value">Value (0-1)</param>
    /// <returns>RGB color</returns>
    public static Color HsvToRgb(double hue, double saturation, double value)
    {
        double c = value * saturation;
        double x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
        double m = value - c;

        double r = 0, g = 0, b = 0;

        if (hue < 60) { r = c; g = x; b = 0; }
        else if (hue < 120) { r = x; g = c; b = 0; }
        else if (hue < 180) { r = 0; g = c; b = x; }
        else if (hue < 240) { r = 0; g = x; b = c; }
        else if (hue < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255)
        );
    }

    #endregion

    #region Mathematical Utilities

    /// <summary>
    /// Calculates Euclidean distance between two points
    /// </summary>
    public static double Distance(double x1, double y1, double x2, double y2)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Calculates Manhattan distance between two points
    /// </summary>
    public static double ManhattanDistance(double x1, double y1, double x2, double y2)
    {
        return Math.Abs(x2 - x1) + Math.Abs(y2 - y1);
    }

    /// <summary>
    /// Linearly interpolates between two values
    /// </summary>
    public static double Lerp(double start, double end, double t)
    {
        return start + (end - start) * Math.Clamp(t, 0, 1);
    }

    /// <summary>
    /// Remaps a value from one range to another
    /// </summary>
    public static double Remap(double value, double fromMin, double fromMax, double toMin, double toMax)
    {
        double t = (value - fromMin) / (fromMax - fromMin);
        return Lerp(toMin, toMax, t);
    }

    /// <summary>
    /// Applies a smooth step function (cubic Hermite interpolation)
    /// </summary>
    public static double SmoothStep(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>
    /// Applies a smoother step function (quintic Hermite interpolation)
    /// </summary>
    public static double SmootherStep(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    #endregion
}
