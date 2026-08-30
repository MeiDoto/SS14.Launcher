using System;
using System.Globalization;
using Avalonia.Media;

namespace SS14.Launcher.Utility;

public static class PaletteUtility
{
    public static bool TryParseHexColor(string? input, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var clean = input.Trim().TrimStart('#');

        if (clean.Length == 3)
        {
            var r = new string(clean[0], 2);
            var g = new string(clean[1], 2);
            var b = new string(clean[2], 2);
            clean = $"FF{r}{g}{b}";
        }
        else if (clean.Length == 4)
        {
            var a = new string(clean[0], 2);
            var r = new string(clean[1], 2);
            var g = new string(clean[2], 2);
            var b = new string(clean[3], 2);
            clean = $"{a}{r}{g}{b}";
        }
        else if (clean.Length == 6)
        {
            clean = $"FF{clean}";
        }
        else if (clean.Length != 8)
        {
            return false;
        }

        if (uint.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
        {
            var a = (byte)((argb >> 24) & 0xFF);
            var r = (byte)((argb >> 16) & 0xFF);
            var g = (byte)((argb >> 8) & 0xFF);
            var b = (byte)(argb & 0xFF);
            color = Color.FromArgb(a, r, g, b);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculates relative luminance (WCAG 2.1 standard)
    /// </summary>
    public static double GetRelativeLuminance(Color color)
    {
        double R = Linearize(color.R / 255.0);
        double G = Linearize(color.G / 255.0);
        double B = Linearize(color.B / 255.0);

        return 0.2126 * R + 0.7152 * G + 0.0722 * B;

        static double Linearize(double val) =>
            val <= 0.03928 ? val / 12.92 : Math.Pow((val + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    /// Computes contrast ratio between two colors according to WCAG 2.1 specifications (1.0 to 21.0).
    /// </summary>
    public static double GetContrastRatio(Color foreground, Color background)
    {
        var l1 = GetRelativeLuminance(foreground);
        var l2 = GetRelativeLuminance(background);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Returns optimal contrasting text color (pure white or deep dark) for a given background color.
    /// </summary>
    public static Color GetOptimalTextColor(Color backgroundColor)
    {
        var lum = GetRelativeLuminance(backgroundColor);
        return lum > 0.4 ? Color.FromRgb(0x18, 0x18, 0x1E) : Color.FromRgb(0xFA, 0xFA, 0xFA);
    }
}
