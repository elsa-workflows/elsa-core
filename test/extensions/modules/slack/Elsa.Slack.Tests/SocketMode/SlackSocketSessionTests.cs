using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Slack.SocketMode;
using Elsa.Slack.SocketMode.Credentials;
using Elsa.Slack.SocketMode.Transport;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Slack.Tests.SocketMode;

public sealed class SlackSocketSessionTests
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData("event-before-hello", (int)SlackSocketSessionEndReason.Protocol)]
    [InlineData("duplicate-hello", (int)SlackSocketSessionEndReason.Protocol)]
    [InlineData("refresh", (int)SlackSocketSessionEndReason.RefreshRequested)]
    [InlineData("link-disabled", (int)SlackSocketSessionEndReason.LinkDisabled)]
    [InlineData("unknown-wire", (int)SlackSocketSessionEndReason.Transport)]
    public async Task PhysicalControlAndHelloOrderingRetireWithoutAdmissionOrAcknowledgement(string scenario, int expected)
    {
        await using var fixture = new SessionFixture();
        await using var peer = new SocketPeer();
        await using var connection = await peer.ConnectAsync(fixture.Configuration);
        var lease = await fixture.CreateLeaseAsync();
        var session = fixture.CreateSession();
        var running = session.RunAsync(connection, lease, CancellationToken.None);
        if (scenario is "duplicate-hello" or "refresh" or "link-disabled")
        {
            await peer.SendAsync("{\"type\":\"hello\"}");
        }
        await peer.SendAsync(scenario switch
        {
            "event-before-hello" => SocketModeTestData.Envelope(),
            "duplicate-hello" => "{\"type\":\"hello\"}",
            "refresh" => "{\"type\":\"disconnect\",\"reason\":\"refresh_requested\"}",
            "link-disabled" => "{\"type\":\"disconnect\",\"reason\":\"link_disabled\"}",
            "unknown-wire" => "{\"type\":\"unsupported\"}",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        });
        Assert.Equal((SlackSocketSessionEndReason)expected, await running.WaitAsync(Deadline));
        Assert.Equal((SlackSocketSessionEndReason)expected, session.TerminationReason);
        Assert.True(session.IsSettled);
        Assert.True(await session.TryDrainAsync(CancellationToken.None));
        await peer.ClientStopped.WaitAsync(Deadline);
        Assert.Equal(0, peer.ClientMessages);
        Assert.Equal(0, fixture.CreatedScopes);
        Assert.Equal(0, fixture.Notifications);
        Assert.Equal(0, fixture.Health.GetSnapshot().Queued);
        Assert.Equal(0, fixture.Health.GetSnapshot().Inflight);
    }

    [Fact]
    public async Task InitialHelloIsBoundedAndAStoppedSessionCannotBeReused()
    {
        await using var fixture = new SessionFixture(TimeSpan.FromMilliseconds(500));
        await using var peer = new SocketPeer();
        await using var connection = await peer.ConnectAsync(fixture.Configuration);
        var lease = await fixture.CreateLeaseAsync();
        var session = fixture.CreateSession();
        Assert.Equal(SlackSocketSessionEndReason.Protocol, await session.RunAsync(connection, lease, CancellationToken.None).WaitAsync(Deadline));
        Assert.True(session.IsSettled);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.RunAsync(connection, lease, CancellationToken.None));
        await peer.ClientStopped.WaitAsync(Deadline);
        Assert.Equal(0, peer.ClientMessages);
        Assert.Equal(0, fixture.Notifications);
    }

    [Fact]
    public async Task PreCanceledRunRetiresTheExactConnectionWithoutCreatingScopes()
    {
        await using var fixture = new SessionFixture();
        await using var peer = new SocketPeer();
        await using var connection = await peer.ConnectAsync(fixture.Configuration);
        var session = fixture.CreateSession();
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Assert.Equal(SlackSocketSessionEndReason.Stopped,
            await session.RunAsync(connection, await fixture.CreateLeaseAsync(), canceled.Token).WaitAsync(Deadline));
        Assert.True(session.IsSettled);
        await peer.ClientStopped.WaitAsync(Deadline);
        Assert.Equal(0, peer.ClientMessages);
        Assert.Equal(0, fixture.CreatedScopes);
        Assert.Equal(0, fixture.Notifications);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IdleRevalidationRetainsAnUncooperativeScopeUntilActualUnwind(bool ignoreCancellation)
    {
        await using var fixture = new SessionFixture(TimeSpan.FromMilliseconds(500));
        await using var peer = new SocketPeer();
        await using var connection = await peer.ConnectAsync(fixture.Configuration);
        var lease = await fixture.CreateLeaseAsync();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Authorizer.AuthorizeAsync(Arg.Any<ConnectionUseRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Assert.Equal("tenant", fixture.Tenants.TenantId);
            entered.TrySetResult();
            return ignoreCancellation ? release.Task : Task.FromResult(false);
        });
        var session = fixture.CreateSession();
        var running = session.RunAsync(connection, lease, CancellationToken.None);
        try
        {
            await peer.SendAsync("{\"type\":\"hello\"}");
            await entered.Task.WaitAsync(Deadline);
            var reason = await running.WaitAsync(Deadline);
            Assert.Equal(ignoreCancellation ? SlackSocketSessionEndReason.ReconciliationRequired : SlackSocketSessionEndReason.Credentials, reason);
            Assert.Equal(SlackSocketSessionEndReason.Credentials, session.TerminationReason);
            Assert.Equal(!ignoreCancellation, session.IsSettled);
            Assert.Equal(ignoreCancellation ? 0 : 1, fixture.DisposedScopes);
            if (ignoreCancellation)
            {
                Assert.False(await session.TryDrainAsync(CancellationToken.None));
                Assert.False(session.IsSettled);
                Assert.Equal(SlackSocketModeHealthReason.Drain, fixture.Health.GetSnapshot().Reason);
            }
        }
        finally
        {
            release.TrySetResult(false);
            await running.WaitAsync(Deadline);
            await fixture.ScopeDisposed.Task.WaitAsync(Deadline);
            Assert.True(await session.TryDrainAsync(CancellationToken.None));
        }
        Assert.True(session.IsSettled);
        Assert.Equal(1, fixture.CreatedScopes);
        Assert.Equal(1, fixture.DisposedScopes);
        await peer.ClientStopped.WaitAsync(Deadline);
        Assert.Equal(0, peer.ClientMessages);
        Assert.Equal(0, fixture.Notifications);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BlockedProviderCancellationCannotSuppressDeadlineOrExternalStop(bool externalStop)
    {
        await using var fixture = new SessionFixture(TimeSpan.FromSeconds(2));
        await using var peer = new SocketPeer();
        await using var connection = await peer.ConnectAsync(fixture.Configuration);
        var lease = await fixture.CreateLeaseAsync();
        var policyEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var policyResult = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var policyCalls = 0;
        fixture.Authorizer.AuthorizeAsync(Arg.Any<ConnectionUseRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            Interlocked.Increment(ref policyCalls);
            // This registration comes AFTER the reader starts, exactly the callback-order counterexample.
            call.ArgAt<CancellationToken>(1).Register(() =>
            {
                callbackEntered.TrySetResult();
                releaseCallback.Task.GetAwaiter().GetResult();
                policyResult.TrySetResult(false);
            });
            policyEntered.TrySetResult();
            return policyResult.Task;
        });
        using var stop = new CancellationTokenSource();
        var session = fixture.CreateSession();
        var running = session.RunAsync(connection, lease, stop.Token);
        try
        {
            await peer.SendAsync("{\"type\":\"hello\"}");
            await policyEntered.Task.WaitAsync(Deadline);
            if (externalStop)
            {
                // Its callback only publishes retirement and starts owned asynchronous cancellation.
                await stop.CancelAsync().WaitAsync(Deadline);
            }
            await callbackEntered.Task.WaitAsync(Deadline);
            Assert.Equal(SlackSocketSessionEndReason.ReconciliationRequired, await running.WaitAsync(Deadline));
            Assert.Equal(externalStop ? SlackSocketSessionEndReason.Stopped : SlackSocketSessionEndReason.Credentials, session.TerminationReason);
            await peer.ClientStopped.WaitAsync(Deadline);
            Assert.Equal(0, peer.ClientMessages);
            Assert.False(session.IsSettled);
            Assert.Equal(1, fixture.CreatedScopes);
            Assert.Equal(0, fixture.DisposedScopes);
            Assert.Equal(1, Volatile.Read(ref policyCalls));
            Assert.Equal(0, fixture.Notifications);
            Assert.False(await session.TryDrainAsync(CancellationToken.None));
            Assert.Equal(0, fixture.DisposedScopes);
        }
        finally
        {
            releaseCallback.TrySetResult();
            await running.WaitAsync(Deadline);
            await fixture.ScopeDisposed.Task.WaitAsync(Deadline);
            Assert.True(await session.TryDrainAsync(CancellationToken.None));
        }
        Assert.True(session.IsSettled);
        Assert.Equal(1, fixture.DisposedScopes);
        Assert.Equal(1, Volatile.Read(ref policyCalls));
        Assert.Equal(0, peer.ClientMessages);
    }

    private sealed class SessionFixture : IAsyncDisposable
    {
        private readonly ServiceProvider _services;
        internal IConnectionUseAuthorizer Authorizer { get; } = Substitute.For<IConnectionUseAuthorizer>();
        private IConnectionLifecycleStore Store { get; } = Substitute.For<IConnectionLifecycleStore>();
        private IManagedSecretManager Secrets { get; } = Substitute.For<IManagedSecretManager>();
        internal DefaultTenantAccessor Tenants { get; } = new();
        internal SlackSocketModeConfiguration Configuration { get; }
        internal SlackSocketModeHealth Health { get; } = new(8, 2);
        internal TaskCompletionSource ScopeDisposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int CreatedScopes;
        internal int DisposedScopes;
        internal int Notifications;

        internal SessionFixture(TimeSpan? operationTimeout = null)
        {
            Configuration = SocketModeTestData.Configuration(SocketModeTestData.Limits with
            {
                OperationTimeout = operationTimeout ?? TimeSpan.FromSeconds(5), DrainTimeout = TimeSpan.FromSeconds(1)
            });
            Authorizer.AuthorizeAsync(Arg.Any<ConnectionUseRequest>(), Arg.Any<CancellationToken>()).Returns(true);
            Store.FindAsync("connection", "tenant", "environment", Arg.Any<CancellationToken>()).Returns(new IntegrationConnection
            {
                Id = "connection", TenantId = "tenant", EnvironmentId = "environment", Revision = 1,
                Status = ConnectionStatus.Active, CurrentGenerationId = "generation", CurrentSecretName = "secret"
            });
            Secrets.ResolveGenerationAsync("secret", "connection", "generation", Arg.Any<CancellationToken>()).Returns(
                SecretPayload.FromValue(JsonSerializer.Serialize(new { kind = 1, accessToken = "synthetic-session-token" })));
            var services = new ServiceCollection();
            services.AddSingleton<ITenantAccessor>(Tenants);
            services.AddScoped(_ => new ScopeProbe(this));
            services.AddScoped(provider =>
            {
                _ = provider.GetRequiredService<ScopeProbe>();
                return new SlackSocketListenerCredentialReader(Configuration, Authorizer, Store, Secrets, TimeProvider.System, Tenants);
            });
            _services = services.BuildServiceProvider();
        }

        internal Task<SlackSocketCredentialLease> CreateLeaseAsync() =>
            new SlackSocketListenerCredentialReader(Configuration, Authorizer, Store, Secrets, TimeProvider.System, Tenants)
                .ResolveCurrentLeaseAsync(CancellationToken.None);

        internal SlackSocketSession CreateSession() => new(Configuration, _services.GetRequiredService<IServiceScopeFactory>(), Health,
            () => Interlocked.Increment(ref Notifications));

        public ValueTask DisposeAsync() => _services.DisposeAsync();

        private sealed class ScopeProbe : IAsyncDisposable
        {
            private readonly SessionFixture _fixture;
            internal ScopeProbe(SessionFixture fixture)
            {
                _fixture = fixture;
                Interlocked.Increment(ref fixture.CreatedScopes);
            }
            public ValueTask DisposeAsync()
            {
                Interlocked.Increment(ref _fixture.DisposedScopes);
                _fixture.ScopeDisposed.TrySetResult();
                return ValueTask.CompletedTask;
            }
        }
    }

    // Actual loopback HTTP upgrade and server WebSocket, never a manufactured ReceivedFrame or socket slot.
    private sealed class SocketPeer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stop = new();
        private readonly TaskCompletionSource<WebSocket> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Task _server;
        private TcpClient? _client;
        private WebSocket? _socket;
        private int _clientMessages;
        internal int ClientMessages => Volatile.Read(ref _clientMessages);
        internal Task ClientStopped => _server;
        internal Uri Endpoint { get; }

        internal SocketPeer()
        {
            _listener.Start();
            Endpoint = new($"ws://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/socket");
            _server = ServeAsync();
        }

        internal async Task<SlackSocketConnection> ConnectAsync(SlackSocketModeConfiguration configuration)
        {
            var policy = SlackSocketTransportPolicy.ForLoopbackFixture(new Uri($"http://127.0.0.1:{Endpoint.Port}/api/apps.connections.open"), Endpoint);
            var connection = await SlackSocketConnection.ConnectAsync(Endpoint, configuration, policy, CancellationToken.None);
            await _ready.Task.WaitAsync(Deadline);
            return connection;
        }

        internal async Task SendAsync(string frame)
        {
            var socket = await _ready.Task.WaitAsync(Deadline);
            await socket.SendAsync(Encoding.UTF8.GetBytes(frame).AsMemory(), WebSocketMessageType.Text, true, _stop.Token).AsTask().WaitAsync(Deadline);
        }

        private async Task ServeAsync()
        {
            try
            {
                _client = await _listener.AcceptTcpClientAsync(_stop.Token);
                var stream = _client.GetStream();
                using var header = new MemoryStream();
                var one = new byte[1];
                while (header.Length < 8192)
                {
                    if (await stream.ReadAsync(one, _stop.Token) != 1)
                    {
                        throw new InvalidDataException();
                    }
                    header.WriteByte(one[0]);
                    var bytes = header.GetBuffer();
                    var length = (int)header.Length;
                    if (length >= 4 && bytes[length - 4] == '\r' && bytes[length - 3] == '\n' && bytes[length - 2] == '\r' && bytes[length - 1] == '\n')
                    {
                        break;
                    }
                }
                var headers = Encoding.ASCII.GetString(header.ToArray()).Split("\r\n");
                var key = headers.Single(x => x.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase)).Split(':', 2)[1].Trim();
                // RFC 6455 mandates SHA-1 for this handshake; it is not credential hashing/storage.
                var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
                await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: {accept}\r\n\r\n"), _stop.Token);
                _socket = WebSocket.CreateFromStream(stream, true, null, Timeout.InfiniteTimeSpan);
                _ready.TrySetResult(_socket);
                var buffer = new byte[4096];
                while (!_stop.IsCancellationRequested)
                {
                    var received = await _socket.ReceiveAsync(buffer.AsMemory(), _stop.Token);
                    if (received.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                    if (received.MessageType == WebSocketMessageType.Text)
                    {
                        Interlocked.Increment(ref _clientMessages);
                    }
                }
            }
            catch (Exception exception) when (_ready.Task.IsCompletedSuccessfully && exception is WebSocketException or IOException or OperationCanceledException or ObjectDisposedException)
            {
                // Exact client retirement aborts its physical transport; no raw close details are retained.
            }
            catch (Exception)
            {
                _ready.TrySetException(new InvalidOperationException("socket_session_peer_failed"));
                throw new InvalidOperationException("socket_session_peer_failed");
            }
        }

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            _listener.Stop();
            _socket?.Dispose();
            _client?.Dispose();
            try
            {
                await _server.WaitAsync(Deadline);
            }
            finally
            {
                _stop.Dispose();
            }
        }
    }
}
