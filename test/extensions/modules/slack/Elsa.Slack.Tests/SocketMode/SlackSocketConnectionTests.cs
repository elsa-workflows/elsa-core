using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Elsa.Slack.SocketMode;
using Elsa.Slack.SocketMode.Transport;

namespace Elsa.Slack.Tests.SocketMode;

public class SlackSocketConnectionTests
{
    private const string Ticket = "socket-fixture-private-physical-ticket";

    [Fact]
    public async Task FragmentedTextProducesOnlyTheExactEnvelopeAckOnItsPhysicalPeer()
    {
        await using var peer = new SlackSocketWebSocketPeer();
        await using var connection = await ConnectAsync(peer);
        var session = await peer.AcceptAsync();
        var receiving = connection.ReceiveAsync(CancellationToken.None);
        var bytes = Encoding.UTF8.GetBytes(SocketModeTestData.Envelope());
        await session.SendAsync(bytes[..(bytes.Length / 2)], endOfMessage: false);
        await session.SendAsync(bytes[(bytes.Length / 2)..]);
        var received = await receiving.WaitAsync(SlackSocketWebSocketPeer.Deadline);
        Assert.NotNull(received);
        Assert.True(await connection.AcknowledgeAsync(received, CancellationToken.None).WaitAsync(SlackSocketWebSocketPeer.Deadline));
        await AssertAckAsync(session, "envelope-a");
        await AssertDrainedAsync(connection, session, 1);
    }

    [Fact]
    public async Task RetiredPhysicalOwnerCannotAcknowledgeThroughItsReplacement()
    {
        await using var peer = new SlackSocketWebSocketPeer();
        await using var oldConnection = await ConnectAsync(peer);
        var oldSession = await peer.AcceptAsync();
        var oldFrame = await SendAndReceiveAsync(oldConnection, oldSession, "old-envelope");
        oldConnection.Retire();
        await oldSession.Ended.WaitAsync(SlackSocketWebSocketPeer.Deadline);

        await using var newConnection = await ConnectAsync(peer);
        var newSession = await peer.AcceptAsync();
        Assert.NotSame(oldSession, newSession);
        Assert.Equal(2, peer.AcceptedCount);
        var newFrame = await SendAndReceiveAsync(newConnection, newSession, "new-envelope");
        Assert.False(await oldConnection.AcknowledgeAsync(oldFrame, CancellationToken.None).WaitAsync(SlackSocketWebSocketPeer.Deadline));
        AssertGenerationDenied(() => newConnection.AcknowledgeAsync(oldFrame, CancellationToken.None));
        AssertGenerationDenied(() => oldConnection.AcknowledgeAsync(newFrame, CancellationToken.None));
        Assert.Null(await oldConnection.ReceiveAsync(CancellationToken.None).WaitAsync(SlackSocketWebSocketPeer.Deadline));
        Assert.True(await newConnection.AcknowledgeAsync(newFrame, CancellationToken.None).WaitAsync(SlackSocketWebSocketPeer.Deadline));
        await AssertAckAsync(newSession, "new-envelope");
        await AssertDrainedAsync(newConnection, newSession, 1);
        Assert.Equal(0, oldSession.MessageCount);
    }

    [Theory]
    [InlineData("""{"type":"hello"}""")]
    [InlineData("""{"type":"disconnect","reason":"warning"}""")]
    [InlineData("""{"type":"disconnect","reason":"link_disabled"}""")]
    public async Task ActualControlFramesCannotBeAcknowledged(string json)
    {
        await using var peer = new SlackSocketWebSocketPeer();
        await using var connection = await ConnectAsync(peer);
        var session = await peer.AcceptAsync();
        var receiving = connection.ReceiveAsync(CancellationToken.None);
        await session.SendAsync(Encoding.UTF8.GetBytes(json));
        var frame = await receiving.WaitAsync(SlackSocketWebSocketPeer.Deadline);
        Assert.NotNull(frame);
        AssertGenerationDenied(() => connection.AcknowledgeAsync(frame, CancellationToken.None));
        await AssertDrainedAsync(connection, session);
    }

    [Theory]
    [InlineData("binary")]
    [InlineData("oversize")]
    [InlineData("fragment-limit")]
    [InlineData("assembly-timeout")]
    [InlineData("malformed-text")]
    public async Task UnsupportedOrUnboundedPhysicalMessagesRetireWithoutAck(string mutation)
    {
        var bytes = Encoding.UTF8.GetBytes(SocketModeTestData.Envelope());
        var limits = mutation switch
        {
            "oversize" => SocketModeTestData.Limits with { MaximumEnvelopeBytes = bytes.Length - 1 },
            // The test wait is shorter than the assembly deadline, so a fragment-bound failure cannot pass via that timeout.
            "fragment-limit" => SocketModeTestData.Limits with { MaximumFragments = 1, MaximumAssemblyTime = TimeSpan.FromSeconds(20) },
            "assembly-timeout" => SocketModeTestData.Limits with { MaximumAssemblyTime = TimeSpan.FromMilliseconds(250) },
            _ => SocketModeTestData.Limits
        };
        await using var peer = new SlackSocketWebSocketPeer();
        await using var connection = await ConnectAsync(peer, limits);
        var session = await peer.AcceptAsync();
        var receiving = connection.ReceiveAsync(CancellationToken.None);
        if (mutation is "fragment-limit" or "assembly-timeout" or "malformed-text")
        {
            await session.SendAsync(Encoding.UTF8.GetBytes("{"), endOfMessage: mutation == "malformed-text");
        }
        else
        {
            await session.SendAsync(bytes, mutation == "binary" ? WebSocketMessageType.Binary : WebSocketMessageType.Text);
        }
        await AssertReceiveFailedAsync(receiving);
        Assert.Null(await connection.ReceiveAsync(CancellationToken.None).WaitAsync(SlackSocketWebSocketPeer.Deadline));
        await AssertDrainedAsync(connection, session);
    }

    [Fact]
    public async Task AConcurrentReceiverIsRejectedWithoutDisturbingTheOwnedReceive()
    {
        await using var peer = new SlackSocketWebSocketPeer();
        await using var connection = await ConnectAsync(peer);
        var session = await peer.AcceptAsync();
        var receiving = connection.ReceiveAsync(CancellationToken.None);
        var failure = Record.Exception(() => { _ = connection.ReceiveAsync(CancellationToken.None); });
        Assert.True(failure is InvalidOperationException && failure.Message == "socket_receive_already_active",
            "A second receiver must be rejected before disturbing the first.");
        Assert.True(failure?.InnerException is null);
        await session.SendAsync(Encoding.UTF8.GetBytes(SocketModeTestData.Envelope()));
        var frame = await receiving.WaitAsync(SlackSocketWebSocketPeer.Deadline);
        Assert.NotNull(frame);
        Assert.True(await connection.AcknowledgeAsync(frame, CancellationToken.None).WaitAsync(SlackSocketWebSocketPeer.Deadline));
        await AssertAckAsync(session, "envelope-a");
        await AssertDrainedAsync(connection, session, 1);
    }

    [Fact]
    public async Task CancellingAnIdleReceiveRetiresAndDrainsThePhysicalSocket()
    {
        await using var peer = new SlackSocketWebSocketPeer();
        await using var connection = await ConnectAsync(peer);
        var session = await peer.AcceptAsync();
        using var cancellation = new CancellationTokenSource();
        var receiving = connection.ReceiveAsync(cancellation.Token);
        Assert.False(receiving.IsCompleted);
        cancellation.Cancel();
        await AssertReceiveFailedAsync(receiving);
        await AssertDrainedAsync(connection, session);
    }

    [Fact]
    public async Task DrainStopsAnIdleReceiveBeforeIdempotentDisposalReturns()
    {
        await using var peer = new SlackSocketWebSocketPeer();
        await using var connection = await ConnectAsync(peer);
        var session = await peer.AcceptAsync();
        var receiving = connection.ReceiveAsync(CancellationToken.None);
        Assert.True(await connection.DrainAsync(CancellationToken.None).WaitAsync(SlackSocketWebSocketPeer.Deadline));
        Assert.True(receiving.IsCompleted);
        await AssertReceiveFailedAsync(receiving);
        await connection.DisposeAsync();
        await connection.DisposeAsync();
        await session.Ended.WaitAsync(SlackSocketWebSocketPeer.Deadline);
        Assert.Equal(0, session.MessageCount);
    }

    [Fact]
    public async Task RemoteCloseRetiresWithoutExportingThePrivateCloseDescription()
    {
        await using var peer = new SlackSocketWebSocketPeer();
        await using var connection = await ConnectAsync(peer);
        var session = await peer.AcceptAsync();
        var receiving = connection.ReceiveAsync(CancellationToken.None);
        await session.CloseOutputAsync(Ticket);
        Assert.Null(await receiving.WaitAsync(SlackSocketWebSocketPeer.Deadline));
        await AssertDrainedAsync(connection, session);
    }

    [Fact]
    public async Task AbruptPeerDisconnectFailsSanitizedAndDrainsWithoutAck()
    {
        await using var peer = new SlackSocketWebSocketPeer();
        await using var connection = await ConnectAsync(peer);
        var session = await peer.AcceptAsync();
        var receiving = connection.ReceiveAsync(CancellationToken.None);
        session.Abort();
        await AssertReceiveFailedAsync(receiving);
        await AssertDrainedAsync(connection, session);
    }

    [Fact]
    public async Task QueuedAcknowledgementsAreSerializedAsSeparateExactMessages()
    {
        await using var peer = new SlackSocketWebSocketPeer();
        await using var connection = await ConnectAsync(peer);
        var session = await peer.AcceptAsync();
        var first = await SendAndReceiveAsync(connection, session, "envelope-first");
        var second = await SendAndReceiveAsync(connection, session, "envelope-second");
        var sends = await Task.WhenAll(connection.AcknowledgeAsync(first, CancellationToken.None),
            connection.AcknowledgeAsync(second, CancellationToken.None)).WaitAsync(SlackSocketWebSocketPeer.Deadline);
        Assert.All(sends, value => Assert.True(value));
        await AssertAckAsync(session, "envelope-first");
        await AssertAckAsync(session, "envelope-second");
        await AssertDrainedAsync(connection, session, 2);
    }

    [Theory]
    [InlineData(302)]
    [InlineData(307)]
    [InlineData(308)]
    public async Task HandshakeRedirectNeverReachesASecondWebSocketPeer(int status)
    {
        await using var destination = new SlackSocketWebSocketPeer();
        // A followed HTTP redirect can complete a real upgrade at the destination; it must not be masked by an invalid response.
        await using var origin = new SlackSocketHttpPeer(_ => new([], Status: status, Location: destination.HttpSocketEndpoint.AbsoluteUri));
        var policy = SlackSocketTransportPolicy.ForLoopbackFixture(origin.ApiEndpoint, origin.SocketEndpoint);
        SlackSocketConnection? connected = null;
        try
        {
            var failure = await Record.ExceptionAsync(async () =>
            {
                connected = await SlackSocketConnection.ConnectAsync(new Uri(origin.SocketEndpoint.AbsoluteUri + "?ticket=" + Ticket), SocketModeTestData.Configuration(),
                    policy, CancellationToken.None).WaitAsync(SlackSocketWebSocketPeer.Deadline);
            });
            Assert.True(failure is InvalidOperationException && failure.Message == "socket_connect_failed",
                "A handshake redirect must fail with the fixed sanitized category.");
            Assert.True(failure?.InnerException is null);
            Assert.True(failure is null || !failure.ToString().Contains(Ticket, StringComparison.Ordinal),
                "A failed handshake must not expose its private ticket.");
            Assert.Equal(1, origin.RequestCount);
            Assert.Equal(0, destination.AcceptedCount);
        }
        finally
        {
            if (connected is not null)
            {
                await connected.DisposeAsync();
            }
        }
    }

    private static Task<SlackSocketConnection> ConnectAsync(SlackSocketWebSocketPeer peer, SlackSocketModeLimits? limits = null) =>
        SlackSocketConnection.ConnectAsync(new Uri(peer.SocketEndpoint.AbsoluteUri + "?ticket=" + Ticket),
            SocketModeTestData.Configuration(limits), SlackSocketTransportPolicy.ForLoopbackFixture(peer.ApiEndpoint, peer.SocketEndpoint),
            CancellationToken.None).WaitAsync(SlackSocketWebSocketPeer.Deadline);

    private static async Task<SlackSocketConnection.ReceivedFrame> SendAndReceiveAsync(SlackSocketConnection connection,
        SlackSocketWebSocketPeer.SocketSession session, string envelopeId)
    {
        var receiving = connection.ReceiveAsync(CancellationToken.None);
        await session.SendAsync(Encoding.UTF8.GetBytes(SocketModeTestData.Envelope(envelopeId: envelopeId)));
        var frame = await receiving.WaitAsync(SlackSocketWebSocketPeer.Deadline);
        Assert.NotNull(frame);
        return frame;
    }

    private static void AssertGenerationDenied(Func<Task<bool>> action)
    {
        var failure = Record.Exception(() => { _ = action(); });
        Assert.True(failure is InvalidOperationException && failure.Message == "socket_ack_generation_denied",
            "A control or foreign physical frame must be rejected before send initiation.");
        Assert.True(failure?.InnerException is null);
    }

    private static async Task AssertReceiveFailedAsync(Task<SlackSocketConnection.ReceivedFrame?> receiving)
    {
        var failure = await Record.ExceptionAsync(() => receiving.WaitAsync(SlackSocketWebSocketPeer.Deadline));
        Assert.True(failure is InvalidOperationException && failure.Message == "socket_receive_failed",
            "The physical message must fail with the fixed receive category within the test bound.");
        Assert.True(failure?.InnerException is null);
        Assert.True(failure is null || !failure.ToString().Contains(Ticket, StringComparison.Ordinal),
            "Receive failure must not expose ticket or remote close data.");
    }

    private static async Task AssertDrainedAsync(SlackSocketConnection connection, SlackSocketWebSocketPeer.SocketSession session,
        int acknowledgedMessages = 0)
    {
        Assert.True(await connection.DrainAsync(CancellationToken.None).WaitAsync(SlackSocketWebSocketPeer.Deadline));
        await session.Ended.WaitAsync(SlackSocketWebSocketPeer.Deadline);
        Assert.Equal(acknowledgedMessages, session.MessageCount);
    }

    private static async Task AssertAckAsync(SlackSocketWebSocketPeer.SocketSession session, string envelopeId)
    {
        var message = await session.ReadAsync();
        Assert.True(message.Type == WebSocketMessageType.Text, "An acknowledgement must be a text message.");
        JsonDocument? document = null;
        var failure = Record.Exception(() => document = JsonDocument.Parse(message.Bytes));
        Assert.True(failure is null, "An acknowledgement must be valid JSON.");
        Assert.NotNull(document);
        using (document)
        {
            Assert.True(document.RootElement.ValueKind == JsonValueKind.Object, "An acknowledgement must be a JSON object.");
            var fields = document.RootElement.EnumerateObject().ToArray();
            Assert.True(fields.Length == 1 && fields[0].Name == "envelope_id" &&
                        fields[0].Value.ValueKind == JsonValueKind.String && fields[0].Value.GetString() == envelopeId,
                "An acknowledgement must contain only the exact physical frame's envelope ID.");
        }
    }
}
