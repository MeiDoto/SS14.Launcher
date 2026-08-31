using System;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public sealed class LauncherCustomizerViewModel : ViewModelBase
{
    private readonly DataManager _dataManager = Locator.Current.GetRequiredService<DataManager>();

    private string _customBackgroundImagePath = "";
    private float _customBackgroundOpacity = 0.85f;
    private string _customLogoImagePath = "";
    private string _customAccentColor = "";
    private string _customButtonColor = "";
    private string _customTabSelectedColor = "";
    private string _customTextColor = "";
    private string _customPopupBackgroundColor = "";
    private float _customFontSize = 15.0f;
    private string _customWindowTitle = "";
    private bool _enableClickVfx = true;
    private string _customUserCode = "";
    private string _scriptOutputText = "Ready.";

    private string _customHomeTabName = "";
    private string _customServersTabName = "";
    private string _customNewsTabName = "";
    private string _customReplaysTabName = "";
    private string _customOptionsTabName = "";

    private Bitmap? _previewBackground;
    private Bitmap? _previewLogo;
    private readonly AnimatedBackgroundManager _previewBgManager = new();

    public LauncherCustomizerViewModel()
    {
        _previewBgManager.FrameUpdated += frame =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                PreviewBackground = frame;
                OnPropertyChanged(nameof(HasPreviewBackground));
            });
        };
    }

    public string CustomBackgroundImagePath
    {
        get => _customBackgroundImagePath;
        set
        {
            if (SetProperty(ref _customBackgroundImagePath, value))
            {
                UpdateBackgroundPreview();
            }
        }
    }

    public float CustomBackgroundOpacity
    {
        get => _customBackgroundOpacity;
        set
        {
            if (SetProperty(ref _customBackgroundOpacity, value))
            {
                OnPropertyChanged(nameof(OpacityPercentageText));
            }
        }
    }

    public string OpacityPercentageText => $"{(int)(_customBackgroundOpacity * 100)}%";

    public string CustomLogoImagePath
    {
        get => _customLogoImagePath;
        set
        {
            if (SetProperty(ref _customLogoImagePath, value))
            {
                UpdateLogoPreview();
            }
        }
    }

    private bool _livePreviewEnabled = true;
    public bool LivePreviewEnabled
    {
        get => _livePreviewEnabled;
        set => SetProperty(ref _livePreviewEnabled, value);
    }

    public record struct CustomizerSnapshot(
        string BackgroundPath,
        float BackgroundOpacity,
        string LogoPath,
        string AccentColor,
        string ButtonColor,
        string TabSelectedColor,
        string TextColor,
        string PopupBackgroundColor,
        float FontSize,
        string WindowTitle,
        bool EnableClickVfx,
        string UserCode,
        string HomeTab,
        string ServersTab,
        string NewsTab,
        string ReplaysTab,
        string OptionsTab,
        string TabPlacement
    );

    private CustomizerSnapshot _initialSnapshot;

    public void RestoreInitialSnapshot()
    {
        CustomBackgroundImagePath = _initialSnapshot.BackgroundPath;
        CustomBackgroundOpacity = _initialSnapshot.BackgroundOpacity;
        CustomLogoImagePath = _initialSnapshot.LogoPath;
        CustomAccentColor = _initialSnapshot.AccentColor;
        CustomButtonColor = _initialSnapshot.ButtonColor;
        CustomTabSelectedColor = _initialSnapshot.TabSelectedColor;
        CustomTextColor = _initialSnapshot.TextColor;
        CustomPopupBackgroundColor = _initialSnapshot.PopupBackgroundColor;
        CustomFontSize = _initialSnapshot.FontSize;
        CustomWindowTitle = _initialSnapshot.WindowTitle;
        EnableClickVfx = _initialSnapshot.EnableClickVfx;
        CustomUserCode = _initialSnapshot.UserCode;
        CustomHomeTabName = _initialSnapshot.HomeTab;
        CustomServersTabName = _initialSnapshot.ServersTab;
        CustomNewsTabName = _initialSnapshot.NewsTab;
        CustomReplaysTabName = _initialSnapshot.ReplaysTab;
        CustomOptionsTabName = _initialSnapshot.OptionsTab;
        CustomTabPlacement = _initialSnapshot.TabPlacement;

        ApplyLivePreview();
    }

    private Avalonia.Threading.DispatcherTimer? _livePreviewTimer;

    private void ScheduleLivePreview()
    {
        if (!_livePreviewEnabled) return;

        _livePreviewTimer?.Stop();
        _livePreviewTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _livePreviewTimer.Tick += (_, _) =>
        {
            _livePreviewTimer.Stop();
            ApplyLivePreview();
        };
        _livePreviewTimer.Start();
    }

    public void ApplyLivePreview()
    {
        var app = Avalonia.Application.Current;
        if (app?.Resources is { } res)
        {
            if (!string.IsNullOrWhiteSpace(_customAccentColor) && PaletteUtility.TryParseHexColor(_customAccentColor, out var accentCol))
            {
                res["ThemeNanoGoldBrush"] = new Avalonia.Media.SolidColorBrush(accentCol);
                res["ThemeNanoGoldColor"] = accentCol;
            }

            if (!string.IsNullOrWhiteSpace(_customButtonColor) && PaletteUtility.TryParseHexColor(_customButtonColor, out var btnCol))
            {
                res["ThemeControlMidBrush"] = new Avalonia.Media.SolidColorBrush(btnCol);
                res["ThemeControlMidColor"] = btnCol;
                var hoverCol = Avalonia.Media.Color.FromArgb(
                    btnCol.A,
                    (byte)Math.Min(255, btnCol.R + 25),
                    (byte)Math.Min(255, btnCol.G + 25),
                    (byte)Math.Min(255, btnCol.B + 35));
                res["ThemeButtonHoveredBrush"] = new Avalonia.Media.SolidColorBrush(hoverCol);
            }

            if (!string.IsNullOrWhiteSpace(_customTabSelectedColor) && PaletteUtility.TryParseHexColor(_customTabSelectedColor, out var tabCol))
            {
                res["ThemeTabItemSelectedBrush"] = new Avalonia.Media.SolidColorBrush(tabCol);
                res["ThemeControlHighBrush"] = new Avalonia.Media.SolidColorBrush(tabCol);
                res["ThemeControlHighColor"] = tabCol;
            }

            if (!string.IsNullOrWhiteSpace(_customTextColor) && PaletteUtility.TryParseHexColor(_customTextColor, out var textCol))
            {
                res["ThemeForegroundBrush"] = new Avalonia.Media.SolidColorBrush(textCol);
                res["ThemeForegroundColor"] = textCol;
            }

            if (!string.IsNullOrWhiteSpace(_customPopupBackgroundColor) && PaletteUtility.TryParseHexColor(_customPopupBackgroundColor, out var popupCol))
            {
                res["ThemePopupBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(popupCol);
                res["ThemePopupBackgroundColor"] = popupCol;
            }

            if (_customFontSize >= 10 && _customFontSize <= 26)
            {
                res["FontSizeNormal"] = (double)_customFontSize;
            }
        }

        OnPropertyChanged(nameof(LivePreviewAccentBrush));
        OnPropertyChanged(nameof(LivePreviewButtonBrush));
        OnPropertyChanged(nameof(LivePreviewTabSelectedBrush));
        OnPropertyChanged(nameof(LivePreviewTextBrush));
        OnPropertyChanged(nameof(LivePreviewPopupBrush));
        OnPropertyChanged(nameof(EffectiveWindowTitle));
        OnPropertyChanged(nameof(EffectiveHomeTabName));
        OnPropertyChanged(nameof(EffectiveServersTabName));
        OnPropertyChanged(nameof(EffectiveNewsTabName));
    }

    public Avalonia.Media.IBrush LivePreviewAccentBrush => PaletteUtility.TryParseHexColor(_customAccentColor, out var col)
        ? new Avalonia.Media.SolidColorBrush(col)
        : (Avalonia.Media.IBrush)(Avalonia.Application.Current?.Resources["ThemeNanoGoldBrush"] as Avalonia.Media.IBrush ?? Avalonia.Media.Brushes.Gold);

    public Avalonia.Media.IBrush LivePreviewButtonBrush => PaletteUtility.TryParseHexColor(_customButtonColor, out var col)
        ? new Avalonia.Media.SolidColorBrush(col)
        : (Avalonia.Media.IBrush)(Avalonia.Application.Current?.Resources["ThemeControlMidBrush"] as Avalonia.Media.IBrush ?? Avalonia.Media.Brushes.DarkSlateGray);

    public Avalonia.Media.IBrush LivePreviewTabSelectedBrush => PaletteUtility.TryParseHexColor(_customTabSelectedColor, out var col)
        ? new Avalonia.Media.SolidColorBrush(col)
        : (Avalonia.Media.IBrush)(Avalonia.Application.Current?.Resources["ThemeTabItemSelectedBrush"] as Avalonia.Media.IBrush ?? Avalonia.Media.Brushes.DarkGreen);

    public Avalonia.Media.IBrush LivePreviewTextBrush => PaletteUtility.TryParseHexColor(_customTextColor, out var col)
        ? new Avalonia.Media.SolidColorBrush(col)
        : (Avalonia.Media.IBrush)(Avalonia.Application.Current?.Resources["ThemeForegroundBrush"] as Avalonia.Media.IBrush ?? Avalonia.Media.Brushes.White);

    public Avalonia.Media.IBrush LivePreviewPopupBrush => PaletteUtility.TryParseHexColor(_customPopupBackgroundColor, out var col)
        ? new Avalonia.Media.SolidColorBrush(col)
        : (Avalonia.Media.IBrush)(Avalonia.Application.Current?.Resources["ThemePopupBackgroundBrush"] as Avalonia.Media.IBrush ?? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x20, 0x20, 0x25)));

    public string EffectiveWindowTitle => string.IsNullOrWhiteSpace(_customWindowTitle)
        ? "Space Station 14 Launcher"
        : _customWindowTitle;

    public string EffectiveHomeTabName => string.IsNullOrWhiteSpace(_customHomeTabName)
        ? LocalizationManager.Instance.GetString("tab-home-title")
        : _customHomeTabName;

    public string EffectiveServersTabName => string.IsNullOrWhiteSpace(_customServersTabName)
        ? LocalizationManager.Instance.GetString("tab-servers-title")
        : _customServersTabName;

    public string EffectiveNewsTabName => string.IsNullOrWhiteSpace(_customNewsTabName)
        ? LocalizationManager.Instance.GetString("tab-news-title")
        : _customNewsTabName;

    public string CustomAccentColor
    {
        get => _customAccentColor;
        set
        {
            if (SetProperty(ref _customAccentColor, value))
                ScheduleLivePreview();
        }
    }

    public string CustomButtonColor
    {
        get => _customButtonColor;
        set
        {
            if (SetProperty(ref _customButtonColor, value))
                ScheduleLivePreview();
        }
    }

    public string CustomTabSelectedColor
    {
        get => _customTabSelectedColor;
        set
        {
            if (SetProperty(ref _customTabSelectedColor, value))
                ScheduleLivePreview();
        }
    }

    public string CustomTextColor
    {
        get => _customTextColor;
        set
        {
            if (SetProperty(ref _customTextColor, value))
                ScheduleLivePreview();
        }
    }

    public string CustomPopupBackgroundColor
    {
        get => _customPopupBackgroundColor;
        set
        {
            if (SetProperty(ref _customPopupBackgroundColor, value))
                ScheduleLivePreview();
        }
    }

    public float CustomFontSize
    {
        get => _customFontSize;
        set
        {
            if (SetProperty(ref _customFontSize, value))
            {
                OnPropertyChanged(nameof(FontSizeText));
                ScheduleLivePreview();
            }
        }
    }

    public string FontSizeText => $"{(int)_customFontSize} pt";

    public string CustomWindowTitle
    {
        get => _customWindowTitle;
        set
        {
            if (SetProperty(ref _customWindowTitle, value))
            {
                OnPropertyChanged(nameof(EffectiveWindowTitle));
            }
        }
    }

    public bool EnableClickVfx
    {
        get => _enableClickVfx;
        set => SetProperty(ref _enableClickVfx, value);
    }

    public string CustomUserCode
    {
        get => _customUserCode;
        set => SetProperty(ref _customUserCode, value);
    }

    public string ScriptOutputText
    {
        get => _scriptOutputText;
        set => SetProperty(ref _scriptOutputText, value);
    }

    public string CustomHomeTabName
    {
        get => _customHomeTabName;
        set => SetProperty(ref _customHomeTabName, value);
    }

    public string CustomServersTabName
    {
        get => _customServersTabName;
        set => SetProperty(ref _customServersTabName, value);
    }

    public string CustomNewsTabName
    {
        get => _customNewsTabName;
        set => SetProperty(ref _customNewsTabName, value);
    }

    public string CustomReplaysTabName
    {
        get => _customReplaysTabName;
        set => SetProperty(ref _customReplaysTabName, value);
    }

    public string CustomOptionsTabName
    {
        get => _customOptionsTabName;
        set => SetProperty(ref _customOptionsTabName, value);
    }

    private string _customTabPlacement = "Top";
    public string CustomTabPlacement
    {
        get => _customTabPlacement;
        set => SetProperty(ref _customTabPlacement, value);
    }

    public string[] PlacementOptions => ["Top", "Bottom", "Left", "Right"];

    public Bitmap? PreviewBackground
    {
        get => _previewBackground;
        private set => SetProperty(ref _previewBackground, value);
    }

    public Bitmap? PreviewLogo
    {
        get => _previewLogo;
        private set => SetProperty(ref _previewLogo, value);
    }

    public bool HasPreviewBackground => PreviewBackground != null;
    public bool HasPreviewLogo => PreviewLogo != null;

    private void UpdateBackgroundPreview()
    {
        _previewBgManager.Load(_customBackgroundImagePath);
        PreviewBackground = _previewBgManager.CurrentFrame;
        OnPropertyChanged(nameof(HasPreviewBackground));
    }

    private void UpdateLogoPreview()
    {
        PreviewLogo?.Dispose();
        PreviewLogo = null;

        if (!string.IsNullOrWhiteSpace(_customLogoImagePath) && File.Exists(_customLogoImagePath))
        {
            try
            {
                PreviewLogo = new Bitmap(_customLogoImagePath);
            }
            catch
            {
                PreviewLogo = null;
            }
        }

        OnPropertyChanged(nameof(HasPreviewLogo));
    }

    public void ClearBackground() => CustomBackgroundImagePath = "";
    public void ClearLogo() => CustomLogoImagePath = "";

    public void ClearHomeTab() => CustomHomeTabName = "";
    public void ClearServersTab() => CustomServersTabName = "";
    public void ClearNewsTab() => CustomNewsTabName = "";
    public void ClearReplaysTab() => CustomReplaysTabName = "";
    public void ClearOptionsTab() => CustomOptionsTabName = "";

    public void SetPresetClassic()
    {
        CustomAccentColor = "#ADA24B";
        CustomButtonColor = "#464966";
        CustomTabSelectedColor = "#3E6C45";
        CustomTextColor = "#EEEEEE";
        CustomPopupBackgroundColor = "#202025";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetCyberpunk()
    {
        CustomAccentColor = "#00F2FE";
        CustomButtonColor = "#2D1B4E";
        CustomTabSelectedColor = "#FF007F";
        CustomTextColor = "#E0F7FA";
        CustomPopupBackgroundColor = "#1A102F";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetSyndicate()
    {
        CustomAccentColor = "#E50914";
        CustomButtonColor = "#26262B";
        CustomTabSelectedColor = "#990000";
        CustomTextColor = "#F5F5F5";
        CustomPopupBackgroundColor = "#1A1A1E";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetSolar()
    {
        CustomAccentColor = "#FFA000";
        CustomButtonColor = "#1E3C72";
        CustomTabSelectedColor = "#FF8C00";
        CustomTextColor = "#FFF8E1";
        CustomPopupBackgroundColor = "#121E36";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetDeepSpace()
    {
        CustomAccentColor = "#A29BFE";
        CustomButtonColor = "#2C2C54";
        CustomTabSelectedColor = "#6C5CE7";
        CustomTextColor = "#F1F2F6";
        CustomPopupBackgroundColor = "#191933";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetMatrix()
    {
        CustomAccentColor = "#00FF66";
        CustomButtonColor = "#0D2818";
        CustomTabSelectedColor = "#00B33C";
        CustomTextColor = "#D8F3DC";
        CustomPopupBackgroundColor = "#081C10";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetMonochrome()
    {
        CustomAccentColor = "#FFFFFF";
        CustomButtonColor = "#333333";
        CustomTabSelectedColor = "#555555";
        CustomTextColor = "#F0F0F0";
        CustomPopupBackgroundColor = "#181818";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetCentComm()
    {
        CustomAccentColor = "#2ED573";
        CustomButtonColor = "#1B3D2F";
        CustomTabSelectedColor = "#10AC84";
        CustomTextColor = "#E8F8F0";
        CustomPopupBackgroundColor = "#0B1A14";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetPlasma()
    {
        CustomAccentColor = "#A55EEA";
        CustomButtonColor = "#361642";
        CustomTabSelectedColor = "#FF793F";
        CustomTextColor = "#F5CD79";
        CustomPopupBackgroundColor = "#15081E";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetSingularity()
    {
        CustomAccentColor = "#70A1FF";
        CustomButtonColor = "#152238";
        CustomTabSelectedColor = "#1E90FF";
        CustomTextColor = "#E0EFFF";
        CustomPopupBackgroundColor = "#060B14";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetSec()
    {
        CustomAccentColor = "#E84118";
        CustomButtonColor = "#2C1A1D";
        CustomTabSelectedColor = "#C23616";
        CustomTextColor = "#F5F6FA";
        CustomPopupBackgroundColor = "#120E10";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetMed()
    {
        CustomAccentColor = "#00D2D3";
        CustomButtonColor = "#10363B";
        CustomTabSelectedColor = "#01A3A4";
        CustomTextColor = "#E8FFFF";
        CustomPopupBackgroundColor = "#081619";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetCyberGoth()
    {
        CustomAccentColor = "#7BED9F";
        CustomButtonColor = "#201B2B";
        CustomTabSelectedColor = "#8854D0";
        CustomTextColor = "#F1F2F6";
        CustomPopupBackgroundColor = "#0F0C16";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetClown()
    {
        CustomAccentColor = "#FFD32A";
        CustomButtonColor = "#48161A";
        CustomTabSelectedColor = "#FF3838";
        CustomTextColor = "#FFFFFF";
        CustomPopupBackgroundColor = "#1A1012";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetSynthwave()
    {
        CustomAccentColor = "#FF7675";
        CustomButtonColor = "#2D132C";
        CustomTabSelectedColor = "#E84393";
        CustomTextColor = "#FEEAA7";
        CustomPopupBackgroundColor = "#1B001F";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void SetPresetMidnight()
    {
        CustomAccentColor = "#4A90E2";
        CustomButtonColor = "#1B2A47";
        CustomTabSelectedColor = "#204A87";
        CustomTextColor = "#F0F4F8";
        CustomPopupBackgroundColor = "#0D1929";
        CustomFontSize = 15.0f;
        ApplyLivePreview();
    }

    public void LoadNeonTemplate()
    {
        CustomUserCode = @"Accent = #00F2FE
Button = #2D1B4E
TabSelected = #FF007F
TextColor = #E0F7FA
PopupBg = #1A102F
Opacity = 0.70
FontSize = 15
Title = Space Station 14 • Cyberpunk Edition
Servers = Server Stations
Replays = Round Archive
";
    }

    public void LoadMatrixTemplate()
    {
        CustomUserCode = @"Accent = #00FF66
Button = #0D2818
TabSelected = #00B33C
TextColor = #D8F3DC
PopupBg = #081C10
Opacity = 0.80
FontSize = 15
Title = Space Station 14 • Terminal
Home = Station
Servers = Server Matrix
";
    }

    public void LoadMinimalistTemplate()
    {
        CustomUserCode = @"Accent = #FFFFFF
Button = #2A2A2E
TabSelected = #44444C
TextColor = #FFFFFF
PopupBg = #1C1C20
Opacity = 0.90
FontSize = 14
";
    }

    public void GenerateRandomPalette()
    {
        var rng = new Random();
        // Generate random harmonious sci-fi palette
        var hue = rng.NextDouble();
        var sat = 0.70 + rng.NextDouble() * 0.25;
        var val = 0.85 + rng.NextDouble() * 0.15;

        var accentRgb = HsvToRgb(hue, sat, val);
        var buttonHue = (hue + 0.5) % 1.0;
        var buttonRgb = HsvToRgb(buttonHue, 0.35, 0.22);
        var tabHue = (hue + 0.08) % 1.0;
        var tabRgb = HsvToRgb(tabHue, 0.75, 0.70);

        CustomAccentColor = $"#{accentRgb.R:X2}{accentRgb.G:X2}{accentRgb.B:X2}";
        CustomButtonColor = $"#{buttonRgb.R:X2}{buttonRgb.G:X2}{buttonRgb.B:X2}";
        CustomTabSelectedColor = $"#{tabRgb.R:X2}{tabRgb.G:X2}{tabRgb.B:X2}";
        CustomTextColor = "#F5F6FA";
        CustomPopupBackgroundColor = "#101015";
        CustomBackgroundOpacity = 0.88f;

        CustomUserCode = $@"# Random Palette Generated
Accent = {CustomAccentColor}
Button = {CustomButtonColor}
TabSelected = {CustomTabSelectedColor}
TextColor = {CustomTextColor}
PopupBg = {CustomPopupBackgroundColor}
Opacity = 0.88
";
        ApplyLivePreview();
        ScriptOutputText = LocalizationManager.Instance.GetString("customizer-script-palette-generated");
    }

    private static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
    {
        double r = 0, g = 0, b = 0;
        int i = (int)Math.Floor(h * 6);
        double f = h * 6 - i;
        double p = v * (1 - s);
        double q = v * (1 - f * s);
        double t = v * (1 - (1 - f) * s);
        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }
        return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    public void ClearScript()
    {
        CustomUserCode = "";
        ScriptOutputText = LocalizationManager.Instance.GetString("customizer-script-cleared");
    }

    public void ExecuteUserCode()
    {
        if (string.IsNullOrWhiteSpace(_customUserCode))
        {
            ScriptOutputText = LocalizationManager.Instance.GetString("customizer-script-empty");
            return;
        }

        var lines = _customUserCode.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var appliedCount = 0;
        var errorCount = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith("//") || line.StartsWith("#") || string.IsNullOrEmpty(line))
                continue;

            // Check for standalone command keywords
            var lowerLine = line.ToLowerInvariant();
            if (lowerLine is "randomize" or "random" or "random palette")
            {
                GenerateRandomPalette();
                appliedCount++;
                continue;
            }
            if (lowerLine.StartsWith("preset ") || lowerLine.StartsWith("load preset "))
            {
                var presetName = lowerLine.Replace("load preset ", "").Replace("preset ", "").Trim();
                switch (presetName)
                {
                    case "classic" or "default": SetPresetClassic(); appliedCount++; break;
                    case "cyberpunk": SetPresetCyberpunk(); appliedCount++; break;
                    case "syndicate": SetPresetSyndicate(); appliedCount++; break;
                    case "centcomm": SetPresetCentComm(); appliedCount++; break;
                    case "plasma": SetPresetPlasma(); appliedCount++; break;
                    case "singularity": SetPresetSingularity(); appliedCount++; break;
                    case "sec": SetPresetSec(); appliedCount++; break;
                    case "med": SetPresetMed(); appliedCount++; break;
                    case "solar": SetPresetSolar(); appliedCount++; break;
                    case "deep space" or "deepspace": SetPresetDeepSpace(); appliedCount++; break;
                    case "matrix": SetPresetMatrix(); appliedCount++; break;
                    case "cybergoth": SetPresetCyberGoth(); appliedCount++; break;
                    case "monochrome": SetPresetMonochrome(); appliedCount++; break;
                    case "synthwave": SetPresetSynthwave(); appliedCount++; break;
                    case "midnight": SetPresetMidnight(); appliedCount++; break;
                    case "clown": SetPresetClown(); appliedCount++; break;
                    default: errorCount++; break;
                }
                continue;
            }
            if (lowerLine is "reset" or "reset all")
            {
                Reset();
                appliedCount++;
                continue;
            }

            // Remove optional 'set ' prefix
            if (line.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
                line = line.Substring(4).Trim();

            var parts = line.Split(['=', ':'], 2);
            if (parts.Length != 2)
            {
                errorCount++;
                continue;
            }

            var key = parts[0].Trim().ToLowerInvariant();
            var val = parts[1].Trim();

            switch (key)
            {
                case "accent" or "accentcolor" or "gold":
                    CustomAccentColor = val;
                    appliedCount++;
                    break;
                case "button" or "buttoncolor":
                    CustomButtonColor = val;
                    appliedCount++;
                    break;
                case "tabselected" or "tabcolor":
                    CustomTabSelectedColor = val;
                    appliedCount++;
                    break;
                case "textcolor" or "text":
                    CustomTextColor = val;
                    appliedCount++;
                    break;
                case "popupbg" or "popup" or "cardbg":
                    CustomPopupBackgroundColor = val;
                    appliedCount++;
                    break;
                case "opacity":
                    if (float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var op))
                    {
                        CustomBackgroundOpacity = Math.Clamp(op, 0.1f, 1.0f);
                        appliedCount++;
                    }
                    break;
                case "fontsize" or "font":
                    if (float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fs))
                    {
                        CustomFontSize = Math.Clamp(fs, 10.0f, 24.0f);
                        appliedCount++;
                    }
                    break;
                case "title" or "windowtitle":
                    CustomWindowTitle = val;
                    appliedCount++;
                    break;
                case "hometab" or "home":
                    CustomHomeTabName = val;
                    appliedCount++;
                    break;
                case "serverstab" or "servers":
                    CustomServersTabName = val;
                    appliedCount++;
                    break;
                case "newstab" or "news":
                    CustomNewsTabName = val;
                    appliedCount++;
                    break;
                case "replaystab" or "replays":
                    CustomReplaysTabName = val;
                    appliedCount++;
                    break;
                case "optionstab" or "options":
                    CustomOptionsTabName = val;
                    appliedCount++;
                    break;
                case "tabplacement" or "dock" or "placement":
                    if (val.Equals("Bottom", StringComparison.OrdinalIgnoreCase) ||
                        val.Equals("Left", StringComparison.OrdinalIgnoreCase) ||
                        val.Equals("Right", StringComparison.OrdinalIgnoreCase) ||
                        val.Equals("Top", StringComparison.OrdinalIgnoreCase))
                    {
                        CustomTabPlacement = char.ToUpperInvariant(val[0]) + val.Substring(1).ToLowerInvariant();
                        appliedCount++;
                    }
                    break;
                case "bgimage" or "background":
                    CustomBackgroundImagePath = val;
                    appliedCount++;
                    break;
                case "logoimage" or "logo":
                    CustomLogoImagePath = val;
                    appliedCount++;
                    break;
                case "vfx":
                    EnableClickVfx = !val.Equals("false", StringComparison.OrdinalIgnoreCase) && !val.Equals("off", StringComparison.OrdinalIgnoreCase) && !val.Equals("0", StringComparison.OrdinalIgnoreCase);
                    appliedCount++;
                    break;
                case "clear":
                    if (val.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        Reset();
                        appliedCount++;
                    }
                    else if (val.Equals("bg", StringComparison.OrdinalIgnoreCase))
                    {
                        ClearBackground();
                        appliedCount++;
                    }
                    else if (val.Equals("logo", StringComparison.OrdinalIgnoreCase))
                    {
                        ClearLogo();
                        appliedCount++;
                    }
                    break;
                default:
                    errorCount++;
                    break;
            }
        }

        ApplyLivePreview();

        var errorsSuffix = errorCount > 0
            ? LocalizationManager.Instance.GetString("customizer-script-errors-suffix", ("skipped", errorCount.ToString()))
            : "";
        ScriptOutputText = LocalizationManager.Instance.GetString("customizer-script-done", ("applied", appliedCount.ToString()), ("errors", errorsSuffix));
    }

    public void Populate()
    {
        CustomBackgroundImagePath = _dataManager.GetCVar(CVars.CustomBackgroundImagePath);
        CustomBackgroundOpacity = _dataManager.GetCVar(CVars.CustomBackgroundOpacity);
        CustomLogoImagePath = _dataManager.GetCVar(CVars.CustomLogoImagePath);
        CustomAccentColor = _dataManager.GetCVar(CVars.CustomAccentColor);
        CustomButtonColor = _dataManager.GetCVar(CVars.CustomButtonColor);
        CustomTabSelectedColor = _dataManager.GetCVar(CVars.CustomTabSelectedColor);
        CustomTextColor = _dataManager.GetCVar(CVars.CustomTextColor);
        CustomPopupBackgroundColor = _dataManager.GetCVar(CVars.CustomPopupBackgroundColor);
        CustomFontSize = _dataManager.GetCVar(CVars.CustomFontSize);
        CustomWindowTitle = _dataManager.GetCVar(CVars.CustomWindowTitle);
        EnableClickVfx = _dataManager.GetCVar(CVars.EnableClickVfx);
        CustomUserCode = _dataManager.GetCVar(CVars.CustomUserCode);

        CustomHomeTabName = _dataManager.GetCVar(CVars.CustomHomeTabName);
        CustomServersTabName = _dataManager.GetCVar(CVars.CustomServersTabName);
        CustomNewsTabName = _dataManager.GetCVar(CVars.CustomNewsTabName);
        CustomReplaysTabName = _dataManager.GetCVar(CVars.CustomReplaysTabName);
        CustomOptionsTabName = _dataManager.GetCVar(CVars.CustomOptionsTabName);
        CustomTabPlacement = _dataManager.GetCVar(CVars.CustomTabPlacement);

        UpdateBackgroundPreview();
        UpdateLogoPreview();
        OnPropertyChanged(nameof(OpacityPercentageText));
        OnPropertyChanged(nameof(FontSizeText));

        _initialSnapshot = new CustomizerSnapshot(
            CustomBackgroundImagePath,
            CustomBackgroundOpacity,
            CustomLogoImagePath,
            CustomAccentColor,
            CustomButtonColor,
            CustomTabSelectedColor,
            CustomTextColor,
            CustomPopupBackgroundColor,
            CustomFontSize,
            CustomWindowTitle,
            EnableClickVfx,
            CustomUserCode,
            CustomHomeTabName,
            CustomServersTabName,
            CustomNewsTabName,
            CustomReplaysTabName,
            CustomOptionsTabName,
            CustomTabPlacement
        );

        ApplyLivePreview();

        ScriptOutputText = LocalizationManager.Instance.GetString("customizer-script-ready");
    }

    public void Save()
    {
        _dataManager.SetCVar(CVars.CustomBackgroundImagePath, CustomBackgroundImagePath);
        _dataManager.SetCVar(CVars.CustomBackgroundOpacity, CustomBackgroundOpacity);
        _dataManager.SetCVar(CVars.CustomLogoImagePath, CustomLogoImagePath);
        _dataManager.SetCVar(CVars.CustomAccentColor, CustomAccentColor);
        _dataManager.SetCVar(CVars.CustomButtonColor, CustomButtonColor);
        _dataManager.SetCVar(CVars.CustomTabSelectedColor, CustomTabSelectedColor);
        _dataManager.SetCVar(CVars.CustomTextColor, CustomTextColor);
        _dataManager.SetCVar(CVars.CustomPopupBackgroundColor, CustomPopupBackgroundColor);
        _dataManager.SetCVar(CVars.CustomFontSize, CustomFontSize);
        _dataManager.SetCVar(CVars.CustomWindowTitle, CustomWindowTitle);
        _dataManager.SetCVar(CVars.EnableClickVfx, EnableClickVfx);
        _dataManager.SetCVar(CVars.CustomUserCode, CustomUserCode);

        _dataManager.SetCVar(CVars.CustomHomeTabName, CustomHomeTabName);
        _dataManager.SetCVar(CVars.CustomServersTabName, CustomServersTabName);
        _dataManager.SetCVar(CVars.CustomNewsTabName, CustomNewsTabName);
        _dataManager.SetCVar(CVars.CustomReplaysTabName, CustomReplaysTabName);
        _dataManager.SetCVar(CVars.CustomOptionsTabName, CustomOptionsTabName);
        _dataManager.SetCVar(CVars.CustomTabPlacement, CustomTabPlacement);
        _dataManager.CommitConfig();
    }

    public void Reset()
    {
        CustomBackgroundImagePath = "";
        CustomBackgroundOpacity = 0.85f;
        CustomLogoImagePath = "";
        CustomAccentColor = "";
        CustomButtonColor = "";
        CustomTabSelectedColor = "";
        CustomTextColor = "";
        CustomPopupBackgroundColor = "";
        CustomFontSize = 15.0f;
        CustomWindowTitle = "";
        EnableClickVfx = true;
        CustomUserCode = "";

        CustomHomeTabName = "";
        CustomServersTabName = "";
        CustomNewsTabName = "";
        CustomReplaysTabName = "";
        CustomOptionsTabName = "";
        CustomTabPlacement = "Top";

        UpdateBackgroundPreview();
        UpdateLogoPreview();
        OnPropertyChanged(nameof(OpacityPercentageText));
        OnPropertyChanged(nameof(FontSizeText));
        ScriptOutputText = LocalizationManager.Instance.GetString("customizer-script-reset");
    }

    private string _exportThemeButtonText = "";
    public string ExportThemeButtonText
    {
        get => string.IsNullOrEmpty(_exportThemeButtonText) ? LocalizationManager.Instance.GetString("launcher-customizer-export") : _exportThemeButtonText;
        set => SetProperty(ref _exportThemeButtonText, value);
    }

    public async void ExportThemeToClipboard()
    {
        try
        {
            var theme = new
            {
                accent = CustomAccentColor,
                button = CustomButtonColor,
                tabSelected = CustomTabSelectedColor,
                text = CustomTextColor,
                popup = CustomPopupBackgroundColor,
                opacity = CustomBackgroundOpacity,
                fontSize = CustomFontSize,
                tabPlacement = CustomTabPlacement,
                vfx = EnableClickVfx
            };
            var json = System.Text.Json.JsonSerializer.Serialize(theme);
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                var top = desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
                if (top?.Clipboard != null)
                {
                    await top.Clipboard.SetTextAsync(json);
                    ScriptOutputText = LocalizationManager.Instance.GetString("customizer-export-success");
                    ExportThemeButtonText = LocalizationManager.Instance.GetString("account-info-copied");
                    var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                    timer.Tick += (_, _) => { timer.Stop(); ExportThemeButtonText = ""; };
                    timer.Start();
                }
            }
        }
        catch (Exception ex)
        {
            ScriptOutputText = $"Export error: {ex.Message}";
        }
    }

    public async void ImportThemeFromClipboard()
    {
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow?.Clipboard is { } clipboard)
            {
                var text = await clipboard.GetTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(text);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("accent", out var a)) CustomAccentColor = a.GetString() ?? "";
                    if (root.TryGetProperty("button", out var b)) CustomButtonColor = b.GetString() ?? "";
                    if (root.TryGetProperty("tabSelected", out var ts)) CustomTabSelectedColor = ts.GetString() ?? "";
                    if (root.TryGetProperty("text", out var t)) CustomTextColor = t.GetString() ?? "";
                    if (root.TryGetProperty("popup", out var p)) CustomPopupBackgroundColor = p.GetString() ?? "";
                    if (root.TryGetProperty("opacity", out var op)) CustomBackgroundOpacity = (float)op.GetDouble();
                    if (root.TryGetProperty("fontSize", out var fs)) CustomFontSize = (float)fs.GetDouble();
                    if (root.TryGetProperty("tabPlacement", out var tp)) CustomTabPlacement = tp.GetString() ?? "Top";
                    if (root.TryGetProperty("vfx", out var vfx)) EnableClickVfx = vfx.GetBoolean();

                    ScriptOutputText = LocalizationManager.Instance.GetString("customizer-import-success");
                }
            }
        }
        catch (Exception ex)
        {
            ScriptOutputText = $"Import error: {ex.Message}";
        }
    }

    public void ApplyPresetCyberpunk()
    {
        CustomAccentColor = "#00FFCC";
        CustomButtonColor = "#1A1D2E";
        CustomTabSelectedColor = "#FF0055";
        CustomTextColor = "#FFFFFF";
        CustomPopupBackgroundColor = "#0E1017";
        CustomBackgroundOpacity = 0.90f;
        CustomFontSize = 15.0f;
        EnableClickVfx = true;
        ScriptOutputText = "Preset: Cyberpunk Neon";
        ApplyLivePreview();
    }

    public void ApplyPresetRetro()
    {
        CustomAccentColor = "#00FF66";
        CustomButtonColor = "#112211";
        CustomTabSelectedColor = "#224422";
        CustomTextColor = "#33FF33";
        CustomPopupBackgroundColor = "#051005";
        CustomBackgroundOpacity = 0.95f;
        CustomFontSize = 15.0f;
        EnableClickVfx = true;
        ScriptOutputText = "Preset: Retro Terminal";
        ApplyLivePreview();
    }

    public void ApplyPresetDeepSpace()
    {
        CustomAccentColor = "#6C5CE7";
        CustomButtonColor = "#2D3436";
        CustomTabSelectedColor = "#A29BFE";
        CustomTextColor = "#DFE6E9";
        CustomPopupBackgroundColor = "#1E1E24";
        CustomBackgroundOpacity = 0.85f;
        CustomFontSize = 15.0f;
        EnableClickVfx = true;
        ScriptOutputText = "Preset: Deep Space";
        ApplyLivePreview();
    }

    public void ApplyPresetSynthwave()
    {
        CustomAccentColor = "#FF7675";
        CustomButtonColor = "#2D132C";
        CustomTabSelectedColor = "#E84393";
        CustomTextColor = "#FEEAA7";
        CustomPopupBackgroundColor = "#1B001F";
        CustomBackgroundOpacity = 0.88f;
        CustomFontSize = 15.0f;
        EnableClickVfx = true;
        ScriptOutputText = "Preset: Synthwave Sunset";
        ApplyLivePreview();
    }

    public void ApplyPresetMidnight()
    {
        CustomAccentColor = "#4A90E2";
        CustomButtonColor = "#1B2A47";
        CustomTabSelectedColor = "#204A87";
        CustomTextColor = "#F0F4F8";
        CustomPopupBackgroundColor = "#0D1929";
        CustomBackgroundOpacity = 0.90f;
        CustomFontSize = 15.0f;
        EnableClickVfx = true;
        ScriptOutputText = "Preset: Midnight Blue";
        ApplyLivePreview();
    }
}
