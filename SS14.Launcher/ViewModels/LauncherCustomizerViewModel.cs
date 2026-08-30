using System;
using System.IO;
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

    private Avalonia.Threading.DispatcherTimer? _livePreviewTimer;

    private void ScheduleLivePreview()
    {
        if (!_livePreviewEnabled) return;

        _livePreviewTimer?.Stop();
        _livePreviewTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _livePreviewTimer.Tick += (_, _) =>
        {
            _livePreviewTimer.Stop();
            ApplyLivePreview();
        };
        _livePreviewTimer.Start();
    }

    private void ApplyLivePreview()
    {
        var app = Avalonia.Application.Current;
        if (app == null) return;

        TrySetResourceColor(app, "SS14AccentBrush", _customAccentColor);
        TrySetResourceColor(app, "SS14ButtonBrush", _customButtonColor);
        TrySetResourceColor(app, "SS14TabSelectedBrush", _customTabSelectedColor);
        TrySetResourceColor(app, "SS14TextBrush", _customTextColor);
        TrySetResourceColor(app, "SS14PopupBackgroundBrush", _customPopupBackgroundColor);
    }

    private static void TrySetResourceColor(Avalonia.Application app, string key, string colorHex)
    {
        if (string.IsNullOrWhiteSpace(colorHex)) return;
        try
        {
            if (Avalonia.Media.Color.TryParse(colorHex, out var color))
            {
                app.Resources[key] = new Avalonia.Media.SolidColorBrush(color);
            }
        }
        catch
        {
            // Ignore invalid color strings during live preview
        }
    }

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
            }
        }
    }

    public string FontSizeText => $"{(int)_customFontSize} pt";

    public string CustomWindowTitle
    {
        get => _customWindowTitle;
        set => SetProperty(ref _customWindowTitle, value);
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
    }

    public void SetPresetCyberpunk()
    {
        CustomAccentColor = "#00F2FE";
        CustomButtonColor = "#2D1B4E";
        CustomTabSelectedColor = "#FF007F";
        CustomTextColor = "#E0F7FA";
        CustomPopupBackgroundColor = "#1A102F";
        CustomFontSize = 15.0f;
    }

    public void SetPresetSyndicate()
    {
        CustomAccentColor = "#E50914";
        CustomButtonColor = "#26262B";
        CustomTabSelectedColor = "#990000";
        CustomTextColor = "#F5F5F5";
        CustomPopupBackgroundColor = "#1A1A1E";
        CustomFontSize = 15.0f;
    }

    public void SetPresetSolar()
    {
        CustomAccentColor = "#FFA000";
        CustomButtonColor = "#1E3C72";
        CustomTabSelectedColor = "#FF8C00";
        CustomTextColor = "#FFF8E1";
        CustomPopupBackgroundColor = "#121E36";
        CustomFontSize = 15.0f;
    }

    public void SetPresetDeepSpace()
    {
        CustomAccentColor = "#A29BFE";
        CustomButtonColor = "#2C2C54";
        CustomTabSelectedColor = "#6C5CE7";
        CustomTextColor = "#F1F2F6";
        CustomPopupBackgroundColor = "#191933";
        CustomFontSize = 15.0f;
    }

    public void SetPresetMatrix()
    {
        CustomAccentColor = "#00FF66";
        CustomButtonColor = "#0D2818";
        CustomTabSelectedColor = "#00B33C";
        CustomTextColor = "#D8F3DC";
        CustomPopupBackgroundColor = "#081C10";
        CustomFontSize = 15.0f;
    }

    public void SetPresetMonochrome()
    {
        CustomAccentColor = "#FFFFFF";
        CustomButtonColor = "#333333";
        CustomTabSelectedColor = "#555555";
        CustomTextColor = "#F0F0F0";
        CustomPopupBackgroundColor = "#181818";
        CustomFontSize = 15.0f;
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
                case "bgimage" or "background":
                    CustomBackgroundImagePath = val;
                    appliedCount++;
                    break;
                case "logoimage" or "logo":
                    CustomLogoImagePath = val;
                    appliedCount++;
                    break;
                case "vfx":
                    EnableClickVfx = !val.Equals("false", StringComparison.OrdinalIgnoreCase);
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
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(json);
                ScriptOutputText = LocalizationManager.Instance.GetString("customizer-export-success");
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
    }
}
