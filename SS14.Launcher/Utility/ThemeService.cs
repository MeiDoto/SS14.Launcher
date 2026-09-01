using System;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Serilog;
using SS14.Launcher.Models.Data;

namespace SS14.Launcher.Utility;

/// <summary>
/// Service responsible for managing UI themes, custom palette colors, font sizes, and custom launcher branding.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Applies custom theme colors, brushes, and font sizes from the current configuration to Avalonia application resources.
    /// </summary>
    void ApplyTheme(DataManager cfg, bool hasCustomBackground);

    /// <summary>
    /// Safely loads a bitmap from disk without throwing unhandled exceptions.
    /// </summary>
    Bitmap? LoadBitmapSafely(string? path);
}

public sealed class ThemeService : IThemeService
{
    public static readonly ThemeService Instance = new();

    public void ApplyTheme(DataManager cfg, bool hasCustomBackground)
    {
        if (Application.Current?.Resources is not { } res)
            return;

        var accent = cfg.GetCVar(CVars.CustomAccentColor);
        if (!string.IsNullOrWhiteSpace(accent) && PaletteUtility.TryParseHexColor(accent, out var accentCol))
        {
            res["ThemeNanoGoldBrush"] = new SolidColorBrush(accentCol);
            res["ThemeNanoGoldColor"] = accentCol;
        }

        var btnColHex = cfg.GetCVar(CVars.CustomButtonColor);
        if (!string.IsNullOrWhiteSpace(btnColHex) && PaletteUtility.TryParseHexColor(btnColHex, out var btnCol))
        {
            res["ThemeControlMidBrush"] = new SolidColorBrush(btnCol);
            res["ThemeControlMidColor"] = btnCol;
        }

        var tabColHex = cfg.GetCVar(CVars.CustomTabSelectedColor);
        if (!string.IsNullOrWhiteSpace(tabColHex) && PaletteUtility.TryParseHexColor(tabColHex, out var tabCol))
        {
            res["ThemeTabItemSelectedBrush"] = new SolidColorBrush(tabCol);
            res["ThemeControlHighBrush"] = new SolidColorBrush(tabCol);
            res["ThemeControlHighColor"] = tabCol;
        }

        var textColHex = cfg.GetCVar(CVars.CustomTextColor);
        if (!string.IsNullOrWhiteSpace(textColHex) && PaletteUtility.TryParseHexColor(textColHex, out var textCol))
        {
            res["ThemeForegroundBrush"] = new SolidColorBrush(textCol);
            res["ThemeForegroundColor"] = textCol;
        }

        var popupBgHex = cfg.GetCVar(CVars.CustomPopupBackgroundColor);
        if (!string.IsNullOrWhiteSpace(popupBgHex) && PaletteUtility.TryParseHexColor(popupBgHex, out var popupCol))
        {
            res["ThemePopupBackgroundBrush"] = new SolidColorBrush(popupCol);
            res["ThemePopupBackgroundColor"] = popupCol;
        }

        var fontSize = cfg.GetCVar(CVars.CustomFontSize);
        if (fontSize >= 10 && fontSize <= 26)
        {
            res["FontSizeNormal"] = (double)fontSize;
        }

        if (hasCustomBackground)
        {
            res["ThemeBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x2A));
            res["ThemeServerListBackgroundBrush"] = new SolidColorBrush(Color.FromArgb(0x28, 0x10, 0x10, 0x18));
        }
    }

    public Bitmap? LoadBitmapSafely(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            return new Bitmap(path);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load image from {Path}", path);
            return null;
        }
    }
}
