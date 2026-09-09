using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Splat;
using SS14.Launcher.Utility;

namespace SS14.Launcher.Models.ContentManagement;

public readonly record struct ReplayDownloadProgress(
    long BytesDownloaded,
    long? TotalBytes,
    double ProgressPercentage,
    double SpeedBytesPerSecond,
    string FormattedProgress,
    TimeSpan? EstimatedRemaining = null);

public static class ReplayDownloader
{
    public static string NormalizeDownloadUrl(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return "";

        return rawUrl.Trim().TrimEnd('?');
    }

    public static string BuildUrlFromTemplate(string roundId, string template)
    {
        if (string.IsNullOrWhiteSpace(roundId))
            throw new ArgumentException("Номер раунда не может быть пустым.", nameof(roundId));

        var cleanId = roundId.Trim().TrimStart('#');
        if (string.IsNullOrWhiteSpace(cleanId))
            throw new ArgumentException("Номер раунда не может быть пустым.", nameof(roundId));

        if (string.IsNullOrWhiteSpace(template) || !template.Contains("{roundId}", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Шаблон ссылки должен содержать маркер {roundId}.", nameof(template));

        return template.Replace("{roundId}", cleanId, StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<string> DownloadReplayAsync(
        string url,
        string targetDirectory,
        IProgress<ReplayDownloadProgress>? progress = null,
        HttpClient? customClient = null,
        CancellationToken cancellationToken = default)
    {
        var client = customClient ?? Locator.Current.GetService<HttpClient>() ?? new HttpClient();

        url = NormalizeDownloadUrl(url);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Invalid download URL. Must be an HTTP or HTTPS address.");
        }

        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        // Determine destination file name
        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            fileName = $"replay_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
        }

        // Avoid overwriting existing files
        var destinationPath = Path.Combine(targetDirectory, fileName);
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var counter = 1;
        while (File.Exists(destinationPath))
        {
            destinationPath = Path.Combine(targetDirectory, $"{baseName}_{counter}.zip");
            counter++;
        }

        var tempPath = destinationPath + ".downloading";

        try
        {
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Detect if response is HTML webpage instead of binary zip
            var mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            if (mediaType == "text/html" || mediaType == "text/plain")
            {
                var htmlContent = await response.Content.ReadAsStringAsync(cancellationToken);

                // Check for a .zip link inside the HTML
                var zipMatch = Regex.Match(htmlContent, @"href=[""']([^""']+\.zip[^""']*)[""']", RegexOptions.IgnoreCase);
                if (zipMatch.Success)
                {
                    var extractedUrl = zipMatch.Groups[1].Value;
                    if (!extractedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !extractedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        extractedUrl = new Uri(uri, extractedUrl).ToString();
                    }

                    return await DownloadReplayAsync(extractedUrl, targetDirectory, progress, client, cancellationToken);
                }

                throw new InvalidDataException("Указанная ссылка ведёт на веб-страницу (HTML), а не на файл архива записи (.zip).");
            }

            var totalBytes = response.Content.Headers.ContentLength;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            const int bufferSize = 262144; // 256 KB high-performance buffer
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
            {
                var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                try
                {
                    long totalDownloaded = 0;
                    var stopwatch = Stopwatch.StartNew();
                    var lastReportTime = TimeSpan.Zero;
                    long bytesSinceLastReport = 0;
                    double smoothedSpeed = 0;

                    while (true)
                    {
                        var read = await contentStream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken);
                        if (read == 0)
                            break;

                        await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                        totalDownloaded += read;
                        bytesSinceLastReport += read;

                        var elapsed = stopwatch.Elapsed;
                        // Throttle UI reports to every 120ms to avoid overloading the Avalonia dispatcher
                        if (elapsed - lastReportTime >= TimeSpan.FromMilliseconds(120))
                        {
                            var timeDiff = (elapsed - lastReportTime).TotalSeconds;
                            if (timeDiff > 0)
                            {
                                var instantSpeed = bytesSinceLastReport / timeDiff;
                                smoothedSpeed = smoothedSpeed <= 0 ? instantSpeed : (smoothedSpeed * 0.65 + instantSpeed * 0.35);
                            }
                            lastReportTime = elapsed;
                            bytesSinceLastReport = 0;

                            double percent = totalBytes.HasValue && totalBytes.Value > 0
                                ? Math.Clamp((double)totalDownloaded / totalBytes.Value * 100.0, 0.0, 100.0)
                                : 0;

                            TimeSpan? eta = null;
                            if (totalBytes.HasValue && totalBytes.Value > totalDownloaded && smoothedSpeed > 0)
                            {
                                var remainingSecs = (totalBytes.Value - totalDownloaded) / smoothedSpeed;
                                if (remainingSecs >= 0 && remainingSecs < 86400)
                                    eta = TimeSpan.FromSeconds(remainingSecs);
                            }

                            string formatted;
                            if (totalBytes.HasValue)
                            {
                                var etaString = FormatEta(eta);
                                var etaPart = !string.IsNullOrEmpty(etaString) ? $", {etaString}" : "";
                                formatted = $"{StorageAnalyzer.FormatBytes(totalDownloaded)} / {StorageAnalyzer.FormatBytes(totalBytes.Value)} ({StorageAnalyzer.FormatBytes((long)smoothedSpeed)}/s{etaPart})";
                            }
                            else
                            {
                                formatted = $"{StorageAnalyzer.FormatBytes(totalDownloaded)} ({StorageAnalyzer.FormatBytes((long)smoothedSpeed)}/s)";
                            }

                            progress?.Report(new ReplayDownloadProgress(totalDownloaded, totalBytes, percent, smoothedSpeed, formatted, eta));
                        }
                    }

                    // Final report (100%)
                    var finalPercent = totalBytes.HasValue && totalBytes.Value > 0 ? 100.0 : 0.0;
                    var finalFormatted = totalBytes.HasValue
                        ? $"{StorageAnalyzer.FormatBytes(totalDownloaded)} / {StorageAnalyzer.FormatBytes(totalBytes.Value)}"
                        : StorageAnalyzer.FormatBytes(totalDownloaded);

                    progress?.Report(new ReplayDownloadProgress(totalDownloaded, totalBytes, finalPercent, smoothedSpeed, finalFormatted));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            // Verify that tempPath is a valid zip archive
            try
            {
                using var archive = ZipFile.OpenRead(tempPath);
                if (archive.Entries.Count == 0)
                {
                    throw new InvalidDataException("Downloaded archive is empty");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Downloaded file is not a valid zip archive", ex);
            }

            // Move into final destination
            File.Move(tempPath, destinationPath, overwrite: true);

            // Populate metadata cache
            try
            {
                await ReplayMetadataCache.Instance.GetOrUpdateAsync(destinationPath, cancellationToken);
                await ReplayMetadataCache.Instance.SaveAsync();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to update metadata cache for downloaded replay {Path}", destinationPath);
            }

            Log.Information("Successfully downloaded replay from {Url} to {Path}", url, destinationPath);
            return destinationPath;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Ignored
                }
            }
        }
    }

    public static string FormatEta(TimeSpan? eta)
    {
        if (!eta.HasValue)
            return "";

        var totalSecs = (int)Math.Round(eta.Value.TotalSeconds);
        if (totalSecs <= 0)
            return "";
        if (totalSecs < 60)
            return $"~{totalSecs} с.";
        if (totalSecs < 3600)
            return $"~{totalSecs / 60} мин. {totalSecs % 60:D2} с.";
        return $"~{totalSecs / 3600} ч. {(totalSecs % 3600) / 60} мин.";
    }
}
