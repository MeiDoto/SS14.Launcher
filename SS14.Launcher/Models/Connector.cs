using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using DynamicData;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Serilog;
using Splat;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.EngineManager;
using SS14.Launcher.Models.Logins;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Models;

/// <summary>
/// Responsible for actually launching the game.
/// Either by connecting to a game server, or by launching a local content bundle.
/// </summary>
public partial class Connector : ObservableObject
{
    private readonly Updater _updater = Locator.Current.GetRequiredService<Updater>();
    private readonly DataManager _cfg = Locator.Current.GetRequiredService<DataManager>();
    private readonly LoginManager _loginManager = Locator.Current.GetRequiredService<LoginManager>();
    private readonly IEngineManager _engineManager = Locator.Current.GetRequiredService<IEngineManager>();

    private readonly HttpClient _http = Locator.Current.GetRequiredService<HttpClient>();

    private TaskCompletionSource<PrivacyPolicyAcceptResult>? _acceptPrivacyPolicyTcs;

    [ObservableProperty] private ConnectionStatus _status = ConnectionStatus.None;
    [ObservableProperty] private bool _clientExitedBadly;
    [ObservableProperty] private bool _privacyPolicyDifferentVersion;
    public ServerPrivacyPolicyInfo? PrivacyPolicyInfo { get; private set; }

    /// <summary>
    /// Initiates an asynchronous connection to a game server address (e.g. ss14://..., http://...),
    /// managing authentication token exchange, engine version verification/download, and game process launching.
    /// </summary>
    /// <param name="address">Target server address.</param>
    /// <param name="cancel">Cancellation token.</param>
    public async void Connect(string address, CancellationToken cancel = default)
    {
        try
        {
            await ConnectInternalAsync(address, cancel);
        }
        catch (ConnectException e)
        {
            Log.Error(e, "Failed to connect: {status}", e.Status);
            Status = e.Status;
        }
        catch (OperationCanceledException e)
        {
            Log.Information(e, "Cancelled connect");
            Status = ConnectionStatus.Cancelled;
        }
        catch (Exception e)
        {
            Log.Error(e, "Unexpected error during connection attempt");
            Status = ConnectionStatus.ConnectionFailed;
        }
        finally
        {
            Cleanup();
        }
    }

    /// <summary>
    /// Launches a local content bundle (.zip package containing game build) in standalone client mode.
    /// </summary>
    /// <param name="file">Storage file reference to the content bundle.</param>
    /// <param name="cancel">Cancellation token.</param>
    public async void LaunchContentBundle(IStorageFile file, CancellationToken cancel = default)
    {
        Log.Information("Launching content bundle: {FileName}", file.Path);

        try
        {
            await LaunchContentBundleInternal(file, cancel);
        }
        catch (ConnectException e)
        {
            Log.Error(e, "Failed to launch: {status}", e.Status);
            Status = e.Status;
        }
        catch (OperationCanceledException e)
        {
            Log.Information(e, "Cancelled launch");
            Status = ConnectionStatus.Cancelled;
        }
        catch (Exception e)
        {
            Log.Error(e, "Unexpected error during content bundle launch");
            Status = ConnectionStatus.ConnectionFailed;
        }
        finally
        {
            Cleanup();
        }
    }

    private async Task ConnectInternalAsync(string address, CancellationToken cancel)
    {
        Status = ConnectionStatus.Connecting;

        var (info, parsedAddr, infoAddr) = await GetServerInfoAsync(address, cancel);

        await HandlePrivacyPolicyAsync(info, cancel);

        // Run update.
        Status = ConnectionStatus.Updating;

        // Must have been set when retrieving build info (inferred to be automatic zipping).
        Debug.Assert(info.BuildInformation != null, "info.BuildInformation != null");

        var installation = await RunUpdateAsync(info.BuildInformation, cancel);

        var connectAddress = GetConnectAddress(info, infoAddr);

        await LaunchClientWrap(installation, info, info.BuildInformation, connectAddress, parsedAddr, false, cancel);
    }

    private async Task HandlePrivacyPolicyAsync(ServerInfo info, CancellationToken cancel)
    {
        if (info.PrivacyPolicy == null)
        {
            // Server has no privacy policy configured, nothing to do.
            return;
        }

        var identifier = info.PrivacyPolicy.Identifier;
        var version = info.PrivacyPolicy.Version;

        if (_cfg.HasAcceptedPrivacyPolicy(identifier, out var acceptedVersion))
        {
            if (version == acceptedVersion)
            {
                Log.Debug(
                    "User has previously accepted privacy policy {Identifier} with version {Version}",
                    identifier,
                    acceptedVersion);

                // User has previously accepted privacy policy, update last connected time in DB at least.
                _cfg.UpdateConnectedToPrivacyPolicy(identifier);
                _cfg.CommitConfig();
                return;
            }
            else
            {
                Log.Debug("User previously accepted privacy policy but version has changed!");
                PrivacyPolicyDifferentVersion = true;
            }
        }

        // Ask user for privacy policy acceptance by waiting here.
        Log.Debug("Prompting user for privacy policy acceptance: {Identifer} version {Version}", identifier, version);
        PrivacyPolicyInfo = info.PrivacyPolicy;
        _acceptPrivacyPolicyTcs = new TaskCompletionSource<PrivacyPolicyAcceptResult>();

        Status = ConnectionStatus.AwaitingPrivacyPolicyAcceptance;
        var result = await _acceptPrivacyPolicyTcs.Task.WaitAsync(cancel);

        if (result == PrivacyPolicyAcceptResult.Accepted)
        {
            // User accepted privacy policy.
            Log.Debug("User accepted privacy policy");
            _cfg.AcceptPrivacyPolicy(identifier, version);
            _cfg.CommitConfig();
            return;
        }

        // User rejected privacy policy. Cancel connection.
        Log.Information("User denied privacy policy, cancelling connection attempt!");
        throw new OperationCanceledException();
    }

    public void ConfirmPrivacyPolicy(PrivacyPolicyAcceptResult result)
    {
        if (_acceptPrivacyPolicyTcs == null)
        {
            Log.Error("_acceptPrivacyPolicyTcs is null???");
            return;
        }

        _acceptPrivacyPolicyTcs.TrySetResult(result);
    }

    private void Cleanup()
    {
        PrivacyPolicyInfo = null;
        _acceptPrivacyPolicyTcs = null;
        PrivacyPolicyDifferentVersion = default;
    }

    private async Task LaunchContentBundleInternal(IStorageFile file, CancellationToken cancel)
    {
        Status = ConnectionStatus.Updating;

        ContentLaunchInfo installation;
        await using (var zipStream = await file.OpenReadAsync())
        {
            var zipHash = await Task.Run(() => Updater.HashFileSha256(zipStream), cancel);

            zipStream.Seek(0, SeekOrigin.Begin);

            using var zipFile = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var metadataJson = zipFile.GetEntry("rt_content_bundle.json");
            if (metadataJson == null)
            {
                Log.Error("Zip file did not contain rt_content_bundle.json");
                throw new ConnectException(ConnectionStatus.NotAContentBundle);
            }

            ContentBundleMetadata? metadata;
            using (var metadataStream = metadataJson.Open())
            {
                metadata = JsonSerializer.Deserialize<ContentBundleMetadata>(metadataStream);
            }

            if (metadata == null)
            {
                Log.Error("rt_content_bundle.json deserialized as null");
                throw new ConnectException(ConnectionStatus.NotAContentBundle);
            }

            Log.Debug("Loaded metadata for content bundle, continuing with launch");

            //
            // Big comment time
            //
            // Originally, I wanted to implement content bundles by not touching the Content DB at all.
            // (At least, if you're not using a base build)
            // The loader would open the zip file directly and provide the engine with both files simultaneously.
            //
            // That all kinda fell apart when I realized that manifest.yml has to be interpreted by the launcher.
            // And then also stuff like dependent engine versions have to be tracked and all that.
            // So, instead we merge the provided content bundle into the Content DB and start the game as normal.
            //
            // I don't like this solution much, as content bundles for SS14 replays will be quite bug (150+ MB).
            // It's a lot of data that needs to get uselessly shoved between the Content DB.
            //
            // In the future, a "hybrid" mode may be best:
            // The launcher will create a new version in the Content DB that contains just the manifest.yml.
            // (or base build data overlaid if necessary)
            // The loader would still be in charge of transparently merging in the zip file at runtime.

            //
            // EXCEPT!
            // SS14 replays, the biggest files, don't have a manifest.yml! So that above comment is all for naught!
            // We only ingest into the ContentDB if there isn't a manifest.yml and there *is* a base build.
            // Why this set of requirements? ...because it's the least intrusive to make SS14 replays better.
            // Also, we need to actually be able to access the zip as a path to give it to the launcher.
            //
            if (zipFile.GetEntry("manifest.yml") is null
                && metadata.BaseBuild is not null
                && file.TryGetLocalPath() is { } localPath)
            {
                installation = await RunUpdateAsync(metadata.GetBaseBuildInformation(), cancel);
                installation = installation with { OverlayZip = localPath };
            }
            else
            {
                installation = await InstallContentBundleAsync(zipFile, zipHash, metadata, cancel);
            }

            if (metadata.ServerGC == true)
                installation = installation with { ServerGC = true };
        }

        Log.Debug("Launching client");

        // Replay bundles pass minimal build info to the client directly.
        await LaunchClientWrap(installation, null, null, null, null, true, cancel);
    }

    private async Task LaunchClientWrap(
        ContentLaunchInfo launchInfo,
        ServerInfo? info = null,
        ServerBuildInformation? buildInfo = null,
        Uri? connectAddress = null,
        Uri? parsedAddr = null,
        bool contentBundle = false,
        CancellationToken cancel = default)
    {
        Status = ConnectionStatus.StartingClient;

        var clientProc = await ConnectLaunchClient(launchInfo, info, buildInfo, connectAddress, parsedAddr, contentBundle);

        if (clientProc != null)
        {
            var startTime = DateTime.UtcNow;
            var targetAddr = parsedAddr?.ToString() ?? connectAddress?.ToString();

            // Wait 300ms to verify the client process initialized without immediate crash.
            var waitClient = clientProc.WaitForExitAsync(cancel);
            var waitDelay = Task.Delay(300, cancel);

            await Task.WhenAny(waitDelay, waitClient);

            if (!clientProc.HasExited)
            {
                Status = ConnectionStatus.ClientRunning;
                await waitClient;

                if (_cfg.GetCVar(CVars.TrackPlaytime) && !string.IsNullOrEmpty(targetAddr))
                {
                    var seconds = (long)(DateTime.UtcNow - startTime).TotalSeconds;
                    if (seconds >= 3)
                    {
                        _cfg.AddServerPlaytime(targetAddr, seconds);
                    }
                }
                return;
            }

            ClientExitedBadly = clientProc.ExitCode != 0;
        }
        else
        {
            ClientExitedBadly = true;
        }

        Status = ConnectionStatus.ClientExited;
    }

    private async Task<Process?> ConnectLaunchClient(ContentLaunchInfo launchInfo,
        ServerInfo? info,
        ServerBuildInformation? serverBuildInformation,
        Uri? connectAddress,
        Uri? parsedAddr,
        bool contentBundle)
    {
        var cVars = new List<(string, string)>();

        if (info != null && info.AuthInformation.Mode != AuthMode.Disabled && _loginManager.ActiveAccount != null)
        {
            var account = _loginManager.ActiveAccount;

            cVars.Add(("ROBUST_AUTH_TOKEN", account.LoginInfo.Token.Token));
            cVars.Add(("ROBUST_AUTH_USERID", account.LoginInfo.UserId.ToString()));
            cVars.Add(("ROBUST_AUTH_PUBKEY", info.AuthInformation.PublicKey));
            cVars.Add(("ROBUST_AUTH_SERVER", ConfigConstants.AuthUrl.GetMostSuccessfulUrl()));
        }

        try
        {
            var compatMode = (_cfg.GetCVar(CVars.CompatMode) && !OperatingSystem.IsMacOS()) || CheckForceCompatMode();

            var args = new List<string>
            {
                // Pass username to launched client.
                // We don't load username from client_config.toml when launched via launcher.
                "--username", _loginManager.ActiveAccount?.Username ?? ConfigConstants.FallbackUsername,

                // GLES2 forcing or using default fallback
                "--cvar", $"display.compat={compatMode}",

                // Tell game we are launcher
                "--cvar", "launch.launcher=true"
            };

            if (contentBundle)
            {
                args.Add("--cvar");
                args.Add("launch.content_bundle=true");
            }

            if (connectAddress != null)
            {
                // We are using the launcher. Don't show main menu etc..
                // Note: --launcher also implied --connect.
                // For this reason, content bundles do not set --launcher.
                args.Add("--launcher");

                args.Add("--connect-address");
                args.Add(connectAddress.ToString());
            }

            if (parsedAddr != null)
            {
                args.Add("--ss14-address");
                args.Add(parsedAddr.ToString());
            }

            // Pass build info to client. Initally added for replays, it is now used for connecting on modern robust CDN versions.
            // If engine_version or manifest_hash is null, the client WILL fail to connect.
            // serverBuildInformation is only null in case of content bundles which shouldn't try to connect to live servers anyways

            BuildCVar("download_url", serverBuildInformation?.DownloadUrl);
            BuildCVar("manifest_url", serverBuildInformation?.ManifestUrl);
            BuildCVar("manifest_download_url", serverBuildInformation?.ManifestDownloadUrl);
            BuildCVar("version", serverBuildInformation?.Version);
            BuildCVar("fork_id", serverBuildInformation?.ForkId);
            BuildCVar("hash", serverBuildInformation?.Hash);
            BuildCVar("manifest_hash", serverBuildInformation?.ManifestHash);
            BuildCVar("engine_version", serverBuildInformation?.EngineVersion);

            void BuildCVar(string name, string? value)
            {
                if (value == null)
                    return;

                args.Add("--cvar");
                args.Add($"build.{name}={value}");
            }

            // Launch client.
            return await LaunchClient(launchInfo, args, cVars);
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception while starting client");
            return null;
        }
    }

    private static Uri GetConnectAddress(ServerInfo info, Uri infoAddr)
    {
        if (string.IsNullOrEmpty(info.ConnectAddress))
        {
            // No connect address specified, use same address/port as base address.
            return new UriBuilder
            {
                Scheme = "udp",
                Host = infoAddr.Host,
                Port = infoAddr.Port
            }.Uri;
        }

        try
        {
            return new Uri(info.ConnectAddress);
        }
        catch (FormatException e)
        {
            Log.Error(e, "Failed to parse ConnectAddress");
            throw new ConnectException(ConnectionStatus.ConnectionFailed);
        }
    }

    private async Task<ContentLaunchInfo> RunUpdateAsync(ServerBuildInformation info, CancellationToken cancel)
    {
        var installation = await _updater.RunUpdateForLaunchAsync(info, cancel);
        if (installation == null)
        {
            throw new ConnectException(ConnectionStatus.UpdateError);
        }

        return installation;
    }

    private async Task<ContentLaunchInfo> InstallContentBundleAsync(
        ZipArchive archive,
        byte[] zipHash,
        ContentBundleMetadata metadata,
        CancellationToken cancel)
    {
        var installation = await _updater.InstallContentBundleForLaunchAsync(archive, zipHash, metadata, cancel);
        if (installation == null)
        {
            throw new ConnectException(ConnectionStatus.UpdateError);
        }

        return installation;
    }

    private async Task<(ServerInfo, Uri, Uri)> GetServerInfoAsync(string address, CancellationToken cancel)
    {
        if (!UriHelper.TryParseSs14Uri(address, out var parsedAddress))
        {
            Log.Error("Invalid URI in GetServerInfoAsync: {Uri}", address);
            throw new ConnectException(ConnectionStatus.ConnectionFailed);
        }

        // Fetch server connect info.
        var infoAddr = UriHelper.GetServerInfoAddress(parsedAddress);

        try
        {
            var info = await _http.GetFromJsonAsync<ServerInfo>(infoAddr, cancel) ?? throw new InvalidDataException();
            if (info.BuildInformation is {} buildInfo && (buildInfo.Acz || string.IsNullOrEmpty(buildInfo.DownloadUrl)))
            {
                var acz = info.BuildInformation.Acz;
                var apiAddress = UriHelper.GetServerApiAddress(parsedAddress);

                // Infer download URL to be self-hosted client address if not supplied
                // (The server may not know it's own address)
                info.BuildInformation.DownloadUrl = new Uri(apiAddress, "client.zip").ToString();

                if (acz)
                {
                    info.BuildInformation.ManifestUrl = new Uri(apiAddress, "manifest.txt").ToString();
                    info.BuildInformation.ManifestDownloadUrl = new Uri(apiAddress, "download").ToString();
                }
            }
            return (info, parsedAddress, infoAddr);
        }
        catch (Exception e) when (e is JsonException or HttpRequestException or InvalidDataException)
        {
            throw new ConnectException(ConnectionStatus.ConnectionFailed, e);
        }
    }

    public static InstalledEngineModule? GetInstalledModuleForEngineVersion(
        Version engineVersion,
        string moduleName,
        DataManager dataManager)
    {
        return dataManager.EngineModules
            .Where(m => m.Name == moduleName)
            .Select(m => new { Version = Version.Parse(m.Version), m })
            .Where(m => engineVersion >= m.Version)
            .MaxBy(m => m.Version)?.m;
    }

    private async Task<Process?> LaunchClient(
        ContentLaunchInfo launchInfo,
        IEnumerable<string> extraArgs,
        List<(string, string)> env)
    {
        var pubKey = LauncherPaths.PathPublicKey;
        if (!File.Exists(pubKey))
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "signing_key"),
                Path.Combine(LauncherPaths.DirLauncherInstall, "signing_key"),
                Path.Combine(Path.GetFullPath(Path.Combine(LauncherPaths.DirLauncherInstall, "..", "..", "..", "..")), "SS14.Launcher", "signing_key"),
                Path.Combine(Path.GetFullPath(Path.Combine(LauncherPaths.DirLauncherInstall, "..", "..")), "signing_key")
            };
            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    pubKey = candidate;
                    break;
                }
            }
        }

        var engineVersion = launchInfo.ModuleInfo.Single(x => x.Module == "Robust").Version;
        var binPath = _engineManager.GetEnginePath(engineVersion);
        var sig = _engineManager.GetEngineSignature(engineVersion);

        var startInfo = await GetLoaderStartInfo();

        startInfo.ArgumentList.Add(binPath);
        startInfo.ArgumentList.Add(sig);
        startInfo.ArgumentList.Add(pubKey);

        foreach (var (k, v) in env)
        {
            startInfo.EnvironmentVariables[k] = v;
        }

        EnvVar("SS14_LOADER_CONTENT_DB", LauncherPaths.PathContentDb);
        EnvVar("SS14_LOADER_CONTENT_VERSION", launchInfo.Version.ToString());
        EnvVar("SS14_LOADER_OVERLAY_ZIP", launchInfo.OverlayZip);

        // Env vars for engine modules.
        {
            foreach (var (moduleName, moduleVersion) in launchInfo.ModuleInfo)
            {
                if (moduleName == "Robust")
                    continue;

                var modulePath = _engineManager.GetEngineModule(moduleName, moduleVersion);

                var envVar = $"ROBUST_MODULE_{moduleName.ToUpperInvariant().Replace('.', '_')}";
                EnvVar(envVar, modulePath);
            }
        }

        if (_cfg.GetCVar(CVars.DisableSigning))
            EnvVar("SS14_DISABLE_SIGNING", "true");

        EnvVar("SS14_LAUNCHER_PATH", Environment.ProcessPath ?? AppContext.BaseDirectory);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            EnvVar("SS14_LOG_CLIENT", LauncherPaths.PathClientMacLog);
        }

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        // Performance tweaks, GPU offloading, and proxy settings
        GameEnvironmentHelper.ConfigureEnvironment(startInfo, _cfg);

        var isReplay = !string.IsNullOrEmpty(launchInfo.OverlayZip);

        if (isReplay)
        {
            EnvVar("DOTNET_SYSTEM_BUFFERS_SHARED_MAXARRAYPERPARTITION", "1048576");
            EnvVar("DOTNET_TieredPGO", "1");
            EnvVar("DOTNET_TC_CallCounting", "0");
            EnvVar("DOTNET_TC_QuickJitForLoops", "1");
            EnvVar("DOTNET_EnableHWIntrinsic", "1");
            EnvVar("DOTNET_GCConcurrent", "1");
            EnvVar("DOTNET_GCConserveMemory", "0");
            startInfo.ArgumentList.Add("--cvar");
            startInfo.ArgumentList.Add("replay.preload=true");
        }

        if (_cfg.GetCVar(CVars.DevGenerateCrashDumps))
        {
            EnvVar("DOTNET_DbgEnableMiniDump", "1");
            EnvVar("DOTNET_DbgMiniDumpType", "2");
            EnvVar("DOTNET_DbgMiniDumpName", Path.Combine(LauncherPaths.DirUserData, "ss14_crash.dmp"));
        }

        if (_cfg.GetCVar(CVars.DevStrictDiagnostics))
        {
            EnvVar("DOTNET_DbgEnableMiniDump", "1");
            EnvVar("DOTNET_EnableCrashReport", "1");
            startInfo.ArgumentList.Add("--cvar");
            startInfo.ArgumentList.Add("debug.throw_on_unhandled=true");
        }

        if (_cfg.GetCVar(CVars.DevFastThreadPool))
        {
            EnvVar("DOTNET_ThreadPool_ForceMinWorkerThreads", "16");
            EnvVar("DOTNET_ThreadPool_UnfairSemaphoreSpinLimit", "100");
        }

        if (_cfg.GetCVar(CVars.DevAggressiveLohTrim))
        {
            EnvVar("DOTNET_GCLargeObjectHeapCompactionMode", "1");
            EnvVar("DOTNET_GCConserveMemory", "5");
        }

        if (_cfg.GetCVar(CVars.DevGCNoAffinitize))
        {
            EnvVar("DOTNET_GCNoAffinitize", "1");
        }

        if (_cfg.GetCVar(CVars.DevRenderValidation))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                EnvVar("VK_INSTANCE_LAYERS", "VK_LAYER_KHRONOS_validation");
                EnvVar("MESA_DEBUG", "1");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EnvVar("D3D12_DEBUG_LAYER", "1");
                EnvVar("DXVK_LOG_LEVEL", "debug");
            }
        }

        EnvVar("DOTNET_TieredPGO", _cfg.GetCVar(CVars.DevTieredPgo) ? "1" : "0");

        var gcLimit = _cfg.GetCVar(CVars.DevGcHeapLimitMb);
        if (gcLimit > 0)
        {
            EnvVar("DOTNET_GCHeapHardLimit", (gcLimit * 1024L * 1024L).ToString("X"));
        }

        var graphicsBackend = _cfg.GetCVar(CVars.DevGraphicsBackend);
        if (!string.IsNullOrWhiteSpace(graphicsBackend) && !graphicsBackend.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            if (graphicsBackend.Equals("Software", StringComparison.OrdinalIgnoreCase))
            {
                EnvVar("LIBGL_ALWAYS_SOFTWARE", "1");
                EnvVar("GALLIUM_DRIVER", "llvmpipe");
            }
            else
            {
                SetGameCVar("display.renderer", graphicsBackend);
            }
        }

        var displayMode = _cfg.GetCVar(CVars.DevDisplayMode);
        if (!string.IsNullOrWhiteSpace(displayMode) && !displayMode.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            var modeVal = displayMode switch
            {
                "Fullscreen" => "2",
                "Borderless" => "1",
                _ => "0"
            };
            SetGameCVar("display.window_mode", modeVal);
        }

        if (_cfg.GetCVar(CVars.DevDebugFpsOverlay))
        {
            SetGameCVar("hud.fps_counter", "true");
            SetGameCVar("display.fps_counter", "true");
            SetGameCVar("debug.fps", "true");
            startInfo.ArgumentList.Add("+showtime");
        }

        if (_cfg.GetCVar(CVars.DevDebugNetGraph))
        {
            SetGameCVar("net.netgraph", "true");
            SetGameCVar("hud.net_graph", "true");
            startInfo.ArgumentList.Add("+netgraph");
        }

        if (_cfg.GetCVar(CVars.DevOpenConsoleOnStart))
        {
            startInfo.ArgumentList.Add("+toggleconsole");
            startInfo.ArgumentList.Add("+openconsole");
        }

        if (_cfg.GetCVar(CVars.DevPhysicsDebug))
        {
            SetGameCVar("physics.debug_draw", "true");
            SetGameCVar("physics.overlay", "true");
            startInfo.ArgumentList.Add("+physics");
            startInfo.ArgumentList.Add("overlay");
        }

        if (_cfg.GetCVar(CVars.DevShowLightMap))
        {
            SetGameCVar("debug.show_light", "true");
            SetGameCVar("light.draw_lighting", "false");
            startInfo.ArgumentList.Add("+showlight");
        }

        if (_cfg.GetCVar(CVars.DevShowEntityBounds))
        {
            SetGameCVar("debug.entity_bounds", "true");
            startInfo.ArgumentList.Add("+entitybounds");
        }

        if (_cfg.GetCVar(CVars.DevMuteAudio))
        {
            SetGameCVar("audio.master_volume", "0");
        }

        var audioBuf = _cfg.GetCVar(CVars.DevAudioBufferSize);
        if (audioBuf > 0)
        {
            SetGameCVar("audio.buffer_size", audioBuf.ToString());
        }

        var customEnv = _cfg.GetCVar(CVars.DevCustomEnvVars);
        if (!string.IsNullOrWhiteSpace(customEnv))
        {
            var lines = customEnv.Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var eqIdx = line.IndexOf('=');
                if (eqIdx > 0 && eqIdx <= line.Length - 1)
                {
                    var k = line.Substring(0, eqIdx).Trim();
                    var v = eqIdx < line.Length - 1 ? line.Substring(eqIdx + 1).Trim() : string.Empty;
                    EnvVar(k, v);
                }
            }
        }

        if (launchInfo.ServerGC || _cfg.GetCVar(CVars.ForceServerGC))
            EnvVar("DOTNET_gcServer", "1");

        ConfigureMultiWindow(launchInfo, startInfo);

        // Лоадер использует ту же версию .NET среды, что и лаунчер, поэтому RollForward указывать не требуется
        EnvVar("DOTNET_MULTILEVEL_LOOKUP", "0");

        startInfo.UseShellExecute = false;
        startInfo.ArgumentList.AddRange(extraArgs);

        var devLogLevel = _cfg.GetCVar(CVars.DevLogLevel);
        if (!string.IsNullOrWhiteSpace(devLogLevel) && !devLogLevel.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add("--loglevel");
            startInfo.ArgumentList.Add(devLogLevel.ToLowerInvariant());
        }

        if (_cfg.GetCVar(CVars.DevUncappedFps))
        {
            SetGameCVar("display.vsync", "false");
            SetGameCVar("display.max_fps", "0");
        }

        var simulatedPing = _cfg.GetCVar(CVars.DevSimulatedPingMs);
        if (simulatedPing > 0)
        {
            SetGameCVar("net.fake_lag", simulatedPing.ToString());
        }

        var simulatedJitter = _cfg.GetCVar(CVars.DevSimulatedJitterMs);
        if (simulatedJitter > 0)
        {
            SetGameCVar("net.fake_jitter", simulatedJitter.ToString());
        }

        var simulatedLoss = _cfg.GetCVar(CVars.DevSimulatedPacketLoss);
        if (simulatedLoss > 0)
        {
            var lossRatio = (simulatedLoss / 100.0f).ToString(System.Globalization.CultureInfo.InvariantCulture);
            SetGameCVar("net.fake_loss", lossRatio);
        }

        if (_cfg.GetCVar(CVars.DevDisableNetCompression))
        {
            SetGameCVar("net.compression", "false");
        }

        var customArgs = _cfg.GetCVar(CVars.DevCustomLaunchArguments);
        if (!string.IsNullOrWhiteSpace(customArgs))
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(customArgs, @"[\""].+?[\""]|[^ ]+");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var arg = match.Value.Trim('"');
                if (!string.IsNullOrWhiteSpace(arg))
                {
                    startInfo.ArgumentList.Add(arg);
                }
            }
        }

        var commandBuilder = new StringBuilder();
        commandBuilder.Append(startInfo.FileName);

        for (var i = 0; i < startInfo.ArgumentList.Count; i++)
        {
            var arg = startInfo.ArgumentList[i];

            commandBuilder.Append($" [{i}] {arg}");
        }

        Log.Debug("Launch command: {LaunchCommand}", commandBuilder.ToString());

        var highPriority = isReplay || _cfg.GetCVar(CVars.HighProcessPriority);
        var process = GameProcessRunner.StartGameProcess(startInfo, highPriority);

        return process;

        void SetGameCVar(string name, string value)
        {
            startInfo.ArgumentList.Add("--cvar");
            startInfo.ArgumentList.Add($"{name}={value}");

            var envKey = "ROBUST_CVAR_" + name.ToUpperInvariant().Replace('.', '_');
            EnvVar(envKey, value);
        }

        void EnvVar(string envVar, string? value)
        {
            startInfo.EnvironmentVariables[envVar] = value;
            // Log.Debug("Env: {EnvVar} = {Value}", envVar, value);
        }
    }

    private static void ConfigureMultiWindow(ContentLaunchInfo launchInfo, ProcessStartInfo startInfo)
    {
        // Implemented in private repo for Steam.
    }

    private static void PipeLogOutput(Process process)
    {
        int pid = 0;
        try { pid = process.Id; } catch { /* ignore */ }
        Log.Debug("Piping output for process {pid} straight to logs", pid);

        async void DoPipe(TextReader reader)
        {
            try
            {
                while (true)
                {
                    var read = await reader.ReadLineAsync();

                    if (read == null)
                    {
                        Log.Debug("EOF, ending pipe logging for {pid}", pid);
                        return;
                    }

                    Log.Information("piped: {content}", read);
                }
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                Log.Debug("Pipe log ended for {pid}", pid);
            }
        }

        try
        {
            DoPipe(process.StandardError);
            DoPipe(process.StandardOutput);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to setup pipe log output for {pid}", pid);
        }
    }

#pragma warning disable 162
    private static async Task<ProcessStartInfo> GetLoaderStartInfo()
    {
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "SS14.Loader.exe" : "SS14.Loader";
        var dllName = "SS14.Loader.dll";

        var baseDir = LauncherPaths.DirLauncherInstall;
        var solutionRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var appPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "Space Station 14.app"));
            if (Directory.Exists(appPath))
            {
                Log.Debug("Using app bundle: {appPath}", appPath);
                Log.Debug("Clearing quarantine on loader.");

                var xattr = Process.Start(new ProcessStartInfo
                {
                    FileName = "xattr",
                    ArgumentList = { "-d", "com.apple.quarantine", appPath },
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                });

                if (xattr is null)
                    throw new Exception("Xattr failed to start");
                PipeLogOutput(xattr);

                await xattr.WaitForExitAsync();

                var startInfo = new ProcessStartInfo
                {
                    FileName = "open",
                    ArgumentList = { appPath }
                };

                if (RuntimeInformation.OSArchitecture != Architecture.X64)
                {
                    if (OperatingSystem.IsMacOSVersionAtLeast(14))
                    {
                        startInfo.ArgumentList.Add("--arch");
                        startInfo.ArgumentList.Add(
                            RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x86_64");
                    }
                }

                startInfo.ArgumentList.Add("--args");
                return startInfo;
            }
        }

        string[] candidateDirs =
        [
            Path.Combine(baseDir, "loader"),
            baseDir,
            Path.Combine(baseDir, "..", "loader"),
            Path.Combine(solutionRoot, "bin", "publish", "Linux", "bin_x64", "loader"),
            Path.Combine(solutionRoot, "bin", "publish", "Windows", "bin_x64", "loader"),
            Path.Combine(solutionRoot, "publish", "linux-x64", "loader"),
            Path.Combine(solutionRoot, "publish", "windows-x64", "loader"),
            Path.Combine(solutionRoot, "SS14.Loader", "bin", "Release", "net10.0", "linux-x64", "publish"),
            Path.Combine(solutionRoot, "SS14.Loader", "bin", "Release", "net10.0", "linux-x64"),
            Path.Combine(solutionRoot, "SS14.Loader", "bin", "Release", "net10.0", "win-x64", "publish"),
            Path.Combine(solutionRoot, "SS14.Loader", "bin", "Release", "net10.0", "win-x64"),
            Path.Combine(solutionRoot, "SS14.Loader", "bin", "Debug", "net10.0", "linux-x64"),
            Path.Combine(solutionRoot, "SS14.Loader", "bin", "Debug", "net10.0", "win-x64"),
            Path.Combine(solutionRoot, "SS14.Loader", "bin", "Release", "net10.0"),
            Path.Combine(solutionRoot, "SS14.Loader", "bin", "Debug", "net10.0")
        ];

        string? validLoaderDir = null;
        string? foundDllPath = null;

        foreach (var dir in candidateDirs)
        {
            if (!Directory.Exists(dir))
                continue;

            var exeP = Path.Combine(dir, exeName);
            var dllP = Path.Combine(dir, dllName);

            if (File.Exists(exeP) && File.Exists(dllP))
            {
                validLoaderDir = dir;
                break;
            }

            if (foundDllPath == null && File.Exists(dllP))
            {
                foundDllPath = dllP;
            }
        }

        // Native binary exists with its managed payload
        if (validLoaderDir != null)
        {
            var localLoaderDir = Path.Combine(baseDir, "loader");
            var localLoaderExe = Path.Combine(localLoaderDir, exeName);

            if (validLoaderDir != localLoaderDir || !File.Exists(localLoaderExe) || !File.Exists(Path.Combine(localLoaderDir, dllName)))
            {
                try
                {
                    Directory.CreateDirectory(localLoaderDir);
                    foreach (var file in Directory.GetFiles(validLoaderDir))
                    {
                        var destFile = Path.Combine(localLoaderDir, Path.GetFileName(file));
                        File.Copy(file, destFile, true);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to copy loader files to local directory");
                }
            }

            var finalExe = File.Exists(localLoaderExe) ? localLoaderExe : Path.Combine(validLoaderDir, exeName);
            Log.Information("Using loader binary at: {LoaderPath}", finalExe);

            Helpers.ChmodPlusX(finalExe);

            return new ProcessStartInfo
            {
                FileName = finalExe
            };
        }

        // Managed DLL exists - run with dotnet
        if (foundDllPath != null)
        {
            Log.Information("Using managed loader DLL at: {LoaderDllPath}", foundDllPath);
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet"
            };
            psi.ArgumentList.Add(foundDllPath);
            return psi;
        }

        var fallbackPath = Path.Combine(baseDir, "loader", exeName);
        Log.Warning("Loader binary not found in candidate paths. Falling back to default: {LoaderPath}", fallbackPath);
        Helpers.ChmodPlusX(fallbackPath);

        return new ProcessStartInfo
        {
            FileName = fallbackPath
        };
    }
#pragma warning restore 162

    public enum ConnectionStatus
    {
        None,
        Updating,
        UpdateError,
        Connecting,
        AwaitingPrivacyPolicyAcceptance,
        ConnectionFailed,
        StartingClient,
        ClientRunning,
        ClientExited,
        Cancelled,
        NotAContentBundle
    }

    private sealed class ConnectException : Exception
    {
        public ConnectionStatus Status { get; }

        public ConnectException(ConnectionStatus status)
        {
            Status = status;
        }

        public ConnectException(ConnectionStatus status, Exception inner)
            : base($"Failed to connect: {status}", inner)
        {
            Status = status;
        }
    }
}

public sealed record ContentBundleMetadata(
    [property: JsonPropertyName("server_gc")]
    bool? ServerGC,
    [property: JsonPropertyName("engine_version")]
    string EngineVersion,
    [property: JsonPropertyName("base_build")]
    ContentBundleBaseBuild? BaseBuild
)
{
    public ServerBuildInformation GetBaseBuildInformation()
    {
        if (BaseBuild == null)
            throw new InvalidOperationException("Metadata must have base build!");

        return new ServerBuildInformation
        {
            DownloadUrl = BaseBuild.DownloadUrl,
            ManifestUrl = BaseBuild.ManifestUrl,
            ManifestDownloadUrl = BaseBuild.ManifestDownloadUrl,
            EngineVersion = EngineVersion,
            Version = BaseBuild.Version,
            ForkId = BaseBuild.ForkId,
            Hash = BaseBuild.Hash,
            ManifestHash = BaseBuild.ManifestHash,
            Acz = false
        };
    }
}

public sealed record ContentBundleBaseBuild(
    [property: JsonPropertyName("fork_id")] string ForkId,
    [property: JsonPropertyName("version")] string Version,
    // Old zip-download system.
    [property: JsonPropertyName("download_url")] string? DownloadUrl,
    [property: JsonPropertyName("hash")] string? Hash,
    // Newer manifest download system.
    [property: JsonPropertyName("manifest_download_url")] string? ManifestDownloadUrl,
    [property: JsonPropertyName("manifest_url")] string? ManifestUrl,
    [property: JsonPropertyName("manifest_hash")] string? ManifestHash
);

public enum PrivacyPolicyAcceptResult
{
    Denied,
    Accepted,
}
