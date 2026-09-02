using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Media;
using Microsoft.Win32;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Splat;
using SS14.Launcher.Api;
using SS14.Launcher.Localization;
using SS14.Launcher.Models;
using SS14.Launcher.Models.ContentManagement;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Models.ServerStatus;
using SS14.Launcher.Models.EngineManager;
using SS14.Launcher.Models.Logins;
using SS14.Launcher.Models.OverrideAssets;
using SS14.Launcher.Utility;
using TerraFX.Interop.Windows;
using LogEventLevel = Serilog.Events.LogEventLevel;

namespace SS14.Launcher;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "AppDomain unhandled exception");
            }
            else
            {
                Log.Fatal("AppDomain unhandled non-exception object: {Object}", eventArgs.ExceptionObject);
            }
            Log.CloseAndFlush();
        };

        TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
        {
            var isHarmless = eventArgs.Exception.Flatten().InnerExceptions.Any(e =>
                e is System.Net.Sockets.SocketException ||
                e is System.Net.Http.HttpRequestException ||
                e is System.IO.IOException ||
                e.GetType().Name.Contains("DBusException", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("Network is unreachable", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("No route to host", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("org.freedesktop.DBus", StringComparison.OrdinalIgnoreCase) ||
                e.Message.Contains("ServiceUnknown", StringComparison.OrdinalIgnoreCase));

            if (isHarmless)
            {
                Log.Debug(eventArgs.Exception, "Handled non-critical unobserved background task exception");
            }
            else
            {
                Log.Error(eventArgs.Exception, "Unobserved task exception");
            }
            eventArgs.SetObserved();
        };
#if DEBUG
        Console.OutputEncoding = Encoding.UTF8;
#endif

        if (OperatingSystem.IsWindows())
        {
            try
            {
                SetCurrentProcessExplicitAppUserModelID("SpaceWizards.SpaceStation14.Launcher");
            }
            catch (Exception ex)
            {
                // Ignore if Shell32 call fails on older OS
            }
        }

#if USE_SYSTEM_SQLITE
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
#endif
        var msgr = new LauncherMessaging();
        Locator.CurrentMutable.RegisterConstant(msgr);

        // Parse arguments as early as possible for launcher messaging reasons.
        string[] commands = { LauncherCommands.PingCommand };
        var commandSendAnyway = false;
        if (args.Length == 1)
        {
            // Check if this is a valid Uri, since that indicates re-invocation.
            if (Uri.TryCreate(args[0], UriKind.Absolute, out var result))
            {
                commands = new string[]
                    { LauncherCommands.BlankReasonCommand, LauncherCommands.ConstructConnectCommand(result) };
                // This ensures we queue up the connection even if we're starting the launcher now.
                commandSendAnyway = true;
            }
        }
        else if (args.Length >= 2)
        {
            if (args[0] == "--commands")
            {
                // Trying to send an arbitrary series of commands.
                // This is how the Loader is expected to communicate (and start the launcher if necessary).
                // Note that there are special "untrusted text" versions of the commands that should be used.
                commands = new string[args.Length - 1];
                for (var i = 0; i < commands.Length; i++)
                    commands[i] = args[i + 1];
                commandSendAnyway = true;
            }
        }

        // Note: This MUST occur before we do certain actions like:
        // + Open the launcher log file (and therefore wipe a user's existing launcher log)
        // + Initialize Avalonia (and therefore waste whatever time it takes to do that)
        // Therefore any messages you receive at this point will be Console.WriteLine-only!
        if (msgr.SendCommandsOrClaim(commands, commandSendAnyway))
            return;

        var logCfg = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen);

        Log.Logger = logCfg.CreateLogger();

        VcRedistCheck.Check();
        LauncherPaths.CreateDirs();

        var cfg = new DataManager();
        cfg.Load();
        Locator.CurrentMutable.RegisterConstant(cfg);

        CheckWindowsVersion();
        // Legacy antivirus conflict check disabled.
        // CheckBadAntivirus();
        CheckWine(cfg);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(cfg.GetCVar(CVars.LogLauncherVerbose) ? LogEventLevel.Verbose : LogEventLevel.Debug)
            .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
            .WriteTo.File(LauncherPaths.PathLauncherLog, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7, fileSizeLimitBytes: 100L * 1024 * 1024)
            .CreateLogger();

        LauncherDiagnostics.LogDiagnostics();

#if DEBUG
        Logger.Sink = new AvaloniaSeriLogger(new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Error)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Area} {Level:u3}] {Message} ({SourceType} #{SourceHash})\n")
            .CreateLogger());
#endif

        try
        {
            BuildAvaloniaApp(cfg).StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Unhandled exception in launcher main loop");
            throw;
        }
        finally
        {
            try
            {
                cfg.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error closing DataManager");
            }
            Log.CloseAndFlush();
        }
    }

    private static unsafe void CheckWindowsVersion()
    {
        // 14393 is Windows 10 version 1607, minimum we currently support.
        if (!OperatingSystem.IsWindows() || Environment.OSVersion.Version.Build >= 14393)
            return;

        var text =
            "You are using an old version of Windows that is no longer supported by Space Station 14.\n\n" +
            "If anything breaks, DO NOT ASK FOR HELP OR SUPPORT.";

        var caption = "Unsupported Windows version";

        uint type = MB.MB_OK | MB.MB_ICONWARNING;

        if (Language.UserHasLanguage("ru"))
        {
            text = "Вы используете старую версию Windows которая больше не поддерживается Space Station 14.\n\n" +
                   "При возникновении ошибок НЕ БУДЕТ ОКАЗАНО НИКАКОЙ ПОДДЕРЖКИ.";

            caption = "Неподдерживаемая версия Windows";
        }

        Helpers.MessageBoxHelper(text, caption, type);
    }

    private static unsafe void CheckBadAntivirus()
    {
        // Avast Free Antivirus breaks the game due to their AMSI integration crashing the process. Awesome!
        // Oh hey back here again, turns out AVG is just the same product as Avast with different paint.
        if (!OperatingSystem.IsWindows())
            return;

        var badPrograms =
            new Dictionary<string, (string shortName, string longName)>(StringComparer.InvariantCultureIgnoreCase)
            {
                // @formatter:off
                {"AvastSvc", ("Avast", "Avast Free Antivirus")},
                {"AVGSvc",   ("AVG",   "AVG Antivirus")},
                // @formatter:on
            };

        var badFound = Process.GetProcesses()
            .Select(x => x.ProcessName)
            .FirstOrDefault(x => badPrograms.ContainsKey(x));

        if (badFound == null)
            return;

        var (shortName, longName) = badPrograms[badFound];

        var text = $"{longName} is detected on your system.\n\n{shortName} is known to cause the game to crash while loading. If the game fails to start, uninstall {shortName}.\n\nThis is {shortName}'s fault, do not ask us for help or support.";
        var caption = $"{longName} detected!";
        uint type = MB.MB_OK | MB.MB_ICONWARNING;

        Helpers.MessageBoxHelper(text, caption, type);
    }

    private static void CheckWine(DataManager dataManager)
    {
        if (!OperatingSystem.IsWindows())
            return;

        if (dataManager.GetCVar(CVars.WineWarningShown))
            return;

        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wine", false);

        if (key != null)
        {
            Log.Debug("Wine detected");
            var text =
                $"You seem to be running the launcher under Wine.\n\nWe recommend you run the native Linux version instead.\n\nThis is the only time you will see this message.";
            var caption = $"Wine detected!";
            uint type = MB.MB_OK | MB.MB_ICONWARNING;

            Helpers.MessageBoxHelper(text, caption, type);
            dataManager.SetCVar(CVars.WineWarningShown, true);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp(DataManager cfg)
    {
        var locator = Locator.CurrentMutable;

        var http = HappyEyeballsHttp.CreateHttpClient();
        http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(LauncherVersion.Name, LauncherVersion.Version?.ToString()));
        http.DefaultRequestHeaders.Add("SS14-Launcher-Fingerprint", cfg.Fingerprint.ToString());
        Locator.CurrentMutable.RegisterConstant(http);

        var loc = new LocalizationManager(cfg);
        var authApi = new AuthApi(http);
        var hubApi = new HubApi(http);
        var launcherInfo = new LauncherInfoManager(http);
        var overrideAssets = new OverrideAssetsManager(cfg, http, launcherInfo);
        var loginManager = new LoginManager(cfg, authApi);
        var engineManager = new EngineManagerDynamic();

        locator.RegisterConstant(loc);
        locator.RegisterConstant(new ContentManager());
        locator.RegisterConstant<IEngineManager>(engineManager);
        locator.RegisterConstant(new Updater());
        locator.RegisterConstant(authApi);
        locator.RegisterConstant(hubApi);
        locator.RegisterConstant(new ServerListCache());
        locator.RegisterConstant(loginManager);
        locator.RegisterConstant(overrideAssets);
        locator.RegisterConstant(launcherInfo);

        CheckLauncherArchitecture(cfg, engineManager);

        return AppBuilder.Configure(() => new App(overrideAssets))
            .UsePlatformDetect()
            .With(new X11PlatformOptions
            {
                WmClass = "SS14.Launcher",
                EnableMultiTouch = true
            })
            .With(new FontManagerOptions
            {
                // Necessary workaround for #84 on Linux
                DefaultFamilyName = "avares://SS14.Launcher/Assets/Fonts/noto_sans/*.ttf#Noto Sans"
            });
    }

    private static void CheckLauncherArchitecture(DataManager cfg, EngineManagerDynamic engineManager)
    {
        var curArchitecture = RuntimeInformation.ProcessArchitecture;
        var previousArchitecture = (Architecture)cfg.GetCVar(CVars.CurrentArchitecture);
        if (previousArchitecture == curArchitecture)
            return;

        Log.Information(
            "CPU architecture has changed since last process run, clearing engine builds. Previously: {PreviousArchitecture}, now: {CurrentArchitecture}",
            previousArchitecture,
            curArchitecture);

        engineManager.ClearAllEngines();
        cfg.SetCVar(CVars.CurrentArchitecture, (int) curArchitecture);
        _ = cfg.CommitConfig();
    }

    [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);
}
