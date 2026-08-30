using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DynamicData;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Serilog;
using Splat;
using SS14.Launcher.Api;
using SS14.Launcher.Localization;
using SS14.Launcher.Models;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.Logins;
using SS14.Launcher.Utility;
using SS14.Launcher.ViewModels.Login;
using SS14.Launcher.ViewModels.MainWindowTabs;
using SS14.Launcher.Views;

namespace SS14.Launcher.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase, IErrorOverlayOwner
{
    private readonly DataManager _cfg;
    private readonly LoginManager _loginMgr;
    private readonly LauncherInfoManager _infoManager;
    private readonly LocalizationManager _loc;

    private int _selectedIndex;

    public DataManager Cfg => _cfg;
    [ObservableProperty] private bool _outOfDate;

    private IDisposable? _authOverrideCountdownTimer;

    public HomePageViewModel HomeTab { get; }
    public ServerListTabViewModel ServersTab { get; }
    public NewsTabViewModel NewsTab { get; }
    public ReplaysTabViewModel ReplaysTab { get; }
    public OptionsTabViewModel OptionsTab { get; }
    public DevelopmentTabViewModel DevelopmentTab { get; }

    public Dock CustomTabStripPlacement
    {
        get
        {
            var p = _cfg.GetCVar(CVars.CustomTabPlacement);
            return p.ToLowerInvariant() switch
            {
                "bottom" => Dock.Bottom,
                "left" => Dock.Left,
                "right" => Dock.Right,
                _ => Dock.Top
            };
        }
    }

    public MainWindowViewModel()
    {
        _cfg = Locator.Current.GetRequiredService<DataManager>();
        _loginMgr = Locator.Current.GetRequiredService<LoginManager>();
        _infoManager = Locator.Current.GetRequiredService<LauncherInfoManager>();
        _loc = LocalizationManager.Instance;

        ServersTab = new ServerListTabViewModel(this);
        HomeTab = new HomePageViewModel(this);
        NewsTab = new NewsTabViewModel();
        ReplaysTab = new ReplaysTabViewModel(this);
        OptionsTab = new OptionsTabViewModel();
        DevelopmentTab = new DevelopmentTabViewModel();

        Tabs = new System.Collections.ObjectModel.ObservableCollection<MainWindowTabViewModel>
        {
            HomeTab,
            ServersTab
        };

        if (_cfg.GetCVar(CVars.ShowNewsTab))
        {
            Tabs.Add(NewsTab);
        }

        if (_cfg.GetCVar(CVars.ShowReplaysTab))
        {
            Tabs.Add(ReplaysTab);
        }

        Tabs.Add(OptionsTab);

        if (_cfg.GetCVar(CVars.ShowDevelopmentTab))
        {
            Tabs.Add(DevelopmentTab);
        }

        _cfg.GetCVarEntry(CVars.CustomTabPlacement).PropertyChanged += (_, _) =>
        {
            Dispatcher.UIThread.Post(() => OnPropertyChanged(nameof(CustomTabStripPlacement)));
        };

        _cfg.GetCVarEntry(CVars.ShowNewsTab).PropertyChanged += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var shouldShow = _cfg.GetCVar(CVars.ShowNewsTab);
                var currentSelectedTab = (SelectedIndex >= 0 && SelectedIndex < Tabs.Count) ? Tabs[SelectedIndex] : null;

                if (shouldShow && !Tabs.Contains(NewsTab))
                {
                    var replaysIdx = Tabs.IndexOf(ReplaysTab);
                    if (replaysIdx >= 0)
                    {
                        Tabs.Insert(replaysIdx, NewsTab);
                    }
                    else
                    {
                        var optionsIdx = Tabs.IndexOf(OptionsTab);
                        if (optionsIdx >= 0)
                            Tabs.Insert(optionsIdx, NewsTab);
                        else
                            Tabs.Add(NewsTab);
                    }
                }
                else if (!shouldShow && Tabs.Contains(NewsTab))
                {
                    if (NewsTab.IsSelected)
                    {
                        NewsTab.IsSelected = false;
                        NewsTab.Unselected();
                    }
                    Tabs.Remove(NewsTab);
                }

                if (currentSelectedTab != null && Tabs.Contains(currentSelectedTab))
                    SelectedIndex = Tabs.IndexOf(currentSelectedTab);
                else
                    SelectedIndex = 0;
            });
        };

        _cfg.GetCVarEntry(CVars.ShowReplaysTab).PropertyChanged += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var shouldShow = _cfg.GetCVar(CVars.ShowReplaysTab);
                var currentSelectedTab = (SelectedIndex >= 0 && SelectedIndex < Tabs.Count) ? Tabs[SelectedIndex] : null;

                if (shouldShow && !Tabs.Contains(ReplaysTab))
                {
                    var optionsIdx = Tabs.IndexOf(OptionsTab);
                    if (optionsIdx >= 0)
                        Tabs.Insert(optionsIdx, ReplaysTab);
                    else
                        Tabs.Add(ReplaysTab);
                }
                else if (!shouldShow && Tabs.Contains(ReplaysTab))
                {
                    if (ReplaysTab.IsSelected)
                    {
                        ReplaysTab.IsSelected = false;
                        ReplaysTab.Unselected();
                    }
                    Tabs.Remove(ReplaysTab);
                }

                if (currentSelectedTab != null && Tabs.Contains(currentSelectedTab))
                    SelectedIndex = Tabs.IndexOf(currentSelectedTab);
                else
                    SelectedIndex = 0;
            });
        };

        _cfg.GetCVarEntry(CVars.ShowDevelopmentTab).PropertyChanged += (_, _) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var shouldShow = _cfg.GetCVar(CVars.ShowDevelopmentTab);
                var currentSelectedTab = (SelectedIndex >= 0 && SelectedIndex < Tabs.Count) ? Tabs[SelectedIndex] : null;

                if (shouldShow && !Tabs.Contains(DevelopmentTab))
                {
                    Tabs.Add(DevelopmentTab);
                }
                else if (!shouldShow && Tabs.Contains(DevelopmentTab))
                {
                    if (DevelopmentTab.IsSelected)
                    {
                        DevelopmentTab.IsSelected = false;
                        DevelopmentTab.Unselected();
                    }
                    Tabs.Remove(DevelopmentTab);
                }

                if (currentSelectedTab != null && Tabs.Contains(currentSelectedTab))
                    SelectedIndex = Tabs.IndexOf(currentSelectedTab);
                else
                    SelectedIndex = 0;
            });
        };

        _loc.LanguageSwitched += () =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (Tabs != null)
                {
                    foreach (var tab in Tabs)
                    {
                        tab.NotifyNameChanged();
                    }
                }
            });
        };

        _cfg.GetCVarEntry(CVars.CustomHomeTabName).PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => HomeTab.NotifyNameChanged());
        _cfg.GetCVarEntry(CVars.CustomServersTabName).PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => ServersTab.NotifyNameChanged());
        _cfg.GetCVarEntry(CVars.CustomNewsTabName).PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => NewsTab.NotifyNameChanged());
        _cfg.GetCVarEntry(CVars.CustomReplaysTabName).PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => ReplaysTab.NotifyNameChanged());
        _cfg.GetCVarEntry(CVars.CustomOptionsTabName).PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => OptionsTab.NotifyNameChanged());
        _cfg.GetCVarEntry(CVars.CustomDevelopmentTabName).PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => DevelopmentTab.NotifyNameChanged());

        AccountDropDown = new AccountDropDownViewModel(this);
        LoginViewModel = new MainWindowLoginViewModel();

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(LoggedIn) && LoggedIn)
                RunSelectedOnTab();
        };

        _loginMgr.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(_loginMgr.ActiveAccount))
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(LoggedIn)));
        };

        _cfg.Logins.Connect()
            .Subscribe(_ => OnPropertyChanged(new PropertyChangedEventArgs(nameof(AccountDropDownVisible))));

        if (_cfg.GetCVar(CVars.SmartCacheCleaner))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var cm = Locator.Current.GetService<Models.ContentManagement.ContentManager>();
                    if (cm != null)
                        await cm.RunSmartCleanerAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Background startup SmartCacheCleaner failed");
                }
            });
        }

        _bgManager.FrameUpdated += frame =>
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _customBackgroundImage = frame;
                OnPropertyChanged(nameof(CustomBackgroundImage));
                OnPropertyChanged(nameof(HasCustomBackgroundImage));
            });
        };

        ReloadCustomVisuals();
    }

    public MainWindow? Control { get; set; }

    public System.Collections.ObjectModel.ObservableCollection<MainWindowTabViewModel> Tabs { get; }

    public bool LoggedIn => _loginMgr.ActiveAccount != null;
    public bool AccountDropDownVisible => _loginMgr.Logins.Count != 0;

    public AccountDropDownViewModel AccountDropDown { get; }

    public MainWindowLoginViewModel LoginViewModel { get; }

    private readonly AnimatedBackgroundManager _bgManager = new();
    private Avalonia.Media.Imaging.Bitmap? _customBackgroundImage;
    private Avalonia.Media.Imaging.Bitmap? _customLogoImage;

    public Avalonia.Media.Imaging.Bitmap? CustomBackgroundImage => _customBackgroundImage;
    public bool HasCustomBackgroundImage => _customBackgroundImage != null;
    public double CustomBackgroundOverlayOpacity => _cfg.GetCVar(CVars.CustomBackgroundOpacity);
    public Avalonia.Media.Imaging.Bitmap? CustomLogoImage => _customLogoImage;

    public void ReloadCustomVisuals()
    {
        var bgPath = _cfg.GetCVar(CVars.CustomBackgroundImagePath);
        _bgManager.Load(bgPath);
        _customBackgroundImage = _bgManager.CurrentFrame;
        OnPropertyChanged(nameof(CustomBackgroundImage));
        OnPropertyChanged(nameof(HasCustomBackgroundImage));
        OnPropertyChanged(nameof(CustomBackgroundOverlayOpacity));

        var logoPath = _cfg.GetCVar(CVars.CustomLogoImagePath);
        if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
        {
            try
            {
                _customLogoImage = new Avalonia.Media.Imaging.Bitmap(logoPath);
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to load custom logo: {Message}", ex.Message);
                _customLogoImage = null;
            }
        }
        else
        {
            _customLogoImage = null;
        }

        if (Avalonia.Application.Current?.Resources is { } res)
        {
            var accent = _cfg.GetCVar(CVars.CustomAccentColor);
            if (!string.IsNullOrWhiteSpace(accent) && PaletteUtility.TryParseHexColor(accent, out var accentCol))
            {
                res["ThemeNanoGoldBrush"] = new Avalonia.Media.SolidColorBrush(accentCol);
                res["ThemeNanoGoldColor"] = accentCol;
            }

            var btnColHex = _cfg.GetCVar(CVars.CustomButtonColor);
            if (!string.IsNullOrWhiteSpace(btnColHex) && PaletteUtility.TryParseHexColor(btnColHex, out var btnCol))
            {
                res["ThemeControlMidBrush"] = new Avalonia.Media.SolidColorBrush(btnCol);
                res["ThemeControlMidColor"] = btnCol;
            }

            var tabColHex = _cfg.GetCVar(CVars.CustomTabSelectedColor);
            if (!string.IsNullOrWhiteSpace(tabColHex) && PaletteUtility.TryParseHexColor(tabColHex, out var tabCol))
            {
                res["ThemeTabItemSelectedBrush"] = new Avalonia.Media.SolidColorBrush(tabCol);
                res["ThemeControlHighBrush"] = new Avalonia.Media.SolidColorBrush(tabCol);
                res["ThemeControlHighColor"] = tabCol;
            }

            var textColHex = _cfg.GetCVar(CVars.CustomTextColor);
            if (!string.IsNullOrWhiteSpace(textColHex) && PaletteUtility.TryParseHexColor(textColHex, out var textCol))
            {
                res["ThemeForegroundBrush"] = new Avalonia.Media.SolidColorBrush(textCol);
                res["ThemeForegroundColor"] = textCol;
            }

            var popupBgHex = _cfg.GetCVar(CVars.CustomPopupBackgroundColor);
            if (!string.IsNullOrWhiteSpace(popupBgHex) && PaletteUtility.TryParseHexColor(popupBgHex, out var popupCol))
            {
                res["ThemePopupBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(popupCol);
                res["ThemePopupBackgroundColor"] = popupCol;
            }

            var fontSize = _cfg.GetCVar(CVars.CustomFontSize);
            if (fontSize >= 10 && fontSize <= 26)
            {
                res["FontSizeNormal"] = (double)fontSize;
            }

            if (HasCustomBackgroundImage)
            {
                res["ThemeBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x25, 0x25, 0x2A));
                res["ThemeServerListBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(0x28, 0x10, 0x10, 0x18));
                res["ThemeServerListRowAltBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));
                res["ThemeHeaderBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(0x45, 0x15, 0x15, 0x1C));
                res["ThemeStripeBackBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(0x35, 0x10, 0x10, 0x18));
            }
            else
            {
                res["ThemeBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x25, 0x25, 0x2A));
                res["ThemeServerListBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x1e, 0x1e, 0x22));
                res["ThemeServerListRowAltBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x26, 0x26, 0x26));
                res["ThemeHeaderBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x21, 0x21, 0x26));

                var vb = new Avalonia.Media.VisualBrush
                {
                    TileMode = Avalonia.Media.TileMode.Tile,
                    Stretch = Avalonia.Media.Stretch.Fill,
                    SourceRect = new Avalonia.RelativeRect(0, 0, 32, 32, Avalonia.RelativeUnit.Absolute),
                    DestinationRect = new Avalonia.RelativeRect(0, 0, 32, 32, Avalonia.RelativeUnit.Absolute),
                    Visual = new Avalonia.Controls.Panel
                    {
                        Height = 32,
                        Width = 32,
                        Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x1e, 0x1e, 0x22)),
                        Children =
                        {
                            new Avalonia.Controls.Shapes.Path { Data = Avalonia.Media.Geometry.Parse("M 0 8 L 24 32 L 8 32 L 0 24 Z"), Fill = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x26, 0x26, 0x26)) },
                            new Avalonia.Controls.Shapes.Path { Data = Avalonia.Media.Geometry.Parse("M 8 0 L 24 0 L 32 8 L 32 24 Z"), Fill = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0x26, 0x26, 0x26)) }
                        }
                    }
                };
                res["ThemeStripeBackBrush"] = vb;
            }
        }

        if (Control != null)
        {
            var title = _cfg.GetCVar(CVars.CustomWindowTitle);
            if (!string.IsNullOrWhiteSpace(title))
            {
                Control.Title = title;
            }
        }

        OnPropertyChanged(nameof(CustomBackgroundImage));
        OnPropertyChanged(nameof(HasCustomBackgroundImage));
        OnPropertyChanged(nameof(CustomBackgroundOverlayOpacity));
        OnPropertyChanged(nameof(CustomLogoImage));

        if (Tabs != null)
        {
            foreach (var tab in Tabs)
            {
                tab.NotifyNameChanged();
            }
        }
    }

    [ObservableProperty] private ConnectingViewModel? _connectingVM;

    [ObservableProperty] private string? _busyTask;
    [ObservableProperty] private ViewModelBase? _overlayViewModel;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex >= 0 && _selectedIndex < Tabs.Count)
            {
                var previous = Tabs[_selectedIndex];
                previous.IsSelected = false;
                previous.Unselected();
            }

            var clamped = Math.Clamp(value, 0, Math.Max(0, Tabs.Count - 1));

            if (!EqualityComparer<int>.Default.Equals(_selectedIndex, clamped))
            {
                OnPropertyChanging();
                _selectedIndex = clamped;
                OnPropertyChanged();
            }

            RunSelectedOnTab();
        }
    }

    private void RunSelectedOnTab()
    {
        if (_selectedIndex >= 0 && _selectedIndex < Tabs.Count)
        {
            var tab = Tabs[_selectedIndex];
            tab.IsSelected = true;
            tab.Selected();
        }
    }

    public ICVarEntry<bool> HasDismissedEarlyAccessWarning => Cfg.GetCVarEntry(CVars.HasDismissedEarlyAccessWarning);
    public bool ShouldShowIntelDegradationWarning => IsVulnerableToIntelDegradation(_cfg);
    public bool ShouldShowRosettaWarning => IsAppleSiliconInRosetta(_cfg);
    [ObservableProperty] private bool _shouldShowAuthOverrideWarning;
    [ObservableProperty] private int _authOverrideCountdown = 5;
    [ObservableProperty] private bool _isAuthOverrideButtonEnabled;

    public string Version => $"v{LauncherVersion.Version}";

    public async void OnWindowInitialized()
    {
        BusyTask = _loc.GetString("main-window-busy-checking-update");
        await CheckLauncherUpdate();
        BusyTask = _loc.GetString("main-window-busy-checking-login-status");
        await CheckAccounts();
        BusyTask = null;

        if (_cfg.SelectedLoginId is { } g && _loginMgr.Logins.TryLookup(g, out var login))
        {
            TrySwitchToAccount(login);
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var update = await LauncherUpdateManager.Instance.CheckForUpdatesAsync();
                if (update != null
                    && !LauncherUpdateManager.Instance.HasDismissedStartupPrompt
                    && !LauncherUpdateManager.Instance.IsVersionSkipped(update.TagName))
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (OverlayViewModel == null)
                        {
                            OverlayViewModel = new LauncherUpdatePromptOverlayViewModel(this, update);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to check for launcher updates on startup.");
            }
        });
    }

    private async Task CheckAccounts()
    {
        // Check if accounts are still valid and refresh their tokens if necessary.
        await _loginMgr.Initialize();
    }

    public void OnDiscordButtonPressed()
    {
        Helpers.OpenUri(new Uri(ConfigConstants.DiscordUrl));
    }

    public void OnWebsiteButtonPressed()
    {
        Helpers.OpenUri(new Uri(ConfigConstants.WebsiteUrl));
    }

    private async Task CheckLauncherUpdate()
    {
        // await Task.Delay(1000);
        if (!ConfigConstants.DoVersionCheck)
        {
            return;
        }

        await _infoManager.LoadTask;
        if (_infoManager.Model == null)
        {
            // Error while loading.
            Log.Warning("Unable to check for launcher update due to error, assuming up-to-date.");
            OutOfDate = false;
            return;
        }

        OutOfDate = Array.IndexOf(_infoManager.Model.AllowedVersions, ConfigConstants.CurrentLauncherVersion) == -1;
        Log.Debug("Launcher out of date? {Value}", OutOfDate);
    }

    public void ExitPressed()
    {
        Control?.Close();
    }

    public void DownloadPressed()
    {
        Helpers.OpenUri(new Uri(ConfigConstants.DownloadUrl));
    }

    public void DismissEarlyAccessPressed()
    {
        Cfg.SetCVar(CVars.HasDismissedEarlyAccessWarning, true);
        Cfg.CommitConfig();
    }

    public void DismissIntelDegradationPressed()
    {
        Cfg.SetCVar(CVars.HasDismissedIntelDegradation, true);
        Cfg.CommitConfig();
        OnPropertyChanged(nameof(ShouldShowIntelDegradationWarning));
    }

    public void DismissAppleSiliconRosettaPressed()
    {
        Cfg.SetCVar(CVars.HasDismissedRosettaWarning, true);
        Cfg.CommitConfig();
        OnPropertyChanged(nameof(ShouldShowRosettaWarning));
    }

    public void DismissAuthOverridePressed()
    {
        _authOverrideCountdownTimer?.Dispose();
        _authOverrideCountdownTimer = null;
        ShouldShowAuthOverrideWarning = false;
    }

    public void StartAuthOverrideCountdown()
    {
        AuthOverrideCountdown = 5;
        IsAuthOverrideButtonEnabled = false;
        _authOverrideCountdownTimer?.Dispose();

        _authOverrideCountdownTimer = DispatcherTimer.Run(() =>
        {
            AuthOverrideCountdown--;
            if (AuthOverrideCountdown <= 0)
            {
                IsAuthOverrideButtonEnabled = true;
                _authOverrideCountdownTimer?.Dispose();
                _authOverrideCountdownTimer = null;
                return false;
            }
            return true;
        }, TimeSpan.FromSeconds(1), DispatcherPriority.Normal);
    }

    public void SelectTabServers()
    {
        SelectedIndex = Tabs.IndexOf(ServersTab);
    }

    public void TrySwitchToAccount(LoggedInAccount account)
    {
        switch (account.Status)
        {
            case AccountLoginStatus.Unsure:
                TrySelectUnsureAccount(account);
                break;

            case AccountLoginStatus.Available:
                _loginMgr.ActiveAccount = account;
                break;

            case AccountLoginStatus.Expired:
                _loginMgr.ActiveAccount = null;
                LoginViewModel.SwitchToExpiredLogin(account);
                break;
        }
    }

    private async void TrySelectUnsureAccount(LoggedInAccount account)
    {
        BusyTask = _loc.GetString("main-window-busy-checking-account-status");
        try
        {
            await _loginMgr.UpdateSingleAccountStatus(account);

            // Can't be unsure, that'd have thrown.
            Debug.Assert(account.Status != AccountLoginStatus.Unsure);
            TrySwitchToAccount(account);
        }
        catch (AuthApiException e)
        {
            Log.Warning(e, "AuthApiException while trying to refresh account {login}", account.LoginInfo);
            OverlayViewModel = new AuthErrorsOverlayViewModel(this, _loc.GetString("main-window-error-connecting-auth-server"),
                new[]
                {
                    e.InnerException?.Message ?? _loc.GetString("main-window-error-unknown")
                });
        }
        finally
        {
            BusyTask = null;
        }
    }

    public void OverlayOk()
    {
        OverlayViewModel = null;
    }

    public bool IsContentBundleDropValid(IStorageFile file)
    {
        // Can only load content bundles if logged in, in some capacity.
        if (!LoggedIn)
            return false;

        // Disallow if currently connecting to a server.
        if (ConnectingVM != null)
            return false;

        return Path.GetExtension(file.Name) == ".zip";
    }

    public void Dropped(IStorageFile file)
    {
        // Trust view validated this.
        Debug.Assert(IsContentBundleDropValid(file));

        ConnectingViewModel.StartContentBundle(this, file);
    }

    private static bool IsVulnerableToIntelDegradation(DataManager cfg)
    {
        var processor = LauncherDiagnostics.GetProcessorModel();

        // No Intel processor, or already dismissed the warning.
        if (!processor.Contains("Intel") || cfg.GetCVar(CVars.HasDismissedIntelDegradation))
            return false;

        // Get the i#-#### from the processor string.
        var match = Regex.Match(processor, @"i\d+-\d+(?:[A-Z]+)?(?=\s|$)");
        if (!match.Success)
            return false;

        var affectedGenerations = new[] { "i3-13", "i5-13", "i7-13", "i9-13", "i3-14", "i5-14", "i7-14", "i9-14" };
        var excludedSuffixes = new[] { "HX", "H", "P", "U" };

        return affectedGenerations.Any(match.Value.Contains) && !excludedSuffixes.Any(match.Value.EndsWith);
    }

    private static bool IsAppleSiliconInRosetta(DataManager cfg)
    {
        if (!OperatingSystem.IsMacOS())
            return false;

        var processor = LauncherDiagnostics.GetProcessorModel();

        return processor.Contains("VirtualApple") && !cfg.GetCVar(CVars.HasDismissedRosettaWarning);
    }
}
