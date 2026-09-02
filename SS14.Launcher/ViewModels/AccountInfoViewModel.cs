using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.Logins;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public sealed class AccountInfoViewModel : ViewModelBase
{
    private readonly DataManager _cfg = Locator.Current.GetRequiredService<DataManager>();
    private readonly LoginManager _loginMgr = Locator.Current.GetRequiredService<LoginManager>();
    private readonly LocalizationManager _loc = LocalizationManager.Instance;

    public string Username { get; private set; } = "-";
    public string UserId { get; private set; } = "-";
    public string Email { get; private set; } = "-";
    public string StatusText { get; private set; } = "-";
    public string TokenExpiresText { get; private set; } = "-";
    public string PasswordStatusText { get; private set; } = "-";
    public string Hwid { get; private set; } = "-";
    public string SystemInfo { get; private set; } = "-";
    public string TotalPlaytimeText { get; private set; } = "-";

    public void Populate()
    {
        PasswordStatusText = _loc.GetString("account-info-password-protected");
        var active = _loginMgr.ActiveAccount;
        if (active != null)
        {
            Username = active.Username;
            UserId = active.UserId.ToString();
            Email = _loc.GetString("account-info-email-linked");
            StatusText = active.Status switch
            {
                AccountLoginStatus.Available => _loc.GetString("account-info-status-active"),
                AccountLoginStatus.Expired => _loc.GetString("account-info-status-expired"),
                _ => _loc.GetString("account-info-status-unknown")
            };

            var exp = active.LoginInfo.Token.ExpireTime;
            TokenExpiresText = exp != default ? exp.ToLocalTime().ToString("g") : _loc.GetString("account-info-token-permanent");
        }
        else
        {
            Username = _loc.GetString("account-info-guest");
            UserId = Guid.Empty.ToString();
            Email = _loc.GetString("account-info-none");
            StatusText = _loc.GetString("account-info-not-logged-in");
            TokenExpiresText = "-";
        }

        try
        {
            long totalSec = 0;
            var playtimeMap = _cfg.GetServerPlaytime();
            foreach (var sec in playtimeMap.Values)
            {
                totalSec += sec;
            }
            TotalPlaytimeText = PlaytimeFormatter.Format(totalSec);
        }
        catch (Exception ex)
        {
            TotalPlaytimeText = PlaytimeFormatter.Format(0);
        }

        try
        {
            var rawHwid = $"{Environment.MachineName}-{Environment.UserName}-{Environment.OSVersion.VersionString}-{Environment.ProcessorCount}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawHwid));
            Hwid = Convert.ToHexString(hash)[..24];
            SystemInfo = $"{Environment.OSVersion} ({Environment.ProcessorCount} Cores)";
        }
        catch (Exception ex)
        {
            Hwid = "UNKNOWN-HWID";
            SystemInfo = Environment.OSVersion.ToString();
        }

        OnPropertyChanged(nameof(Username));
        OnPropertyChanged(nameof(UserId));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(TokenExpiresText));
        OnPropertyChanged(nameof(PasswordStatusText));
        OnPropertyChanged(nameof(Hwid));
        OnPropertyChanged(nameof(SystemInfo));
        OnPropertyChanged(nameof(TotalPlaytimeText));
    }

    private bool _isUserIdRevealed;
    public bool IsUserIdRevealed
    {
        get => _isUserIdRevealed;
        set
        {
            if (SetProperty(ref _isUserIdRevealed, value))
            {
                OnPropertyChanged(nameof(UserIdToggleIcon));
            }
        }
    }

    private bool _isHwidRevealed;
    public bool IsHwidRevealed
    {
        get => _isHwidRevealed;
        set
        {
            if (SetProperty(ref _isHwidRevealed, value))
            {
                OnPropertyChanged(nameof(HwidToggleIcon));
            }
        }
    }

    public string UserIdToggleIcon => _isUserIdRevealed ? "🔒" : "👁";
    public string HwidToggleIcon => _isHwidRevealed ? "🔒" : "👁";

    private string _copyUserIdText = "";
    public string CopyUserIdText
    {
        get => string.IsNullOrEmpty(_copyUserIdText) ? _loc.GetString("account-info-copy") : _copyUserIdText;
        private set => SetProperty(ref _copyUserIdText, value);
    }

    private string _copyHwidText = "";
    public string CopyHwidText
    {
        get => string.IsNullOrEmpty(_copyHwidText) ? _loc.GetString("account-info-copy") : _copyHwidText;
        private set => SetProperty(ref _copyHwidText, value);
    }

    private string _copyAllDiagText = "";
    public string CopyAllDiagText
    {
        get => string.IsNullOrEmpty(_copyAllDiagText) ? _loc.GetString("account-info-copy-all-diag") : _copyAllDiagText;
        private set => SetProperty(ref _copyAllDiagText, value);
    }

    public void ToggleUserIdVisibility() => IsUserIdRevealed = !IsUserIdRevealed;
    public void ToggleHwidVisibility() => IsHwidRevealed = !IsHwidRevealed;

    public void CopyUserId()
    {
        _ = ClipboardHelper.CopyWithFeedbackAsync(UserId, s => CopyUserIdText = s);
    }

    public void CopyHwid()
    {
        _ = ClipboardHelper.CopyWithFeedbackAsync(Hwid, s => CopyHwidText = s);
    }

    public void CopyAllDiagnostics()
    {
        var text = $"### SS14 Account Diagnostics\n" +
                   $"- **User**: `{Username}`\n" +
                   $"- **UserID**: `{UserId}`\n" +
                   $"- **HWID**: `{Hwid}`\n" +
                   $"- **OS**: `{SystemInfo}`\n" +
                   $"- **Total Playtime**: `{TotalPlaytimeText}`\n" +
                   $"- **Token Status**: `{StatusText}`\n" +
                   $"- **Launcher**: `v{ConfigConstants.LauncherCustomVersion}` (.NET 10.0)";

        _ = ClipboardHelper.CopyWithFeedbackAsync(text, s => CopyAllDiagText = s);
    }

    public void OpenAccountWebsite()
    {
        Helpers.OpenUri(ConfigConstants.AccountManagementUrl);
    }

    public void OpenChangePassword()
    {
        Helpers.OpenUri("https://account.spacestation14.com/Identity/Account/Manage/ChangePassword");
    }

    public void Open2FaSettings()
    {
        Helpers.OpenUri("https://account.spacestation14.com/Identity/Account/Manage/TwoFactorAuthentication");
    }
}
