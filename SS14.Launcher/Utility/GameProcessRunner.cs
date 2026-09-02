using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace SS14.Launcher.Utility;

/// <summary>
/// Handles low-level execution, process priority management, and asynchronous I/O piping
/// for launched Space Station 14 game instances.
/// </summary>
public static class GameProcessRunner
{
    /// <summary>
    /// Spawns the game process from the given ProcessStartInfo and configures priority and log redirection.
    /// </summary>
    public static Process? StartGameProcess(ProcessStartInfo startInfo, bool highPriority = false)
    {
        var process = Process.Start(startInfo);
        if (process == null)
            return null;

        if (highPriority)
        {
            try
            {
                process.PriorityClass = ProcessPriorityClass.High;
                Log.Information("Game process priority elevated to High (PID: {PID}).", process.Id);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to set process priority to High.");
            }
        }

        if (startInfo.RedirectStandardOutput && startInfo.RedirectStandardError)
        {
            var fileStdout = new FileStream(
                LauncherPaths.PathClientStdoutLog,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Delete | FileShare.ReadWrite,
                0,
                FileOptions.Asynchronous);

            var fileStderr = new FileStream(
                LauncherPaths.PathClientStderrLog,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Delete | FileShare.ReadWrite,
                0,
                FileOptions.Asynchronous);

            _ = PipeOutputAsync(process, fileStdout, fileStderr);
        }

        return process;
    }

    /// <summary>
    /// Backward-compatible fire-and-forget pipe output invocation.
    /// </summary>
    public static void PipeOutput(Process process, Stream targetStdout, Stream targetStderr)
    {
        _ = PipeOutputAsync(process, targetStdout, targetStderr);
    }

    /// <summary>
    /// Asynchronously pipes standard output and standard error from the game process to target log streams.
    /// </summary>
    public static async Task PipeOutputAsync(Process process, Stream targetStdout, Stream targetStderr)
    {
        int pid = 0;
        try
        {
            pid = process.Id;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to retrieve game process ID for pipe logging.");
        }

        async Task DoPipe(StreamReader reader, Stream writer)
        {
            var buf = ArrayPool<byte>.Shared.Rent(8192);
            try
            {
                var readStream = reader.BaseStream;
                while (true)
                {
                    var read = await readStream.ReadAsync(buf.AsMemory(0, buf.Length));
                    if (read == 0)
                    {
                        Log.Debug("EOF, ending pipe logging for {pid}.", pid);
                        return;
                    }

                    await writer.WriteAsync(buf.AsMemory(0, read));
                    await writer.FlushAsync();
                }
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                Log.Debug("Pipe ended with {Exception} for {pid}.", ex.GetType().Name, pid);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buf);
            }
        }

        try
        {
            await Task.WhenAll(
                DoPipe(process.StandardOutput, targetStdout),
                DoPipe(process.StandardError, targetStderr));
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Exception in output pipe for {pid}", pid);
        }
        finally
        {
            try
            {
                await targetStdout.DisposeAsync();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to cleanly dispose targetStdout stream.");
            }

            try
            {
                await targetStderr.DisposeAsync();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Failed to cleanly dispose targetStderr stream.");
            }
        }
    }
}
