using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SS14.Launcher.Bootstrap;

internal static partial class Program
{
    public static void Main(string[] args)
    {
        // Очищаем устаревшие записи DOTNET_ROOT в реестре, если они были оставлены старыми скриптами
        CleanStaleDotnetRootRegistryKey();

        // Определяем базовую директорию лаунчера
        var ourDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!Directory.Exists(Path.Combine(ourDir, "bin_x64"))
            && !Directory.Exists(Path.Combine(ourDir, "bin_x86"))
            && !Directory.Exists(Path.Combine(ourDir, "bin_arm64")))
        {
            var parent = Path.GetDirectoryName(ourDir);
            if (!string.IsNullOrEmpty(parent) &&
                (Directory.Exists(Path.Combine(parent, "bin_x64")) ||
                 Directory.Exists(Path.Combine(parent, "bin_x86")) ||
                 Directory.Exists(Path.Combine(parent, "bin_arm64"))))
            {
                ourDir = parent;
            }
        }

        // Автоматическое определение архитектуры процессора (x64, x86, ARM64)
        var architecture = "x64";
        if (RuntimeInformation.OSArchitecture == Architecture.Arm64 && Directory.Exists(Path.Combine(ourDir, "bin_arm64")))
        {
            architecture = "arm64";
        }
        else if (RuntimeInformation.OSArchitecture == Architecture.X86 && Directory.Exists(Path.Combine(ourDir, "bin_x86")))
        {
            architecture = "x86";
        }
        else if (!Directory.Exists(Path.Combine(ourDir, "bin_x64")) && Directory.Exists(Path.Combine(ourDir, "bin_x86")))
        {
            architecture = "x86";
        }

        var dotnetDir = Path.Combine(ourDir, $"dotnet_{architecture}");
        if (!Directory.Exists(dotnetDir) && Directory.Exists(Path.Combine(ourDir, "dotnet_x64")))
        {
            dotnetDir = Path.Combine(ourDir, "dotnet_x64");
        }

        var exeDir = Path.Combine(ourDir, $"bin_{architecture}");
        if (!Directory.Exists(exeDir) && Directory.Exists(Path.Combine(ourDir, "bin_x64")))
        {
            exeDir = Path.Combine(ourDir, "bin_x64");
        }

        var launcherExe = Path.Combine(exeDir, "SS14.Launcher.exe");
        var launcherDll = Path.Combine(exeDir, "SS14.Launcher.dll");
        var dotnetExe = Path.Combine(dotnetDir, "dotnet.exe");

        // Настройка переменных окружения для встроенного рантайма .NET
        if (Directory.Exists(dotnetDir))
        {
            Environment.SetEnvironmentVariable("DOTNET_ROOT", dotnetDir);
        }
        Environment.SetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0");

        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        Environment.SetEnvironmentVariable("PATH", $"{dotnetDir};{exeDir};{currentPath}");

        var isDebug = Array.IndexOf(args, "--debug") != -1;

        if (!isDebug)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = File.Exists(launcherExe) ? launcherExe : (File.Exists(dotnetExe) ? dotnetExe : "SS14.Launcher.exe"),
                    WorkingDirectory = ourDir,
                    UseShellExecute = false
                };

                if (!File.Exists(launcherExe) && File.Exists(dotnetExe))
                {
                    psi.ArgumentList.Add(launcherDll);
                }

                foreach (var arg in args)
                {
                    psi.ArgumentList.Add(arg);
                }

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                AllocConsole();
                Console.WriteLine("========================================");
                Console.WriteLine("Space Station 14 Launcher - Ошибка запуска");
                Console.WriteLine("========================================");
                Console.WriteLine($"Папка установки: {ourDir}");
                Console.WriteLine($"Архитектура: {architecture}");
                Console.WriteLine($"Исполняемый файл: {launcherExe}");
                Console.WriteLine($"Среда выполнения .NET: {dotnetExe}");
                Console.WriteLine($"Текст ошибки: {ex.Message}");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("\nНажмите Enter для выхода...");
                Console.ReadLine();
            }
        }
        else
        {
            AllocConsole();

            Console.WriteLine("========================================");
            Console.WriteLine("Space Station 14 Launcher - Консоль отладки");
            Console.WriteLine("========================================");
            Console.WriteLine($"Папка: {ourDir}");
            Console.WriteLine($"Архитектура: {architecture}");
            Console.WriteLine($"DOTNET_ROOT: {dotnetDir}");
            Console.WriteLine($"Исполняемый файл: {launcherExe}");

            var psi = new ProcessStartInfo
            {
                FileName = File.Exists(dotnetExe) ? dotnetExe : launcherExe,
                WorkingDirectory = ourDir,
                UseShellExecute = false
            };

            if (File.Exists(dotnetExe))
            {
                psi.ArgumentList.Add(launcherDll);
            }

            foreach (var arg in args)
            {
                if (arg != "--debug")
                    psi.ArgumentList.Add(arg);
            }

            try
            {
                var process = Process.Start(psi);
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске процесса: {ex}");
            }

            Console.WriteLine("\nПроцесс завершил работу. Нажмите Enter для закрытия.");
            Console.ReadLine();
        }
    }

    /// <summary>
    /// Очищает устаревшие глобальные переменные реестра DOTNET_ROOT, если они ссылаются на Space Station 14.
    /// </summary>
    private static void CleanStaleDotnetRootRegistryKey()
    {
        try
        {
            using var envKey = Registry.CurrentUser.OpenSubKey("Environment", true);
            var val = envKey?.GetValue("DOTNET_ROOT");
            if (val is not string s)
                return;

            if (!s.Contains("Space Station 14") && !s.Contains("SS14.Launcher"))
                return;

            envKey.DeleteValue("DOTNET_ROOT");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Предупреждение при проверке реестра DOTNET_ROOT: {e.Message}");
        }
    }

    [LibraryImport("KERNEL32.dll")]
    private static partial int AllocConsole();
}
