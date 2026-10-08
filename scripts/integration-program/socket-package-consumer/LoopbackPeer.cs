using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SocketPackageConsumer;

// Physical HTTP endpoint and RFC6455 peer. No graph events, fake frames, message source or workflow dispatcher.
internal sealed class LoopbackPeer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly TaskCompletionSource<WebSocket> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly EvidenceClock _clock;
    private readonly Task _server;
    private TcpClient? _socketClient;
    private WebSocket? _socket;
    private int _acknowledgements;
    internal Uri ApiEndpoint { get; }
    internal Uri SocketEndpoint { get; }
    internal Task Connected => _ready.Task;
    internal TaskCompletionSource Acknowledged { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal int Acknowledgements => Volatile.Read(ref _acknowledgements);
    internal int OpenCalls { get; private set; }
    internal bool ExactBearerObserved { get; private set; }
    internal long AckOrder { get; private set; }
    internal bool Disposed { get; private set; }
    internal bool ClientRetiredBeforeDisposal { get; private set; }
    internal Task ClientRetired => _server;

    internal LoopbackPeer(EvidenceClock clock)
    {
        _clock = clock;
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        ApiEndpoint = new($"http://127.0.0.1:{port}/api/apps.connections.open");
        SocketEndpoint = new($"ws://127.0.0.1:{port}/socket");
        _server = ServeAsync();
    }

    internal async Task SendAsync(string frame, CancellationToken cancellationToken)
    {
        var socket = await _ready.Task.WaitAsync(cancellationToken);
        await socket.SendAsync(Encoding.UTF8.GetBytes(frame).AsMemory(), WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task ServeAsync()
    {
        try
        {
            using (var api = await _listener.AcceptTcpClientAsync(_stop.Token))
            {
                var stream = api.GetStream();
                var headers = await ReadHeadersAsync(stream);
                Require.That(headers[0].StartsWith("POST /api/apps.connections.open HTTP/1.", StringComparison.Ordinal), "physical-open-path");
                ExactBearerObserved = headers.Any(x => x.Equals("Authorization: Bearer " + FixtureConstants.SecretMarker, StringComparison.OrdinalIgnoreCase));
                Require.That(ExactBearerObserved, "physical-open-bearer");
                var lengthHeader = headers.SingleOrDefault(x => x.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
                if (lengthHeader != null)
                {
                    Require.That(int.TryParse(lengthHeader.Split(':', 2)[1].Trim(), out var length) && length is >= 0 and <= 4096, "physical-open-body-bound");
                    var body = new byte[length];
                    await stream.ReadExactlyAsync(body, _stop.Token);
                }
                OpenCalls++;
                var bodyBytes = JsonSerializer.SerializeToUtf8Bytes(new { ok = true, url = SocketEndpoint.AbsoluteUri });
                await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\n\r\n"), _stop.Token);
                await stream.WriteAsync(bodyBytes, _stop.Token);
            }
            _socketClient = await _listener.AcceptTcpClientAsync(_stop.Token);
            var socketStream = _socketClient.GetStream();
            var upgrade = await ReadHeadersAsync(socketStream);
            Require.That(upgrade[0] == "GET /socket HTTP/1.1", "physical-upgrade-path");
            var key = upgrade.Single(x => x.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase)).Split(':', 2)[1].Trim();
            // RFC6455 requires SHA1 for the upgrade; this is not credential hashing.
            var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
            await socketStream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: {accept}\r\n\r\n"), _stop.Token);
            // https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets.websocket.createfromstream
            _socket = WebSocket.CreateFromStream(socketStream, true, null, Timeout.InfiniteTimeSpan);
            _ready.TrySetResult(_socket);
            var buffer = new byte[1024];
            while (!_stop.IsCancellationRequested)
            {
                var received = await _socket.ReceiveAsync(buffer.AsMemory(), _stop.Token);
                if (received.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
                Require.That(received.EndOfMessage && received.MessageType == WebSocketMessageType.Text && received.Count > 0, "physical-ack-frame");
                using var json = JsonDocument.Parse(buffer.AsMemory(0, received.Count));
                var root = json.RootElement;
                Require.That(root.ValueKind == JsonValueKind.Object && root.EnumerateObject().Count() == 1 &&
                    root.GetProperty("envelope_id").GetString() == FixtureConstants.EnvelopeId, "physical-ack-identity");
                AckOrder = _clock.Next();
                Interlocked.Increment(ref _acknowledgements);
                Acknowledged.TrySetResult();
            }
        }
        catch (Exception exception) when (_ready.Task.IsCompletedSuccessfully && exception is WebSocketException or IOException or OperationCanceledException or ObjectDisposedException)
        {
            // Listener retirement aborts the transport. No provider messages or URLs are exported.
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            var failure = new ProofFailure("physical-peer-failed");
            _ready.TrySetException(failure);
            Acknowledged.TrySetException(failure);
            throw failure;
        }
    }

    private async Task<string[]> ReadHeadersAsync(NetworkStream stream)
    {
        using var header = new MemoryStream();
        var one = new byte[1];
        while (header.Length < 8192)
        {
            Require.That(await stream.ReadAsync(one, _stop.Token) == 1, "physical-request-truncated");
            header.WriteByte(one[0]);
            var bytes = header.GetBuffer();
            var n = (int)header.Length;
            if (n >= 4 && bytes[n - 4] == '\r' && bytes[n - 3] == '\n' && bytes[n - 2] == '\r' && bytes[n - 1] == '\n')
            {
                return Encoding.ASCII.GetString(header.ToArray()).Split("\r\n");
            }
        }
        throw new ProofFailure("physical-request-header-bound");
    }

    public async ValueTask DisposeAsync()
    {
        if (Disposed)
        {
            return;
        }
        ClientRetiredBeforeDisposal = _server.IsCompletedSuccessfully;
        _stop.Cancel();
        _listener.Stop();
        _socket?.Dispose();
        _socketClient?.Dispose();
        try
        {
            await _server.WaitAsync(TimeSpan.FromSeconds(20));
            Disposed = true;
        }
        finally
        {
            _stop.Dispose();
        }
    }
}

internal sealed class EvidenceClock
{
    private long _value;
    internal long Next() => Interlocked.Increment(ref _value);
}
