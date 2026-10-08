using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Elsa.Slack.Tests.SocketMode;

internal sealed record SlackSocketHttpResponse(byte[] Body, string ContentType = "application/json", int Status = 200,
    bool IncludeLength = true, string? Location = null, bool HoldBody = false);

// Request contents and ticket-bearing URLs stay in fixture memory, never test output or receipts.
internal sealed record SlackSocketHttpRequest(string Method, string Target, string? Authorization, string Body);

internal sealed class SlackSocketHttpPeer : IAsyncDisposable
{
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _shutdown = new(TimeSpan.FromSeconds(20));
    private readonly TaskCompletionSource<SlackSocketHttpRequest> _requestReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _bodyStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseBody = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ConcurrentQueue<SlackSocketHttpRequest> _requests = new();
    private readonly Func<SlackSocketHttpPeer, SlackSocketHttpResponse> _response;
    private readonly Task _server;
    private bool _failed;

    internal SlackSocketHttpPeer(Func<SlackSocketHttpPeer, SlackSocketHttpResponse> response)
    {
        _response = response;
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        ApiEndpoint = new Uri($"http://127.0.0.1:{port}/api/apps.connections.open");
        SocketEndpoint = new Uri($"ws://127.0.0.1:{port}/socket");
        _server = ServeAsync();
    }

    internal Uri ApiEndpoint { get; }
    // This is an origin for URL validation only. The peer does not implement WebSocket upgrade.
    internal Uri SocketEndpoint { get; }
    internal int RequestCount => _requests.Count;
    internal Task<SlackSocketHttpRequest> RequestReceived => _requestReceived.Task;
    internal Task BodyStarted => _bodyStarted.Task;

    private async Task ServeAsync()
    {
        try
        {
            while (!_shutdown.IsCancellationRequested)
            {
                using var client = await _listener.AcceptTcpClientAsync(_shutdown.Token);
                await using var stream = client.GetStream();
                var request = await ReadRequestAsync(stream, _shutdown.Token);
                if (_requests.Count >= 4)
                {
                    throw new InvalidDataException("Fixture request count exceeded its bound.");
                }
                _requests.Enqueue(request);
                _requestReceived.TrySetResult(request);
                var response = _response(this);
                var headers = $"HTTP/1.1 {response.Status} Fixture\r\nConnection: close\r\nContent-Type: {response.ContentType}\r\n" +
                              (response.IncludeLength ? $"Content-Length: {response.Body.Length}\r\n" : "") +
                              (response.Location is not null ? $"Location: {response.Location}\r\n" : "") + "\r\n";
                try
                {
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(headers), _shutdown.Token);
                    if (response.HoldBody)
                    {
                        if (response.Body.Length < 2)
                        {
                            throw new InvalidOperationException("A delayed fixture body must have two parts.");
                        }
                        await stream.WriteAsync(response.Body.AsMemory(0, 1), _shutdown.Token);
                        await stream.FlushAsync(_shutdown.Token);
                        _bodyStarted.TrySetResult();
                        await _releaseBody.Task.WaitAsync(_shutdown.Token);
                        await stream.WriteAsync(response.Body.AsMemory(1), _shutdown.Token);
                    }
                    else
                    {
                        await stream.WriteAsync(response.Body, _shutdown.Token);
                    }
                    await stream.FlushAsync(_shutdown.Token);
                }
                catch (IOException)
                {
                    // An early rejection/cancellation may close the client before its response finishes.
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
            // Do not attach an exception that might contain private request or response content.
            _requestReceived.TrySetException(new InvalidOperationException("Loopback HTTP fixture failed."));
            _bodyStarted.TrySetException(new InvalidOperationException("Loopback HTTP fixture failed."));
        }
    }

    private static async Task<SlackSocketHttpRequest> ReadRequestAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var header = new MemoryStream();
        var single = new byte[1];
        while (header.Length < 8192)
        {
            if (await stream.ReadAsync(single, cancellationToken) != 1)
            {
                throw new InvalidDataException("Incomplete fixture request.");
            }
            header.WriteByte(single[0]);
            var bytes = header.GetBuffer();
            var length = (int)header.Length;
            if (length >= 4 && bytes[length - 4] == '\r' && bytes[length - 3] == '\n' &&
                bytes[length - 2] == '\r' && bytes[length - 1] == '\n')
            {
                var lines = Encoding.ASCII.GetString(bytes, 0, length).Split("\r\n", StringSplitOptions.None);
                var first = lines[0].Split(' ');
                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in lines.Skip(1).Where(x => x.Length > 0))
                {
                    var separator = line.IndexOf(':');
                    if (separator <= 0 || !headers.TryAdd(line[..separator], line[(separator + 1)..].Trim()))
                    {
                        throw new InvalidDataException("Invalid fixture request headers.");
                    }
                }
                if (first.Length != 3 || headers.ContainsKey("Transfer-Encoding") ||
                    !int.TryParse(headers.GetValueOrDefault("Content-Length", "0"), NumberStyles.None,
                        CultureInfo.InvariantCulture, out var bodyLength) || bodyLength is < 0 or > 4096)
                {
                    throw new InvalidDataException("Unbounded fixture request.");
                }
                var body = new byte[bodyLength];
                await stream.ReadExactlyAsync(body, cancellationToken);
                return new(first[0], first[1], headers.GetValueOrDefault("Authorization"), Encoding.UTF8.GetString(body));
            }
        }
        throw new InvalidDataException("Fixture request headers exceeded their bound.");
    }

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        _listener.Stop();
        try
        {
            await _server.WaitAsync(Deadline);
        }
        finally
        {
            _shutdown.Dispose();
            _requests.Clear();
        }
        if (_failed)
        {
            throw new InvalidOperationException("Loopback HTTP fixture failed.");
        }
    }
}
