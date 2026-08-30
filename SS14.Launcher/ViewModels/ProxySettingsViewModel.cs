using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Splat;
using SS14.Launcher.Localization;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;

namespace SS14.Launcher.ViewModels;

public sealed class ProxySettingsViewModel : ViewModelBase
{
    private readonly DataManager _cfg;

    private bool _proxyEnabled;
    private string _proxyType;
    private string _proxyHost;
    private int _proxyPort;
    private string _proxyUsername;
    private string _proxyPassword;
    private bool _proxyApplyToGameClient;
    private bool _proxyApplyToLauncher;

    private string _testStatus = "";
    private bool _isTesting;
    private bool _testSuccess;

    public string[] AvailableProxyTypes => ["SOCKS5", "HTTP", "HTTPS"];

    public bool ProxyEnabled
    {
        get => _proxyEnabled;
        set => SetProperty(ref _proxyEnabled, value);
    }

    public string ProxyType
    {
        get => _proxyType;
        set => SetProperty(ref _proxyType, value);
    }

    public string ProxyHost
    {
        get => _proxyHost;
        set => SetProperty(ref _proxyHost, value);
    }

    public int ProxyPort
    {
        get => _proxyPort;
        set => SetProperty(ref _proxyPort, Math.Clamp(value, 1, 65535));
    }

    public string ProxyUsername
    {
        get => _proxyUsername;
        set => SetProperty(ref _proxyUsername, value);
    }

    public string ProxyPassword
    {
        get => _proxyPassword;
        set => SetProperty(ref _proxyPassword, value);
    }

    public bool ProxyApplyToGameClient
    {
        get => _proxyApplyToGameClient;
        set => SetProperty(ref _proxyApplyToGameClient, value);
    }

    public bool ProxyApplyToLauncher
    {
        get => _proxyApplyToLauncher;
        set => SetProperty(ref _proxyApplyToLauncher, value);
    }

    public string TestStatus
    {
        get => _testStatus;
        set => SetProperty(ref _testStatus, value);
    }

    public bool IsTesting
    {
        get => _isTesting;
        set => SetProperty(ref _isTesting, value);
    }

    public bool TestSuccess
    {
        get => _testSuccess;
        set => SetProperty(ref _testSuccess, value);
    }

    public ProxySettingsViewModel()
    {
        _cfg = Locator.Current.GetRequiredService<DataManager>();

        _proxyEnabled = _cfg.GetCVar(CVars.ProxyEnabled);
        _proxyType = _cfg.GetCVar(CVars.ProxyType);
        _proxyHost = _cfg.GetCVar(CVars.ProxyHost);
        _proxyPort = _cfg.GetCVar(CVars.ProxyPort);
        _proxyUsername = _cfg.GetCVar(CVars.ProxyUsername);
        _proxyPassword = _cfg.GetCVar(CVars.ProxyPassword);
        _proxyApplyToGameClient = _cfg.GetCVar(CVars.ProxyApplyToGameClient);
        _proxyApplyToLauncher = _cfg.GetCVar(CVars.ProxyApplyToLauncher);
    }

    public void Save()
    {
        _cfg.SetCVar(CVars.ProxyEnabled, ProxyEnabled);
        _cfg.SetCVar(CVars.ProxyType, ProxyType);
        _cfg.SetCVar(CVars.ProxyHost, ProxyHost.Trim());
        _cfg.SetCVar(CVars.ProxyPort, ProxyPort);
        _cfg.SetCVar(CVars.ProxyUsername, ProxyUsername.Trim());
        _cfg.SetCVar(CVars.ProxyPassword, ProxyPassword);
        _cfg.SetCVar(CVars.ProxyApplyToGameClient, ProxyApplyToGameClient);
        _cfg.SetCVar(CVars.ProxyApplyToLauncher, ProxyApplyToLauncher);
        _cfg.CommitConfig();
    }

    public async Task TestConnectionAsync()
    {
        if (IsTesting) return;

        IsTesting = true;
        TestStatus = LocalizationManager.Instance.GetString("proxy-dialog-testing");
        TestSuccess = false;

        try
        {
            var host = ProxyHost.Trim();
            if (string.IsNullOrWhiteSpace(host))
            {
                TestStatus = LocalizationManager.Instance.GetString("proxy-dialog-test-specify-host");
                return;
            }

            var type = ProxyType.ToLowerInvariant();
            var uriBuilder = new UriBuilder
            {
                Scheme = type.StartsWith("socks") ? "socks5" : (type.StartsWith("https") ? "https" : "http"),
                Host = host,
                Port = ProxyPort
            };

            if (!string.IsNullOrWhiteSpace(ProxyUsername))
            {
                uriBuilder.UserName = ProxyUsername;
                uriBuilder.Password = ProxyPassword;
            }

            var proxyUri = uriBuilder.Uri;
            var webProxy = new WebProxy(proxyUri);
            if (!string.IsNullOrWhiteSpace(ProxyUsername))
            {
                webProxy.Credentials = new NetworkCredential(ProxyUsername, ProxyPassword);
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(7));
            var sw = Stopwatch.StartNew();

            using var handler = new SocketsHttpHandler
            {
                Proxy = webProxy,
                UseProxy = true,
                ConnectTimeout = TimeSpan.FromSeconds(5)
            };

            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(6)
            };

            // Test request to auth server
            var response = await client.GetAsync("https://auth.spacestation14.com/api/info", cts.Token);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                TestSuccess = true;
                TestStatus = LocalizationManager.Instance.GetString("proxy-dialog-test-success", ("type", ProxyType), ("ping", sw.ElapsedMilliseconds));
            }
            else
            {
                TestStatus = LocalizationManager.Instance.GetString("proxy-dialog-test-error-status",
                    ("status", (int)response.StatusCode),
                    ("reason", response.ReasonPhrase ?? ""),
                    ("ping", sw.ElapsedMilliseconds));
            }
        }
        catch (Exception ex)
        {
            TestStatus = LocalizationManager.Instance.GetString("proxy-dialog-test-error", ("error", ex.Message));
        }
        finally
        {
            IsTesting = false;
        }
    }
}
