using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Serilog;
using SkiaSharp;

namespace SS14.Launcher.Utility;

public sealed class AnimatedBackgroundManager : IDisposable
{
    private CancellationTokenSource? _cts;
    private Process? _ffmpegProcess;
    private readonly List<(Bitmap Frame, int Duration)> _preloadedFrames = new();
    private WriteableBitmap? _videoFrameA;
    private WriteableBitmap? _videoFrameB;
    private bool _useFrameA;
    private bool _disposed;

    public Bitmap? CurrentFrame { get; private set; }
    public bool IsAnimated { get; private set; }
    public event Action<Bitmap?>? FrameUpdated;

    public void Load(string? filePath)
    {
        Stop();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            CurrentFrame = null;
            IsAnimated = false;
            FrameUpdated?.Invoke(null);
            return;
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        try
        {
            if (ext is ".mp4" or ".webm" or ".mkv" or ".avi" or ".mov" or ".flv" or ".wmv")
            {
                StartVideoPlayback(filePath);
            }
            else
            {
                StartSkiaPlayback(filePath);
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to initialize background from {Path}: {Message}", filePath, ex.Message);
            try
            {
                CurrentFrame = new Bitmap(filePath);
                IsAnimated = false;
                FrameUpdated?.Invoke(CurrentFrame);
            }
            catch
            {
                CurrentFrame = null;
                IsAnimated = false;
                FrameUpdated?.Invoke(null);
            }
        }
    }

    private void StartSkiaPlayback(string filePath)
    {
        byte[] fileBytes;
        try
        {
            fileBytes = File.ReadAllBytes(filePath);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Cannot read background file {Path}", filePath);
            CurrentFrame = null;
            IsAnimated = false;
            FrameUpdated?.Invoke(null);
            return;
        }

        using var memoryStream = new MemoryStream(fileBytes);
        using var codec = SKCodec.Create(memoryStream);

        if (codec == null)
        {
            CurrentFrame = new Bitmap(new MemoryStream(fileBytes));
            IsAnimated = false;
            FrameUpdated?.Invoke(CurrentFrame);
            return;
        }

        var frameCount = codec.FrameCount;
        var info = codec.Info;

        if (frameCount <= 1)
        {
            CurrentFrame = new Bitmap(new MemoryStream(fileBytes));
            IsAnimated = false;
            FrameUpdated?.Invoke(CurrentFrame);
            return;
        }

        _preloadedFrames.Clear();

        var imageInfo = new SKImageInfo(info.Width, info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvasBitmap = new SKBitmap(imageInfo);
        using var canvas = new SKCanvas(canvasBitmap);
        var frameInfos = codec.FrameInfo;

        for (int i = 0; i < frameCount; i++)
        {
            var reqFrame = (frameInfos != null && i < frameInfos.Length) ? frameInfos[i].RequiredFrame : -1;
            var options = new SKCodecOptions(i, reqFrame);

            using var tempBitmap = new SKBitmap(imageInfo);
            var result = codec.GetPixels(imageInfo, tempBitmap.GetPixels(), options);

            if (reqFrame == -1)
            {
                canvas.Clear(SKColors.Transparent);
            }
            canvas.DrawBitmap(tempBitmap, 0, 0);

            using var image = SKImage.FromBitmap(canvasBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var frameMs = new MemoryStream(data.ToArray());
            var avaloniaBitmap = new Bitmap(frameMs);

            var duration = (frameInfos != null && i < frameInfos.Length) ? frameInfos[i].Duration : 100;
            if (duration <= 15) duration = 100;

            _preloadedFrames.Add((avaloniaBitmap, duration));
        }

        if (_preloadedFrames.Count == 0)
        {
            CurrentFrame = new Bitmap(new MemoryStream(fileBytes));
            IsAnimated = false;
            FrameUpdated?.Invoke(CurrentFrame);
            return;
        }

        IsAnimated = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        CurrentFrame = _preloadedFrames[0].Frame;
        FrameUpdated?.Invoke(CurrentFrame);

        Task.Run(async () =>
        {
            int idx = 0;
            while (!token.IsCancellationRequested)
            {
                var current = _preloadedFrames[idx];
                var delay = current.Duration;

                Dispatcher.UIThread.Post(() =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        CurrentFrame = current.Frame;
                        FrameUpdated?.Invoke(CurrentFrame);
                    }
                }, DispatcherPriority.Render);

                try
                {
                    await Task.Delay(delay, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                idx = (idx + 1) % _preloadedFrames.Count;
            }
        }, token);
    }

    private void StartVideoPlayback(string filePath)
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        const int targetWidth = 1280;
        const int targetHeight = 720;
        const int frameBytes = targetWidth * targetHeight * 4;

        _videoFrameA = new WriteableBitmap(
            new PixelSize(targetWidth, targetHeight),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        _videoFrameB = new WriteableBitmap(
            new PixelSize(targetWidth, targetHeight),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        _useFrameA = true;
        CurrentFrame = _videoFrameA;
        IsAnimated = true;
        FrameUpdated?.Invoke(CurrentFrame);

        Task.Run(async () =>
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-re -stream_loop -1 -i \"{filePath}\" -f rawvideo -pix_fmt bgra -s {targetWidth}x{targetHeight} -r 30 -an -v quiet -",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                _ffmpegProcess = Process.Start(startInfo);
                if (_ffmpegProcess == null)
                {
                    Log.Warning("ffmpeg not available on system for background video playback");
                    return;
                }

                var stdout = _ffmpegProcess.StandardOutput.BaseStream;
                var buffer = new byte[frameBytes];

                while (!token.IsCancellationRequested && !_ffmpegProcess.HasExited)
                {
                    var bytesRead = 0;
                    while (bytesRead < frameBytes)
                    {
                        var read = await stdout.ReadAsync(buffer.AsMemory(bytesRead, frameBytes - bytesRead), token);
                        if (read <= 0) break;
                        bytesRead += read;
                    }

                    if (bytesRead < frameBytes)
                        break;

                    var targetBmp = _useFrameA ? _videoFrameA : _videoFrameB;
                    if (targetBmp == null) break;

                    unsafe
                    {
                        using var frameLock = targetBmp.Lock();
                        fixed (byte* src = buffer)
                        {
                            Buffer.MemoryCopy(src, (void*)frameLock.Address, frameBytes, frameBytes);
                        }
                    }

                    _useFrameA = !_useFrameA;

                    Dispatcher.UIThread.Post(() =>
                    {
                        if (!token.IsCancellationRequested)
                        {
                            CurrentFrame = targetBmp;
                            FrameUpdated?.Invoke(CurrentFrame);
                        }
                    }, DispatcherPriority.Render);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Log.Debug("Video background playback stopped: {Message}", ex.Message);
            }
            finally
            {
                try
                {
                    if (_ffmpegProcess is { HasExited: false })
                    {
                        _ffmpegProcess.Kill();
                    }
                    _ffmpegProcess?.Dispose();
                    _ffmpegProcess = null;
                }
                catch
                {
                }
            }
        }, token);
    }

    public void Stop()
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            if (_ffmpegProcess is { HasExited: false })
            {
                _ffmpegProcess.Kill();
            }
            _ffmpegProcess?.Dispose();
            _ffmpegProcess = null;

            _preloadedFrames.Clear();
            _videoFrameA = null;
            _videoFrameB = null;
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
