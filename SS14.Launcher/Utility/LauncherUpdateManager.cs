using System;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Splat;
using SS14.Launcher.Models.Data;

namespace SS14.Launcher.Utility;

public sealed class LauncherUpdateManager
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders =
        {
            { "User-Agent", "SS14-Launcher-UpdateManager" },
            { "Accept", "application/vnd.github.v3+json" }
        }
    };

    public static readonly LauncherUpdateManager Instance = new();

    // кэш результата проверки
    public LauncherUpdateInfo? CachedUpdate { get; private set; }
    public bool HasDismissedStartupPrompt { get; set; }

    // кулдаун: не дёргаем API чаще раза в 5 минут
    private DateTime _lastCheckUtc = DateTime.MinValue;
    private static readonly TimeSpan CheckCooldown = TimeSpan.FromMinutes(5);

    // ETag для условных запросов к GitHub API
    private string? _lastETag;

    // количество ретраев на скачивание
    private const int MaxDownloadRetries = 3;

    /// <summary>
    /// Проверяет наличие обновления на GitHub.
    /// Если прошло меньше 5 минут с последней проверки — возвращает кэш.
    /// Использует ETag для экономии трафика.
    /// </summary>
    public async Task<LauncherUpdateInfo?> CheckForUpdatesAsync(CancellationToken cancel = default)
    {
        // кулдаун — не спамим GitHub
        if ((DateTime.UtcNow - _lastCheckUtc) < CheckCooldown && CachedUpdate != null)
        {
            return CachedUpdate;
        }

        try
        {
            var url = $"https://api.github.com/repos/{ConfigConstants.LauncherGitHubRepo}/releases/latest";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            // если есть ETag от прошлого запроса — шлём его, GitHub вернёт 304 если ничего не изменилось
            if (!string.IsNullOrEmpty(_lastETag))
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(_lastETag));
            }

            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel);

            // 304 Not Modified — данные не изменились с прошлого раза
            if (response.StatusCode == System.Net.HttpStatusCode.NotModified)
            {
                _lastCheckUtc = DateTime.UtcNow;
                return CachedUpdate;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                Log.Warning("GitHub releases API rate limit exceeded (HTTP 403 Forbidden). Postponing update check.");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("GitHub releases API returned {StatusCode}", response.StatusCode);
                return null;
            }

            // сохраняем ETag
            if (response.Headers.ETag != null)
            {
                _lastETag = response.Headers.ETag.Tag;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancel);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancel);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagElem))
                return null;

            _lastCheckUtc = DateTime.UtcNow;

            var tagName = tagElem.GetString() ?? "";
            var cleanRemoteVer = tagName.TrimStart('v', 'V');

            if (!TryParseVersion(cleanRemoteVer, out var remoteVer) ||
                !TryParseVersion(ConfigConstants.LauncherCustomVersion, out var currentVer))
            {
                return null;
            }

            // текущая или старая версия — обновления нет
            if (remoteVer <= currentVer)
            {
                CachedUpdate = null;
                return null;
            }

            var title = root.TryGetProperty("name", out var nameElem) ? nameElem.GetString() ?? tagName : tagName;
            var body = root.TryGetProperty("body", out var bodyElem) ? bodyElem.GetString() ?? "" : "";

            string? targetAssetUrl = null;
            string targetAssetName = "";
            long targetAssetSize = 0;
            string? checksumsUrl = null;

            if (root.TryGetProperty("assets", out var assetsElem) && assetsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsElem.EnumerateArray())
                {
                    var assetName = asset.TryGetProperty("name", out var an) ? an.GetString() ?? "" : "";
                    var downloadUrl = asset.TryGetProperty("browser_download_url", out var du) ? du.GetString() : null;
                    var size = asset.TryGetProperty("size", out var sz) ? sz.GetInt64() : 0L;

                    if (string.IsNullOrEmpty(downloadUrl))
                        continue;

                    if (assetName.Equals("SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase) ||
                        assetName.Equals("SHA256SUMS", StringComparison.OrdinalIgnoreCase) ||
                        assetName.Equals("hashes.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        checksumsUrl = downloadUrl;
                        continue;
                    }

                    if (OperatingSystem.IsWindows() && assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && assetName.Contains("Windows", StringComparison.OrdinalIgnoreCase))
                    {
                        targetAssetUrl = downloadUrl;
                        targetAssetName = assetName;
                        targetAssetSize = size;
                    }
                    else if (OperatingSystem.IsLinux() && (assetName.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) || assetName.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)) && assetName.Contains("Linux", StringComparison.OrdinalIgnoreCase))
                    {
                        targetAssetUrl = downloadUrl;
                        targetAssetName = assetName;
                        targetAssetSize = size;
                    }
                }
            }

            if (string.IsNullOrEmpty(targetAssetUrl))
            {
                Log.Warning("New launcher release {Version} found, but matching asset for current OS was not found.", tagName);
                return null;
            }

            var expectedSha256 = ExtractSha256FromBody(body, targetAssetName);

            CachedUpdate = new LauncherUpdateInfo(
                cleanRemoteVer,
                tagName,
                title,
                body,
                targetAssetUrl,
                targetAssetSize,
                targetAssetName,
                expectedSha256,
                checksumsUrl);
            return CachedUpdate;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check for launcher updates.");
            return null;
        }
    }

    /// <summary>
    /// Проверяет, пропустил ли пользователь эту конкретную версию.
    /// Если версия новее той что он пропустил — показываем снова.
    /// </summary>
    public bool IsVersionSkipped(string tagName)
    {
        try
        {
            var cfg = Locator.Current.GetService<DataManager>();
            if (cfg == null) return false;

            var skipped = cfg.GetCVar(CVars.SkippedUpdateVersion);
            if (string.IsNullOrEmpty(skipped)) return false;

            return string.Equals(skipped, tagName, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Update check failed gracefully");
            return false;
        }
    }

    /// <summary>
    /// Запоминает версию как пропущенную.
    /// </summary>
    public void SkipVersion(string tagName)
    {
        try
        {
            var cfg = Locator.Current.GetService<DataManager>();
            if (cfg == null) return;

            cfg.SetCVar(CVars.SkippedUpdateVersion, tagName);
            _ = cfg.CommitConfig();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to save skipped version.");
        }
    }

    /// <summary>
    /// Сбрасывает пропущенную версию (вызывается при ручной проверке из настроек).
    /// </summary>
    public void ClearSkippedVersion()
    {
        try
        {
            var cfg = Locator.Current.GetService<DataManager>();
            if (cfg == null) return;

            cfg.SetCVar(CVars.SkippedUpdateVersion, "");
            _ = cfg.CommitConfig();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to clear SkippedUpdateVersion in config.");
        }
    }

    /// <summary>
    /// Сбрасывает кулдаун, чтобы следующая проверка пошла на сервер.
    /// Полезно для ручной кнопки «Проверить обновления».
    /// </summary>
    public void ResetCooldown()
    {
        _lastCheckUtc = DateTime.MinValue;
        _lastETag = null;
    }

    /// <summary>
    /// Скачивает обновление с автоматическими ретраями и докачкой,
    /// верифицирует SHA256, создаёт бэкап и применяет.
    /// </summary>
    public async Task DownloadAndApplyUpdateAsync(
        LauncherUpdateInfo info,
        IProgress<(long Downloaded, long Total, double SpeedBytesPerSec)> progress,
        CancellationToken cancel)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"ss14_launcher_upd_{Guid.NewGuid():N}" + (OperatingSystem.IsWindows() ? ".zip" : ".tar.gz"));
        var extractDir = Path.Combine(Path.GetTempPath(), $"ss14_launcher_extracted_{Guid.NewGuid():N}");

        try
        {
            // скачивание с ретраями и докачкой
            await DownloadWithResumeAsync(info.DownloadUrl, tempFile, info.SizeBytes, progress, cancel);

            // верификация SHA256
            var fileHash = await ComputeSha256Async(tempFile, cancel);
            Log.Information("Downloaded update archive SHA256: {Hash}", fileHash);

            var expectedSha = info.ExpectedSha256;
            if (string.IsNullOrEmpty(expectedSha) && !string.IsNullOrEmpty(info.ChecksumsUrl))
            {
                try
                {
                    using var checksumsReq = new HttpRequestMessage(HttpMethod.Get, info.ChecksumsUrl);
                    using var checksumsResp = await HttpClient.SendAsync(checksumsReq, cancel);
                    if (checksumsResp.IsSuccessStatusCode)
                    {
                        var sumsText = await checksumsResp.Content.ReadAsStringAsync(cancel);
                        expectedSha = ParseChecksumFromSha256Sums(sumsText, info.TargetFileName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Could not retrieve SHA256 checksums file from {Url}", info.ChecksumsUrl);
                }
            }

            if (!string.IsNullOrEmpty(expectedSha))
            {
                if (!string.Equals(fileHash, expectedSha, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Failed to delete corrupted temp file {Path}", tempFile);
                    }
                    Log.Error("SHA256 checksum mismatch! Expected: {Expected}, Computed: {Actual}. Aborting update.", expectedSha, fileHash);
                    throw new InvalidDataException($"SHA256 verification failed! Downloaded archive appears corrupted or modified. Expected {expectedSha}, but got {fileHash}.");
                }
                Log.Information("SHA256 checksum verified successfully: {Hash}", fileHash);
            }

            // распаковка
            Directory.CreateDirectory(extractDir);

            if (OperatingSystem.IsWindows())
            {
                ZipFile.ExtractToDirectory(tempFile, extractDir, overwriteFiles: true);
            }
            else
            {
                await using var archiveStream = File.OpenRead(tempFile);
                await using var gzipStream = new GZipStream(archiveStream, CompressionMode.Decompress);
                await TarFile.ExtractToDirectoryAsync(gzipStream, extractDir, overwriteFiles: true, cancel);
            }

            // обработка вложенной папки в архиве:
            // если внутри extractDir ровно одна папка и нет файлов — значит архив обёрнут,
            // переходим внутрь неё
            extractDir = UnwrapNestedDirectory(extractDir);

            // определяем куда ставить
            var installDir = ResolveInstallDirectory();

            // бэкап текущих файлов перед перезаписью
            var backupDir = Path.Combine(installDir, "_backup");
            CreateBackup(installDir, backupDir);

            // запускаем скрипт замены и рестарта
            LaunchSelfUpdaterScript(extractDir, installDir, tempFile, backupDir);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            try
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
            catch (Exception innerEx)
            {
                Log.Debug(ex, "Failed to delete temp file on error {Path}", tempFile);
            }

            try
            {
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
            }
            catch (Exception fallbackEx)
            {
                Log.Debug(ex, "Failed to delete extract directory on error {Path}", extractDir);
            }
            throw;
        }
    }

    /// <summary>
    /// Скачивает файл с поддержкой докачки (HTTP Range) и экспоненциальным бэкоффом при ошибках.
    /// </summary>
    private static async Task DownloadWithResumeAsync(
        string url,
        string destPath,
        long expectedSize,
        IProgress<(long Downloaded, long Total, double SpeedBytesPerSec)> progress,
        CancellationToken cancel)
    {
        long downloadedSoFar = 0;
        var sw = Stopwatch.StartNew();
        double smoothedSpeed = 0;
        long lastBytes = 0;
        var lastTime = sw.Elapsed;

        for (int attempt = 0; attempt < MaxDownloadRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                // если уже что-то скачали — просим сервер дослать остаток
                if (downloadedSoFar > 0 && File.Exists(destPath))
                {
                    request.Headers.Range = new RangeHeaderValue(downloadedSoFar, null);
                }

                using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel);

                // если сервер не поддерживает Range, начнём сначала
                if (downloadedSoFar > 0 && response.StatusCode != System.Net.HttpStatusCode.PartialContent)
                {
                    downloadedSoFar = 0;
                }

                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? expectedSize;
                if (downloadedSoFar > 0)
                    totalBytes += downloadedSoFar;

                await using var contentStream = await response.Content.ReadAsStreamAsync(cancel);
                var fileMode = downloadedSoFar > 0 ? FileMode.Append : FileMode.Create;
                await using var fileStream = new FileStream(destPath, fileMode, FileAccess.Write, FileShare.None, 81920, useAsync: true);

                var buffer = new byte[81920];

                while (true)
                {
                    var read = await contentStream.ReadAsync(buffer, cancel);
                    if (read == 0) break;

                    await fileStream.WriteAsync(buffer.AsMemory(0, read), cancel);
                    downloadedSoFar += read;

                    var now = sw.Elapsed;
                    var timeDiff = (now - lastTime).TotalSeconds;
                    if (timeDiff >= 0.25)
                    {
                        var bytesDiff = downloadedSoFar - lastBytes;
                        var curSpeed = bytesDiff / timeDiff;
                        smoothedSpeed = smoothedSpeed <= 0.001 ? curSpeed : (0.3 * curSpeed) + (0.7 * smoothedSpeed);
                        lastBytes = downloadedSoFar;
                        lastTime = now;
                        progress.Report((downloadedSoFar, totalBytes, smoothedSpeed));
                    }
                }

                progress.Report((downloadedSoFar, totalBytes, smoothedSpeed));
                return; // всё скачали
            }
            catch (OperationCanceledException)
            {
                throw; // отмена — не ретраим
            }
            catch (Exception ex) when (attempt < MaxDownloadRetries - 1)
            {
                // экспоненциальный бэкофф: 1с, 3с, 9с...
                var delaySec = Math.Pow(3, attempt);
                Log.Warning(ex, "Download attempt {Attempt} failed, retrying in {Delay}s...", attempt + 1, delaySec);
                await Task.Delay(TimeSpan.FromSeconds(delaySec), cancel);
            }
        }
    }

    /// <summary>
    /// Считает SHA256 скачанного файла для верификации целостности.
    /// </summary>
    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancel)
    {
        using var sha = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hash = await sha.ComputeHashAsync(stream, cancel);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Если в архиве одна корневая папка без файлов рядом — разворачиваем из неё.
    /// Часто tar.gz пакуют как SS14.Launcher_Linux/bin/... — нам нужно содержимое.
    /// </summary>
    private static string UnwrapNestedDirectory(string extractDir)
    {
        var entries = Directory.GetFileSystemEntries(extractDir);
        if (entries.Length == 1 && Directory.Exists(entries[0]))
        {
            Log.Information("Archive has a single nested directory: {Dir}, unwrapping.", Path.GetFileName(entries[0]));
            return entries[0];
        }
        return extractDir;
    }

    /// <summary>
    /// Определяет директорию установки лаунчера.
    /// </summary>
    private static string ResolveInstallDirectory()
    {
        var installDir = AppContext.BaseDirectory;
        if (File.Exists(Path.Combine(installDir, "SS14.Launcher")) || File.Exists(Path.Combine(installDir, "SS14.Launcher.exe")))
        {
            return installDir;
        }

        var parent = Directory.GetParent(installDir)?.FullName;
        if (!string.IsNullOrEmpty(parent) && (File.Exists(Path.Combine(parent, "SS14.Launcher")) || File.Exists(Path.Combine(parent, "SS14.Launcher.exe"))))
        {
            return parent;
        }

        return installDir;
    }

    /// <summary>
    /// Создаёт бэкап текущих бинарников перед перезаписью.
    /// Копирует только dll/exe/json конфиги, без контента серверов.
    /// </summary>
    private static void CreateBackup(string installDir, string backupDir)
    {
        try
        {
            // чистим старый бэкап если есть
            if (Directory.Exists(backupDir))
                Directory.Delete(backupDir, true);

            Directory.CreateDirectory(backupDir);

            // копируем основные файлы лаунчера
            var criticalExtensions = new[] { ".dll", ".exe", ".json", ".deps" };
            var mainBinary = OperatingSystem.IsWindows() ? "SS14.Launcher.exe" : "SS14.Launcher";

            foreach (var file in Directory.GetFiles(installDir))
            {
                var name = Path.GetFileName(file);
                var ext = Path.GetExtension(file).ToLowerInvariant();

                // бинарь лаунчера + dll + json конфиги
                if (name == mainBinary || criticalExtensions.Any(e => ext.Contains(e)))
                {
                    File.Copy(file, Path.Combine(backupDir, name), overwrite: true);
                }
            }

            Log.Information("Backup of {Count} files created in {BackupDir}",
                Directory.GetFiles(backupDir).Length, backupDir);
        }
        catch (Exception ex)
        {
            // бэкап не критичен, не ломаем процесс из-за него
            Log.Warning(ex, "Failed to create backup before update, continuing anyway.");
        }
    }

    private static void LaunchSelfUpdaterScript(string extractDir, string installDir, string tempArchive, string backupDir)
    {
        var pid = Environment.ProcessId;

        if (OperatingSystem.IsWindows())
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), $"ss14_updater_{pid}.cmd");
            var spaceExe = Path.Combine(installDir, "Space Station 14 Launcher.exe");
            var binExe = Path.Combine(installDir, "bin_x64", "SS14.Launcher.exe");
            var rootExe = Path.Combine(installDir, "SS14.Launcher.exe");

            var script = $@"@echo off
:waitpid
tasklist /fi ""PID eq {pid}"" 2>nul | find ""{pid}"" >nul
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto waitpid
)
xcopy /s /e /y /q ""{extractDir}\*"" ""{installDir}\""
if errorlevel 1 (
    if exist ""{backupDir}"" xcopy /s /e /y /q ""{backupDir}\*"" ""{installDir}\""
)
if exist ""{spaceExe}"" (
    start """" ""{spaceExe}""
) else if exist ""{binExe}"" (
    start """" ""{binExe}""
) else (
    start """" ""{rootExe}""
)
rd /s /q ""{extractDir}""
del /f /q ""{tempArchive}""
del /f /q ""%~f0""
";
            File.WriteAllText(scriptPath, script);
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{scriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
        else
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), $"ss14_updater_{pid}.sh");
            var script = $@"#!/bin/sh
while kill -0 {pid} 2>/dev/null; do sleep 0.2; done
if ! cp -rf ""{extractDir}/""* ""{installDir}/""; then
    echo ""Update failed, rolling back..."" >&2
    if [ -d ""{backupDir}"" ]; then
        cp -rf ""{backupDir}/""* ""{installDir}/""
    fi
fi
chmod +x ""{Path.Combine(installDir, "SS14.Launcher")}"" 2>/dev/null || true
chmod +x ""{Path.Combine(installDir, "setup-desktop.sh")}"" 2>/dev/null || true
chmod +x ""{Path.Combine(installDir, "bin_x64/loader/SS14.Loader")}"" 2>/dev/null || true
chmod +x ""{Path.Combine(installDir, "loader/SS14.Loader")}"" 2>/dev/null || true
chmod +x ""{Path.Combine(installDir, "dotnet_x64/dotnet")}"" 2>/dev/null || true
rm -rf ""{extractDir}"" ""{tempArchive}""
nohup ""{Path.Combine(installDir, "SS14.Launcher")}"" >/dev/null 2>&1 &
rm -f ""$0""
";
            File.WriteAllText(scriptPath, script);
            try
            {
                File.SetUnixFileMode(scriptPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            catch (Exception ex)
            {
                // fallback
            }

            var psi = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = scriptPath,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
    }

    /// <summary>
    /// Parses a semantic/numerical version string into a <see cref="Version"/> object,
    /// stripping prefixes like 'v', 'V' and prerelease tags like '-beta'.
    /// </summary>
    public static bool TryParseVersion(string versionStr, out Version version)
    {
        versionStr = versionStr.Trim().TrimStart('v', 'V');
        var dash = versionStr.IndexOf('-');
        if (dash >= 0)
            versionStr = versionStr.Substring(0, dash);

        var parts = versionStr.Split('.');
        if (parts.Length == 1)
            versionStr += ".0";

        return Version.TryParse(versionStr, out version!);
    }

    /// <summary>
    /// Extracts a 64-character hex SHA-256 hash for the given filename from release notes or body text.
    /// </summary>
    public static string? ExtractSha256FromBody(string body, string fileName)
    {
        if (string.IsNullOrWhiteSpace(body) || string.IsNullOrWhiteSpace(fileName))
            return null;

        var lines = body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.Contains(fileName, StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"\b([a-fA-F0-9]{64})\b");
                if (match.Success)
                    return match.Groups[1].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Parses a standard SHA256SUMS format file (hash  filename) to locate the hash for a specific file.
    /// </summary>
    public static string? ParseChecksumFromSha256Sums(string sumsContent, string fileName)
    {
        if (string.IsNullOrWhiteSpace(sumsContent) || string.IsNullOrWhiteSpace(fileName))
            return null;

        var lines = sumsContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"^([a-fA-F0-9]{64})");
                if (match.Success)
                    return match.Groups[1].Value;
            }
        }

        return null;
    }
}

public sealed record LauncherUpdateInfo(
    string Version,
    string TagName,
    string Title,
    string ReleaseNotes,
    string DownloadUrl,
    long SizeBytes,
    string TargetFileName = "",
    string? ExpectedSha256 = null,
    string? ChecksumsUrl = null);

