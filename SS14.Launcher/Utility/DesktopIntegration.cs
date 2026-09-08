using System;
using System.Diagnostics;
using System.IO;
using Serilog;

namespace SS14.Launcher.Utility;

public static class DesktopIntegration
{
    public static bool IsSupported => OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

    public static (bool Success, string Message) CreateDesktopAndMenuShortcuts()
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateWindowsShortcuts();
        }

        if (OperatingSystem.IsLinux())
        {
            return CreateLinuxShortcuts();
        }

        return (false, "Shortcut creation is not supported on this operating system.");
    }

    private static (bool Success, string Message) CreateWindowsShortcuts()
    {
        try
        {
            var installDir = AppContext.BaseDirectory;
            var exePath = Path.Combine(installDir, "Space Station 14 Launcher.exe");
            if (!File.Exists(exePath))
            {
                exePath = Path.Combine(installDir, "SS14.Launcher.exe");
            }

            if (!File.Exists(exePath))
            {
                var parent = Directory.GetParent(installDir)?.FullName;
                if (!string.IsNullOrEmpty(parent))
                {
                    var parentExe = Path.Combine(parent, "Space Station 14 Launcher.exe");
                    if (!File.Exists(parentExe))
                        parentExe = Path.Combine(parent, "SS14.Launcher.exe");

                    if (File.Exists(parentExe))
                    {
                        installDir = parent;
                        exePath = parentExe;
                    }
                }
            }

            if (!File.Exists(exePath))
            {
                return (false, "Launcher executable not found.");
            }

            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var programsDir = Environment.GetFolderPath(Environment.SpecialFolder.Programs);

            var psScript = $@"
$ws = New-Object -ComObject WScript.Shell
$desktop = '{desktopDir.Replace("'", "''")}'
if (Test-Path $desktop) {{
    $sc = $ws.CreateShortcut((Join-Path $desktop 'Space Station 14 Launcher.lnk'))
    $sc.TargetPath = '{exePath.Replace("'", "''")}'
    $sc.WorkingDirectory = '{installDir.Replace("'", "''")}'
    $sc.Description = 'Space Station 14 Launcher'
    $sc.IconLocation = '{exePath.Replace("'", "''")},0'
    $sc.Save()
}}
$programs = '{programsDir.Replace("'", "''")}'
if (Test-Path $programs) {{
    $sc2 = $ws.CreateShortcut((Join-Path $programs 'Space Station 14 Launcher.lnk'))
    $sc2.TargetPath = '{exePath.Replace("'", "''")}'
    $sc2.WorkingDirectory = '{installDir.Replace("'", "''")}'
    $sc2.Description = 'Space Station 14 Launcher'
    $sc2.IconLocation = '{exePath.Replace("'", "''")},0'
    $sc2.Save()
}}
";

            var encodedScript = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(psScript));
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encodedScript}",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(10000);

            Log.Information("Windows desktop shortcuts created successfully.");
            return (true, "Shortcuts created on Desktop and Start Menu!");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create Windows shortcuts");
            return (false, ex.Message);
        }
    }

    private static (bool Success, string Message) CreateLinuxShortcuts()
    {
        try
        {
            var installDir = AppContext.BaseDirectory;
            var binPath = Path.Combine(installDir, "SS14.Launcher");
            if (!File.Exists(binPath))
            {
                var parent = Directory.GetParent(installDir)?.FullName;
                if (!string.IsNullOrEmpty(parent) && File.Exists(Path.Combine(parent, "SS14.Launcher")))
                {
                    installDir = parent;
                    binPath = Path.Combine(installDir, "SS14.Launcher");
                }
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            var dataHome = !string.IsNullOrEmpty(xdgDataHome)
                ? xdgDataHome
                : Path.Combine(home, ".local", "share");

            var iconDestDir = Path.Combine(dataHome, "icons", "hicolor", "256x256", "apps");
            Directory.CreateDirectory(iconDestDir);
            var iconDestPath = Path.Combine(iconDestDir, "SS14.png");

            CopyIconToDestination(iconDestPath);

            var pixmapsDir = Path.Combine(dataHome, "pixmaps");
            try
            {
                Directory.CreateDirectory(pixmapsDir);
                if (File.Exists(iconDestPath))
                {
                    File.Copy(iconDestPath, Path.Combine(pixmapsDir, "SS14.png"), overwrite: true);
                }
            }
            catch (Exception) { }

            var appsDir = Path.Combine(dataHome, "applications");
            Directory.CreateDirectory(appsDir);
            var menuDesktopPath = Path.Combine(appsDir, "SS14.desktop");
            var launcherDesktopPath = Path.Combine(appsDir, "SS14.Launcher.desktop");

            var desktopContent = $@"#!/usr/bin/env xdg-open
[Desktop Entry]
Type=Application
Version=1.5
Name=Space Station 14 Launcher
Name[ru]=Лаунчер Space Station 14
GenericName=Space Station 14 Launcher
GenericName[ru]=Лаунчер Space Station 14
Comment=A multiplayer disaster simulator
Comment[ru]=Многопользовательский симулятор космической станции
Icon={iconDestPath}
Exec=""{binPath}"" %u
Path={installDir}
Categories=Game;
Keywords=game;gaming;launcher;multiplayer;ss14;
StartupNotify=true
StartupWMClass=SS14.Launcher
SingleMainWindow=true
Terminal=false
PrefersNonDefaultGPU=false
";
            File.WriteAllText(menuDesktopPath, desktopContent);
            File.WriteAllText(launcherDesktopPath, desktopContent);
            try
            {
                File.SetUnixFileMode(menuDesktopPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                File.SetUnixFileMode(launcherDesktopPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Could not set UnixFileMode on {Path}", menuDesktopPath);
            }

            // Try to find Desktop directory from user-dirs.dirs or fallback
            var desktopDir = Path.Combine(home, "Desktop");
            try
            {
                var userDirsFile = Path.Combine(home, ".config", "user-dirs.dirs");
                if (File.Exists(userDirsFile))
                {
                    foreach (var line in File.ReadAllLines(userDirsFile))
                    {
                        if (line.StartsWith("XDG_DESKTOP_DIR=", StringComparison.OrdinalIgnoreCase))
                        {
                            var val = line.Substring("XDG_DESKTOP_DIR=".Length).Trim('"', ' ');
                            val = val.Replace("$HOME", home);
                            if (Directory.Exists(val))
                            {
                                desktopDir = val;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to parse user-dirs.dirs for XDG_DESKTOP_DIR");
            }

            if (!Directory.Exists(desktopDir))
            {
                var ruDesktopDir = Path.Combine(home, "Рабочий стол");
                if (Directory.Exists(ruDesktopDir))
                {
                    desktopDir = ruDesktopDir;
                }
            }

            if (Directory.Exists(desktopDir))
            {
                var desktopShortcut = Path.Combine(desktopDir, "SS14.desktop");
                File.WriteAllText(desktopShortcut, desktopContent);
                try
                {
                    File.SetUnixFileMode(desktopShortcut, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Could not set UnixFileMode on {Path}", desktopShortcut);
                }

                try
                {
                    Process.Start(new ProcessStartInfo("gio", $"set \"{desktopShortcut}\" metadata::trusted true") { CreateNoWindow = true, UseShellExecute = false });
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "Failed to execute gio metadata trust on shortcut");
                }
            }

            try
            {
                Process.Start(new ProcessStartInfo("update-desktop-database", $"\"{appsDir}\"") { CreateNoWindow = true, UseShellExecute = false });
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to run update-desktop-database");
            }

            Log.Information("Linux desktop shortcuts created successfully.");
            return (true, "Shortcuts created on Desktop and Application Menu!");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create Linux desktop shortcut");
            return (false, ex.Message);
        }
    }

    public static void EnsureLinuxIconInstalled()
    {
        if (!OperatingSystem.IsLinux())
            return;

        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            var dataHome = !string.IsNullOrEmpty(xdgDataHome)
                ? xdgDataHome
                : Path.Combine(home, ".local", "share");

            var iconDestDir = Path.Combine(dataHome, "icons", "hicolor", "256x256", "apps");
            Directory.CreateDirectory(iconDestDir);
            var iconDestPath = Path.Combine(iconDestDir, "SS14.png");

            if (!File.Exists(iconDestPath))
            {
                CopyIconToDestination(iconDestPath);
            }

            var pixmapsDir = Path.Combine(dataHome, "pixmaps");
            var pixmapPath = Path.Combine(pixmapsDir, "SS14.png");
            if (!File.Exists(pixmapPath) && File.Exists(iconDestPath))
            {
                Directory.CreateDirectory(pixmapsDir);
                File.Copy(iconDestPath, pixmapPath, overwrite: true);
            }

            var appsDir = Path.Combine(dataHome, "applications");
            var ss14DesktopPath = Path.Combine(appsDir, "SS14.desktop");
            var launcherDesktopPath = Path.Combine(appsDir, "SS14.Launcher.desktop");

            if (File.Exists(ss14DesktopPath) && !File.Exists(launcherDesktopPath))
            {
                File.Copy(ss14DesktopPath, launcherDesktopPath, overwrite: true);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to ensure Linux icon is installed");
        }
    }

    private static void CopyIconToDestination(string iconDestPath)
    {
        var installDir = AppDomain.CurrentDomain.BaseDirectory;
        string[] candidateSrcs =
        [
            Path.Combine(installDir, "SS14.png"),
            Path.Combine(installDir, "icon.png"),
            Path.Combine(installDir, "Assets", "SS14.png"),
            Path.Combine(installDir, "Assets", "icon.png"),
            Path.Combine(AppContext.BaseDirectory, "SS14.png"),
            Path.Combine(AppContext.BaseDirectory, "icon.png"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "SS14.png"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "icon.png")
        ];

        foreach (var cand in candidateSrcs)
        {
            if (File.Exists(cand))
            {
                File.Copy(cand, iconDestPath, overwrite: true);
                return;
            }
        }

        try
        {
            using var assetStream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://SS14.Launcher/Assets/icon.png"));
            using var fileStream = File.Create(iconDestPath);
            assetStream.CopyTo(fileStream);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to extract embedded icon to {Dest}", iconDestPath);
        }
    }
}
