using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
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
    string FormattedProgress);

public enum ReplayProviderPreset
{
    SpaceStories = 0,
    CustomTemplate = 1
}

public sealed class SpaceStoriesSearchResponse
{
    [JsonPropertyName("found")]
    public bool Found { get; set; }

    [JsonPropertyName("server_name")]
    public string? ServerName { get; set; }

    [JsonPropertyName("filename")]
    public string? FileName { get; set; }
}

public static class ReplayDownloader
{
    public static string NormalizeDownloadUrl(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return "";

        var url = rawUrl.Trim().TrimEnd('?');

        // Space Stories frontend page to direct download API:
        // https://spacestories.club/replays/{server}/{filename.zip} -> https://spacestories.club/replays/api/download/{server}/{filename.zip}
        var match = Regex.Match(url, @"^https?://(?:www\.)?spacestories\.club/replays/(?!api/download/)([a-zA-Z0-9_\-]+)/([^/?#]+\.zip)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var server = match.Groups[1].Value;
            var file = match.Groups[2].Value;
            return $"https://spacestories.club/replays/api/download/{server}/{file}";
        }

        return url;
    }

    public static async Task<string> ResolveUrlFromRoundIdAsync(
        string roundId,
        ReplayProviderPreset preset,
        string? customTemplate = null,
        HttpClient? customClient = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roundId))
            throw new ArgumentException("Round ID cannot be empty", nameof(roundId));

        var cleanId = roundId.Trim().TrimStart('#');
        if (string.IsNullOrWhiteSpace(cleanId))
            throw new ArgumentException("Round ID cannot be empty", nameof(roundId));

        if (preset == ReplayProviderPreset.SpaceStories)
        {
            var client = customClient ?? Locator.Current.GetService<HttpClient>() ?? new HttpClient();
            var searchUrl = $"https://spacestories.club/replays/api/search?round_id={Uri.EscapeDataString(cleanId)}";

            using var response = await client.GetAsync(searchUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var searchResult = await response.Content.ReadFromJsonAsync<SpaceStoriesSearchResponse>(cancellationToken: cancellationToken);
            if (searchResult == null || !searchResult.Found || string.IsNullOrWhiteSpace(searchResult.ServerName) || string.IsNullOrWhiteSpace(searchResult.FileName))
            {
                throw new InvalidOperationException($"Раунд #{cleanId} не найден в архиве Space Stories.");
            }

            return $"https://spacestories.club/replays/api/download/{searchResult.ServerName}/{searchResult.FileName}";
        }

        if (preset == ReplayProviderPreset.CustomTemplate)
        {
            if (string.IsNullOrWhiteSpace(customTemplate) || !customTemplate.Contains("{roundId}", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Шаблон должен содержать маркер {roundId}");

            return customTemplate.Replace("{roundId}", cleanId, StringComparison.OrdinalIgnoreCase);
        }

        throw new ArgumentException("Неизвестный провайдер записей.");
    }

    public static string BuildUrlFromRoundId(string roundId, ReplayProviderPreset preset, string? customTemplate = null)
    {
        if (string.IsNullOrWhiteSpace(roundId))
            throw new ArgumentException("Round ID cannot be empty", nameof(roundId));

        var cleanId = roundId.Trim().TrimStart('#');
        if (string.IsNullOrWhiteSpace(cleanId))
            throw new ArgumentException("Round ID cannot be empty", nameof(roundId));

        return preset switch
        {
            ReplayProviderPreset.CustomTemplate when !string.IsNullOrWhiteSpace(customTemplate) && customTemplate.Contains("{roundId}", StringComparison.OrdinalIgnoreCase) =>
                customTemplate.Replace("{roundId}", cleanId, StringComparison.OrdinalIgnoreCase),
            _ => throw new ArgumentException("Provider requires async resolution or invalid custom template")
        };
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

                // If it's a spacestories link that wasn't normalized, try api download
                var ssMatch = Regex.Match(url, @"spacestories\.club/replays/([a-zA-Z0-9_\-]+)/([^/?#]+\.zip)", RegexOptions.IgnoreCase);
                if (ssMatch.Success && !url.Contains("/api/download/"))
                {
                    var convertedUrl = $"https://spacestories.club/replays/api/download/{ssMatch.Groups[1].Value}/{ssMatch.Groups[2].Value}";
                    return await DownloadReplayAsync(convertedUrl, targetDirectory, progress, client, cancellationToken);
                }

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
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920];
                long totalDownloaded = 0;
                var stopwatch = Stopwatch.StartNew();
                var lastReportTime = TimeSpan.Zero;
                long bytesSinceLastReport = 0;
                double currentSpeed = 0;

                while (true)
                {
                    var read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                    if (read == 0)
                        break;

                    await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    totalDownloaded += read;
                    bytesSinceLastReport += read;

                    var elapsed = stopwatch.Elapsed;
                    if (elapsed - lastReportTime >= TimeSpan.FromMilliseconds(200))
                    {
                        var timeDiff = (elapsed - lastReportTime).TotalSeconds;
                        if (timeDiff > 0)
                        {
                            currentSpeed = bytesSinceLastReport / timeDiff;
                        }
                        lastReportTime = elapsed;
                        bytesSinceLastReport = 0;

                        double percent = totalBytes.HasValue && totalBytes.Value > 0
                            ? Math.Clamp((double)totalDownloaded / totalBytes.Value * 100.0, 0.0, 100.0)
                            : 0;

                        var formatted = totalBytes.HasValue
                            ? $"{StorageAnalyzer.FormatBytes(totalDownloaded)} / {StorageAnalyzer.FormatBytes(totalBytes.Value)} ({StorageAnalyzer.FormatBytes((long)currentSpeed)}/s)"
                            : $"{StorageAnalyzer.FormatBytes(totalDownloaded)} ({StorageAnalyzer.FormatBytes((long)currentSpeed)}/s)";

                        progress?.Report(new ReplayDownloadProgress(totalDownloaded, totalBytes, percent, currentSpeed, formatted));
                    }
                }

                // Final report
                var finalPercent = totalBytes.HasValue && totalBytes.Value > 0 ? 100.0 : 0.0;
                var finalFormatted = totalBytes.HasValue
                    ? $"{StorageAnalyzer.FormatBytes(totalDownloaded)} / {StorageAnalyzer.FormatBytes(totalBytes.Value)}"
                    : StorageAnalyzer.FormatBytes(totalDownloaded);

                progress?.Report(new ReplayDownloadProgress(totalDownloaded, totalBytes, finalPercent, currentSpeed, finalFormatted));
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
}
