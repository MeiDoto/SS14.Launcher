using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace SS14.Launcher.Utility;

/// <summary>
/// Lightweight, native cross-platform Discord Rich Presence (RPC) client.
/// Connects to local Discord IPC via Named Pipes on Windows or Unix Domain Sockets on Linux/macOS.
/// Zero external dependencies, fully resilient to Discord start/exit.
/// </summary>
public sealed class DiscordRpcClient : IDisposable
{
    private const int OpcodeHandshake = 0;
    private const int OpcodeFrame = 1;
    private const int OpcodeClose = 2;

    public static readonly DiscordRpcClient Instance = new();

    private Stream? _stream;
    private IDisposable? _underlyingDisposable;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string _clientId = "524733878418046997"; // Space Station 14 default App ID
    private bool _connected;
    private DateTimeOffset _sessionStart = DateTimeOffset.UtcNow;

    public bool IsConnected => _connected;

    public async Task InitializeAsync(string? clientId = null)
    {
        if (!string.IsNullOrWhiteSpace(clientId))
            _clientId = clientId;

        _sessionStart = DateTimeOffset.UtcNow;
        await EnsureConnectedAsync();
    }

    public async Task UpdatePresenceAsync(string details, string state, DateTimeOffset? startTime = null)
    {
        try
        {
            if (!await EnsureConnectedAsync())
                return;

            startTime ??= _sessionStart;
            var startUnix = startTime.Value.ToUnixTimeSeconds();

            var payload = new
            {
                cmd = "SET_ACTIVITY",
                args = new
                {
                    pid = Environment.ProcessId,
                    activity = new
                    {
                        details,
                        state,
                        timestamps = new
                        {
                            start = startUnix
                        },
                        assets = new
                        {
                            large_image = "icon",
                            large_text = "Space Station 14"
                        }
                    }
                },
                nonce = Guid.NewGuid().ToString("N")
            };

            var json = JsonSerializer.Serialize(payload);
            await SendPacketAsync(OpcodeFrame, json);
        }
        catch (Exception ex)
        {
            Log.Verbose("Failed to update Discord presence: {Message}", ex.Message);
            Disconnect();
        }
    }

    public async Task ClearPresenceAsync()
    {
        try
        {
            if (!_connected)
                return;

            var payload = new
            {
                cmd = "SET_ACTIVITY",
                args = new
                {
                    pid = Environment.ProcessId,
                    activity = (object?)null
                },
                nonce = Guid.NewGuid().ToString("N")
            };

            var json = JsonSerializer.Serialize(payload);
            await SendPacketAsync(OpcodeFrame, json);
        }
        catch
        {
            Disconnect();
        }
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        if (_connected && _stream != null)
            return true;

        await _lock.WaitAsync();
        try
        {
            if (_connected && _stream != null)
                return true;

            Disconnect();

            for (var i = 0; i < 10; i++)
            {
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        var pipe = new NamedPipeClientStream(".", $"discord-ipc-{i}", PipeDirection.InOut, PipeOptions.Asynchronous);
                        await pipe.ConnectAsync(100);
                        _stream = pipe;
                        _underlyingDisposable = pipe;
                    }
                    else
                    {
                        var socketPath = GetUnixSocketPath(i);
                        if (string.IsNullOrEmpty(socketPath) || !File.Exists(socketPath))
                            continue;

                        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                        var endpoint = new UnixDomainSocketEndPoint(socketPath);
                        await socket.ConnectAsync(endpoint);
                        var netStream = new NetworkStream(socket, ownsSocket: true);
                        _stream = netStream;
                        _underlyingDisposable = netStream;
                    }

                    // Handshake
                    var handshakePayload = JsonSerializer.Serialize(new { v = 1, client_id = _clientId });
                    await SendPacketAsync(OpcodeHandshake, handshakePayload);

                    // Read handshake response
                    var respHeader = new byte[8];
                    var read = await _stream.ReadAsync(respHeader);
                    if (read == 8)
                    {
                        var len = BitConverter.ToInt32(respHeader, 4);
                        if (len > 0 && len < 65536)
                        {
                            var buf = new byte[len];
                            await _stream.ReadAsync(buf);
                        }
                    }

                    _connected = true;
                    Log.Debug("Connected to Discord RPC on slot {Slot}", i);
                    return true;
                }
                catch
                {
                    Disconnect();
                }
            }

            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string? GetUnixSocketPath(int index)
    {
        var xdgRuntime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        if (!string.IsNullOrEmpty(xdgRuntime))
        {
            var p = Path.Combine(xdgRuntime, $"discord-ipc-{index}");
            if (File.Exists(p))
                return p;
        }

        var tmp = Environment.GetEnvironmentVariable("TMPDIR") ?? "/tmp";
        var candidate = Path.Combine(tmp, $"discord-ipc-{index}");
        if (File.Exists(candidate))
            return candidate;

        return null;
    }

    private async Task SendPacketAsync(int opcode, string json)
    {
        if (_stream == null)
            return;

        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var header = new byte[8];
        BitConverter.TryWriteBytes(header.AsSpan(0, 4), opcode);
        BitConverter.TryWriteBytes(header.AsSpan(4, 4), jsonBytes.Length);

        await _stream.WriteAsync(header);
        await _stream.WriteAsync(jsonBytes);
        await _stream.FlushAsync();
    }

    private void Disconnect()
    {
        _connected = false;
        try
        {
            _stream?.Dispose();
        }
        catch { }
        try
        {
            _underlyingDisposable?.Dispose();
        }
        catch { }

        _stream = null;
        _underlyingDisposable = null;
    }

    public void Dispose()
    {
        Disconnect();
        _lock.Dispose();
    }
}
