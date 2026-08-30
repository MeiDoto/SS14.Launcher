using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Serilog;

namespace SS14.Launcher.Utility;

public static class DesktopNotificationManager
{
    private static readonly Dictionary<string, DateTime> _lastNotified = new();
    private static readonly object _lock = new();

    public static void Notify(string title, string message, string? serverAddress = null)
    {
        if (serverAddress != null)
        {
            lock (_lock)
            {
                if (_lastNotified.TryGetValue(serverAddress, out var lastTime) &&
                    DateTime.UtcNow - lastTime < TimeSpan.FromMinutes(3))
                {
                    return; // 3-minute cooldown per server
                }
                _lastNotified[serverAddress] = DateTime.UtcNow;
            }
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                SendLinuxNotification(title, message);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SendWindowsNotification(title, message);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                SendMacNotification(title, message);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to send desktop notification");
        }
    }

    private static void SendLinuxNotification(string title, string message)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "notify-send",
                ArgumentList = { "-a", "SS14.Launcher", "-u", "normal", title, message },
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var iconPath = Path.Combine(LauncherPaths.DirLauncherInstall, "Assets", "icon.png");
            if (File.Exists(iconPath))
            {
                psi.ArgumentList.Insert(0, "-i");
                psi.ArgumentList.Insert(1, iconPath);
            }

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "notify-send execution failed on Linux, trying kdialog fallback");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "kdialog",
                    ArgumentList = { "--title", title, "--passivepopup", message, "5" },
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch
            {
                // ignore
            }
        }
    }

    private static void SendWindowsNotification(string title, string message)
    {
        try
        {
            var escapedTitle = title.Replace("'", "''").Replace("`", "``").Replace("$", "`$");
            var escapedMsg = message.Replace("'", "''").Replace("`", "``").Replace("$", "`$");

            var script = $@"[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null;
$template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02);
$textNodes = $template.GetElementsByTagName('text');
$textNodes.Item(0).AppendChild($template.CreateTextNode('{escapedTitle}')) > $null;
$textNodes.Item(1).AppendChild($template.CreateTextNode('{escapedMsg}')) > $null;
$toast = [Windows.UI.Notifications.ToastNotification]::new($template);
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('SS14.Launcher').Show($toast);";

            var encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                ArgumentList = { "-NoProfile", "-ExecutionPolicy", "Bypass", "-EncodedCommand", encoded },
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "PowerShell toast notification failed on Windows");
        }
    }

    private static void SendMacNotification(string title, string message)
    {
        try
        {
            var script = $"display notification \"{message.Replace("\"", "\\\"")}\" with title \"{title.Replace("\"", "\\\"")}\"";
            var psi = new ProcessStartInfo
            {
                FileName = "osascript",
                ArgumentList = { "-e", script },
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "osascript notification failed on macOS");
        }
    }
}
