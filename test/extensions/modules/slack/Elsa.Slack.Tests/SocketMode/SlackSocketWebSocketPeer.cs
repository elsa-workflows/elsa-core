using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

namespace Elsa.Slack.Tests.SocketMode;

// Real loopback HTTP upgrade and WebSocket frames. No provider endpoint, SDK auto-ACK or mocked socket.
internal sealed class SlackSocketWebSocketPeer : IAsyncDisposable
{
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _shutdown = new(TimeSpan.FromSeconds(30));
    private readonly ConcurrentBag<SocketSession> _sessions = new();
    private readonly Channel<SocketSession> _accepted = Channel.CreateBounded<SocketSession>(4);
    private readonly Task _accepting;
    private int _acceptedCount;
    private bool _failed;

    internal SlackSocketWebSocketPeer()
    {
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        SocketEndpoint = new Uri($"ws://127.0.0.1:{port}/socket");
        ApiEndpoint = new Uri($"http://127.0.0.1:{port}/api/apps.connections.open");
        HttpSocketEndpoint = new Uri($"http://127.0.0.1:{port}/socket");
        _accepting = AcceptConnectionsAsync();
    }

    internal Uri SocketEndpoint { get; }
    internal Uri ApiEndpoint { get; }
    internal Uri HttpSocketEndpoint { get; }
    internal int AcceptedCount => Volatile.Read(ref _acceptedCount);
    internal Task<SocketSession> AcceptAsync() => _accepted.Reader.ReadAsync(_shutdown.Token).AsTask().WaitAsync(Deadline);

    private async Task AcceptConnectionsAsync()
    {
        try
        {
            while (!_shutdown.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_shutdown.Token);
                var transferred = false;
                try
                {
                    if (Interlocked.Increment(ref _acceptedCount) > 4)
                    {
                        throw new InvalidDataException("Fixture connection count exceeded its bound.");
                    }
                    var stream = client.GetStream();
                    var key = await ReadUpgradeKeyAsync(stream, _shutdown.Token);
                    // SHA-1 is the RFC 6455 handshake algorithm, not a credential-storage choice.
                    var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
                    var response = "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: " + accept + "\r\n\r\n";
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(response), _shutdown.Token);
                    await stream.FlushAsync(_shutdown.Token);
                    var socket = WebSocket.CreateFromStream(stream, true, null, Timeout.InfiniteTimeSpan);
                    var session = new SocketSession(client, socket, _shutdown.Token);
                    _sessions.Add(session);
                    transferred = true;
                    if (!_accepted.Writer.TryWrite(session))
                    {
                        throw new InvalidOperationException("Fixture session queue exceeded its bound.");
                    }
                }
                finally
                {
                    if (!transferred)
                    {
                        client.Dispose();
                    }
                }
            }
        }
        catch (Exception exception) when (_shutdown.IsCancellationRequested &&
                                         exception is OperationCanceledException or SocketException or ObjectDisposedException)
        {
        }
        catch (Exception)
        {
            _failed = true;
            // Never attach handshake paths/tickets or remote exception details to a test failure.
            _accepted.Writer.TryComplete(new InvalidOperationException("Loopback WebSocket fixture failed."));
        }
        finally
        {
            _accepted.Writer.TryComplete();
        }
    }

    private static async Task<string> ReadUpgradeKeyAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[8192];
        for (var length = 1; length <= header.Length; length++)
        {
            await stream.ReadExactlyAsync(header.AsMemory(length - 1, 1), cancellationToken);
            if (length < 4 || header[length - 4] != '\r' || header[length - 3] != '\n' ||
                header[length - 2] != '\r' || header[length - 1] != '\n')
            {
                continue;
            }
            var lines = Encoding.ASCII.GetString(header, 0, length).Split("\r\n", StringSplitOptions.None);
            var first = lines[0].Split(' ');
            var fields = lines.Skip(1).Where(x => x.Length > 0).Select(x => x.Split(':', 2)).ToArray();
            if (first.Length != 3 || first[0] != "GET" || first[2] != "HTTP/1.1" ||
                !(first[1] == "/socket" || first[1].StartsWith("/socket?", StringComparison.Ordinal)) ||
                fields.Any(x => x.Length != 2) || fields.Any(x => x[0].Equals("Authorization", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidDataException("Fixture upgrade request is invalid.");
            }
            string? SingleHeader(string name)
            {
                var matches = fields.Where(x => x[0].Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
                return matches.Length == 1 ? matches[0][1].Trim() : null;
            }
            if (!string.Equals(SingleHeader("Upgrade"), "websocket", StringComparison.OrdinalIgnoreCase) ||
                SingleHeader("Sec-WebSocket-Version") != "13" ||
                SingleHeader("Connection")?.Split(',').Any(x => x.Trim().Equals("Upgrade", StringComparison.OrdinalIgnoreCase)) != true)
            {
                throw new InvalidDataException("Fixture WebSocket upgrade headers are invalid.");
            }
            var keys = fields.Where(x => x[0].Equals("Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (keys.Length != 1 || Convert.FromBase64String(keys[0][1].Trim()).Length != 16)
            {
                throw new InvalidDataException("Fixture upgrade key is invalid.");
            }
            return keys[0][1].Trim();
        }
        throw new InvalidDataException("Fixture upgrade headers exceeded their bound.");
    }

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        _listener.Stop();
        try
        {
            await _accepting.WaitAsync(Deadline);
            var sessions = _sessions.ToArray();
            foreach (var session in sessions)
            {
                session.Abort();
            }
            await Task.WhenAll(sessions.Select(x => x.Ended)).WaitAsync(Deadline);
            if (_failed || sessions.Any(x => x.Failed))
            {
                throw new InvalidOperationException("Loopback WebSocket fixture failed.");
            }
        }
        finally
        {
            // Dispose even if a bounded wait fails; never abandon an owned physical socket on a test failure.
            foreach (var session in _sessions)
            {
                session.Abort();
                session.Dispose();
            }
            _shutdown.Dispose();
        }
    }

    internal sealed record ObservedMessage(WebSocketMessageType Type, byte[] Bytes);

    internal sealed class SocketSession
    {
        private readonly TcpClient _client;
        private readonly WebSocket _socket;
        private readonly CancellationToken _shutdown;
        private readonly Channel<ObservedMessage> _messages = Channel.CreateBounded<ObservedMessage>(16);
        private readonly SemaphoreSlim _sendGate = new(1, 1);
        private int _messageCount;
        private int _abortRequested;

        internal SocketSession(TcpClient client, WebSocket socket, CancellationToken shutdown)
        {
            _client = client;
            _socket = socket;
            _shutdown = shutdown;
            Ended = ReadMessagesAsync();
        }

        internal Task Ended { get; }
        internal bool Failed { get; private set; }
        internal int MessageCount => Volatile.Read(ref _messageCount);
        internal Task<ObservedMessage> ReadAsync() => _messages.Reader.ReadAsync(_shutdown).AsTask().WaitAsync(Deadline);

        internal Task SendAsync(byte[] bytes, WebSocketMessageType type = WebSocketMessageType.Text, bool endOfMessage = true) =>
            WriteAsync(token => _socket.SendAsync(bytes.AsMemory(), type, endOfMessage, token).AsTask(),
                "Loopback WebSocket send failed.");

        internal Task CloseOutputAsync(string description) =>
            WriteAsync(token => _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, description, token),
                "Loopback WebSocket close failed.");

        private async Task WriteAsync(Func<CancellationToken, Task> write, string failureMessage)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_shutdown);
            timeout.CancelAfter(Deadline);
            await _sendGate.WaitAsync(timeout.Token);
            try
            {
                await write(timeout.Token);
            }
            catch (Exception)
            {
                throw new InvalidOperationException(failureMessage);
            }
            finally
            {
                _sendGate.Release();
            }
        }

        private async Task ReadMessagesAsync()
        {
            try
            {
                while (!_shutdown.IsCancellationRequested)
                {
                    var bytes = new byte[4096];
                    var length = 0;
                    for (var fragments = 0; fragments < 16; fragments++)
                    {
                        var result = await _socket.ReceiveAsync(bytes.AsMemory(length), _shutdown);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            return;
                        }
                        length += result.Count;
                        if (result.EndOfMessage)
                        {
                            if (Interlocked.Increment(ref _messageCount) > 16 ||
                                !_messages.Writer.TryWrite(new(result.MessageType, bytes[..length])))
                            {
                                throw new InvalidDataException("Fixture message count exceeded its bound.");
                            }
                            break;
                        }
                        if (length == bytes.Length || fragments == 15)
                        {
                            throw new InvalidDataException("Fixture message assembly exceeded its bound.");
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested || Volatile.Read(ref _abortRequested) != 0)
            {
            }
            catch (WebSocketException)
            {
                // Client retirement deliberately aborts the physical transport without a close handshake.
            }
            catch (Exception)
            {
                Failed = true;
            }
            finally
            {
                _messages.Writer.TryComplete();
            }
        }

        internal void Abort()
        {
            Volatile.Write(ref _abortRequested, 1);
            _socket.Abort();
        }
        internal void Dispose()
        {
            _socket.Dispose();
            _client.Dispose();
            _sendGate.Dispose();
        }
    }
}
