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

            var iconSrcPath = Path.Combine(installDir, "SS14.png");
            if (!File.Exists(iconSrcPath))
                iconSrcPath = Path.Combine(installDir, "Assets", "SS14.png");

            if (File.Exists(iconSrcPath))
            {
                File.Copy(iconSrcPath, iconDestPath, overwrite: true);
            }

            var appsDir = Path.Combine(dataHome, "applications");
            Directory.CreateDirectory(appsDir);
            var menuDesktopPath = Path.Combine(appsDir, "SS14.desktop");

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
            try
            {
                File.SetUnixFileMode(menuDesktopPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch { }

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
            catch { }

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
                catch { }

                try
                {
                    Process.Start(new ProcessStartInfo("gio", $"set \"{desktopShortcut}\" metadata::trusted true") { CreateNoWindow = true, UseShellExecute = false });
                }
                catch { }
            }

            try
            {
                Process.Start(new ProcessStartInfo("update-desktop-database", $"\"{appsDir}\"") { CreateNoWindow = true, UseShellExecute = false });
            }
            catch { }

            Log.Information("Linux desktop shortcuts created successfully.");
            return (true, "Shortcuts created on Desktop and Application Menu!");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create Linux desktop shortcut");
            return (false, ex.Message);
        }
    }
}
