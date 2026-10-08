using System.Text;
using System.Text.Json;
using Elsa.Slack.SocketMode;
using Elsa.Slack.SocketMode.Transport;

namespace Elsa.Slack.Tests.SocketMode;

public class SlackSocketUrlOpenerTests
{
    private const string AppToken = "socket-fixture-private-app-token";
    private const string Ticket = "socket-fixture-private-ticket";

    [Fact]
    public async Task PinnedSdkUsesFixedPostBearerAndReturnsTheExactValidatedUrl()
    {
        await using var peer = new SlackSocketHttpPeer(x => Success(x));
        Uri? result = null;
        var failure = await Record.ExceptionAsync(async () =>
        {
            result = await Opener(peer).OpenAsync(AppToken, CancellationToken.None).WaitAsync(SlackSocketHttpPeer.Deadline);
        });
        Assert.True(failure is null, "The valid private response must open successfully.");
        Assert.NotNull(result);
        var request = await peer.RequestReceived.WaitAsync(SlackSocketHttpPeer.Deadline);
        Assert.Equal("POST", request.Method);
        Assert.Equal("/api/apps.connections.open", request.Target);
        Assert.True(request.Authorization == "Bearer " + AppToken, "The private bearer must be sent only in its header.");
        Assert.True(request.Body == "{}", "The SDK request body must not carry the private credential.");
        Assert.True(result.AbsoluteUri == TicketedUrl(peer), "The SDK must return exactly the validated dynamic URL.");
        Assert.Equal(1, peer.RequestCount);
    }

    [Theory]
    [InlineData(302)]
    [InlineData(307)]
    [InlineData(308)]
    public async Task RedirectsNeverReachASecondOwnedHttpPeer(int status)
    {
        string? permittedUrl = null;
        // Following the redirect would return a valid URL for the original socket policy, not mask a redirect bug with a second rejection.
        await using var destination = new SlackSocketHttpPeer(_ => new(JsonSerializer.SerializeToUtf8Bytes(new { ok = true, url = permittedUrl })));
        await using var origin = new SlackSocketHttpPeer(_ => new([], Status: status, Location: destination.ApiEndpoint.AbsoluteUri));
        permittedUrl = TicketedUrl(origin);
        await AssertRejectedAsync(Opener(origin));
        Assert.Equal(1, origin.RequestCount);
        Assert.Equal(0, destination.RequestCount);
    }

    [Theory]
    [InlineData("non-json")]
    [InlineData("false-ok")]
    [InlineData("string-ok")]
    [InlineData("null-ok")]
    [InlineData("missing-ok")]
    [InlineData("missing-url")]
    [InlineData("null-url")]
    [InlineData("object-url")]
    [InlineData("relative-url")]
    [InlineData("malformed")]
    [InlineData("duplicate-ok")]
    [InlineData("duplicate-url")]
    [InlineData("invalid-utf8")]
    [InlineData("too-deep")]
    [InlineData("sdk-discriminator")]
    public async Task RawHttpValidationAndSdkConversionFailuresAreSanitized(string mutation)
    {
        await using var peer = new SlackSocketHttpPeer(x => MutatedResponse(x, mutation));
        await AssertRejectedAsync(Opener(peer, SocketModeTestData.Limits with { MaximumJsonDepth = 4 }));
        Assert.Equal(1, peer.RequestCount);
    }

    [Theory]
    [InlineData(429)]
    [InlineData(500)]
    public async Task HttpFailureDoesNotRetryOrExposeTheResponse(int status)
    {
        await using var peer = new SlackSocketHttpPeer(_ => new(Encoding.UTF8.GetBytes(AppToken + Ticket), Status: status));
        await AssertRejectedAsync(Opener(peer));
        Assert.Equal(1, peer.RequestCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OversizeBodiesFailWithAndWithoutContentLength(bool includeLength)
    {
        await using var peer = new SlackSocketHttpPeer(x => new(JsonSerializer.SerializeToUtf8Bytes(new
        {
            ok = true, url = TicketedUrl(x), padding = new string('x', 512)
        }), IncludeLength: includeLength));
        await AssertRejectedAsync(Opener(peer, SocketModeTestData.Limits with { MaximumEnvelopeBytes = 256 }));
        Assert.Equal(1, peer.RequestCount);
    }

    [Theory]
    [InlineData("scheme")]
    [InlineData("host")]
    [InlineData("port")]
    [InlineData("path")]
    [InlineData("userinfo")]
    [InlineData("fragment")]
    public async Task ReturnedFixtureUrlCannotChangeItsBoundOrigin(string mutation)
    {
        await using var peer = new SlackSocketHttpPeer(x =>
        {
            var url = new UriBuilder(TicketedUrl(x));
            switch (mutation)
            {
                case "scheme": url.Scheme = "wss"; break;
                case "host": url.Host = "127.0.0.2"; break;
                case "port": url.Port = x.SocketEndpoint.Port == 65535 ? 65534 : x.SocketEndpoint.Port + 1; break;
                case "path": url.Path = "/other-socket"; break;
                case "userinfo": url.UserName = AppToken; break;
                case "fragment": url.Fragment = Ticket; break;
                default: throw new ArgumentOutOfRangeException(nameof(mutation));
            }
            return new(JsonSerializer.SerializeToUtf8Bytes(new { ok = true, url = url.Uri.AbsoluteUri }));
        });
        await AssertRejectedAsync(Opener(peer));
        Assert.Equal(1, peer.RequestCount);
    }

    [Fact]
    public async Task CancellationAfterResponseHeadersInterruptsAPartialBody()
    {
        await using var peer = new SlackSocketHttpPeer(x => Success(x) with { HoldBody = true });
        using var cancellation = new CancellationTokenSource();
        var opening = Opener(peer, SocketModeTestData.Limits with { OperationTimeout = TimeSpan.FromSeconds(15) })
            .OpenAsync(AppToken, cancellation.Token);
        var barrierFailure = await Record.ExceptionAsync(() => peer.BodyStarted.WaitAsync(SlackSocketHttpPeer.Deadline));
        var wasIncomplete = !opening.IsCompleted;
        cancellation.Cancel();
        var error = await Record.ExceptionAsync(() => opening.WaitAsync(SlackSocketHttpPeer.Deadline));
        Assert.True(barrierFailure is null, "The owned peer must observe a flushed partial response before cancellation.");
        Assert.True(wasIncomplete, "Opening must still await the held response body.");
        Assert.True(error is OperationCanceledException cancelled && cancelled.CancellationToken == cancellation.Token,
            "Opening must propagate bounded caller cancellation.");
        Assert.NotNull(error);
        Assert.True(error.Message == "Socket URL opening was cancelled.", "Cancellation diagnostics must be fixed and sanitized.");
        Assert.True(error.InnerException is null, "Cancellation must not retain a private inner exception.");
        Assert.Equal(1, peer.RequestCount);
    }

    [Theory]
    [InlineData("wss.slack.com")]
    [InlineData("wss-primary.slack.com")]
    [InlineData("wss-backup.slack.com")]
    public void ProductionPolicyAllowsOnlyTheReviewedSlackOrigins(string host) =>
        SlackSocketTransportPolicy.Production.ValidateSocket(new Uri($"wss://{host}/link/?ticket={Ticket}"));

    [Theory]
    [InlineData("ws://wss.slack.com/link/")]
    [InlineData("wss://wss.slack.com.evil.example/link/")]
    [InlineData("wss://evil.example/link/")]
    [InlineData("wss://wss.slack.com:444/link/")]
    [InlineData("wss://user@wss.slack.com/link/")]
    [InlineData("wss://wss.slack.com/link/#fragment")]
    public void ProductionPolicyRejectsOriginAuthorityPortAndFragmentChanges(string url)
    {
        var error = Assert.Throws<InvalidOperationException>(() => SlackSocketTransportPolicy.Production.ValidateSocket(new Uri(url)));
        Assert.Equal("socket_origin_denied", error.Message);
        Assert.True(error.InnerException is null, "Origin rejection must not retain an inner exception.");
    }

    [Theory]
    [InlineData("https://127.0.0.1/api/apps.connections.open", "ws://127.0.0.1/socket")]
    [InlineData("http://localhost/api/apps.connections.open", "ws://localhost/socket")]
    [InlineData("http://127.0.0.1/api/apps.connections.open?token=private", "ws://127.0.0.1/socket")]
    [InlineData("http://127.0.0.1/api/apps.connections.open", "ws://127.0.0.1/socket?ticket=private")]
    [InlineData("http://127.0.0.1/wrong", "ws://127.0.0.1/socket")]
    [InlineData("http://user@127.0.0.1/api/apps.connections.open", "ws://127.0.0.1/socket")]
    [InlineData("http://127.0.0.1/api/apps.connections.open", "wss://127.0.0.1/socket")]
    public void FixturePolicyCannotBecomeAConfigurableProductionTransport(string api, string socket)
    {
        var error = Assert.Throws<ArgumentException>(() => SlackSocketTransportPolicy.ForLoopbackFixture(new Uri(api), new Uri(socket)));
        Assert.Equal("Fixture transport requires explicit literal loopback endpoints.", error.Message);
        Assert.True(error.InnerException is null, "Fixture policy rejection must not retain an inner exception.");
    }

    private static SlackSocketUrlOpener Opener(SlackSocketHttpPeer peer, SlackSocketModeLimits? limits = null) =>
        new(SocketModeTestData.Configuration(limits), SlackSocketTransportPolicy.ForLoopbackFixture(peer.ApiEndpoint, peer.SocketEndpoint));

    private static string TicketedUrl(SlackSocketHttpPeer peer) => peer.SocketEndpoint.AbsoluteUri + "?ticket=" + Ticket;
    private static SlackSocketHttpResponse Success(SlackSocketHttpPeer peer) =>
        new(JsonSerializer.SerializeToUtf8Bytes(new { ok = true, url = TicketedUrl(peer) }));

    private static SlackSocketHttpResponse MutatedResponse(SlackSocketHttpPeer peer, string mutation)
    {
        var url = JsonSerializer.Serialize(TicketedUrl(peer));
        var json = mutation switch
        {
            "false-ok" => "{\"ok\":false,\"url\":" + url + "}",
            "string-ok" => "{\"ok\":\"true\",\"url\":" + url + "}",
            "null-ok" => "{\"ok\":null,\"url\":" + url + "}",
            "missing-ok" => "{\"url\":" + url + "}",
            "missing-url" => "{\"ok\":true}",
            "null-url" => "{\"ok\":true,\"url\":null}",
            "object-url" => "{\"ok\":true,\"url\":{}}",
            "relative-url" => "{\"ok\":true,\"url\":\"/socket\"}",
            "malformed" => "{\"ok\":true,\"url\":" + url,
            "duplicate-ok" => "{\"ok\":true,\"ok\":true,\"url\":" + url + "}",
            "duplicate-url" => "{\"ok\":true,\"url\":" + url + ",\"url\":" + url + "}",
            "too-deep" => "{\"ok\":true,\"url\":" + url + ",\"unknown\":{\"a\":{\"b\":{\"c\":{\"d\":1}}}}}",
            "sdk-discriminator" => "{\"ok\":true,\"url\":" + url + ",\"reply_to\":1}",
            "invalid-utf8" => "{\"ok\":true,\"url\":" + url + ",\"ignored\":\"hello\"}",
            "non-json" => "{\"ok\":true,\"url\":" + url + "}",
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        };
        var body = Encoding.UTF8.GetBytes(json);
        if (mutation == "invalid-utf8")
        {
            var offset = Encoding.UTF8.GetByteCount(json[..json.IndexOf("hello", StringComparison.Ordinal)]);
            body[offset] = 0xc3;
            body[offset + 1] = 0x28;
        }
        return new(body, ContentType: mutation == "non-json" ? "text/plain" : "application/json");
    }

    private static async Task AssertRejectedAsync(SlackSocketUrlOpener opener)
    {
        var error = await Record.ExceptionAsync(() => opener.OpenAsync(AppToken, CancellationToken.None)
            .WaitAsync(SlackSocketHttpPeer.Deadline));
        Assert.True(error is InvalidOperationException, "Opening must fail with the fixed sanitized failure category.");
        Assert.NotNull(error);
        Assert.True(error.Message == "socket_open_failed", "Failure diagnostics must be fixed and sanitized.");
        Assert.True(error.InnerException is null, "Opening must not retain a private inner exception.");
        Assert.True(!error.ToString().Contains(AppToken, StringComparison.Ordinal) &&
                    !error.ToString().Contains(Ticket, StringComparison.Ordinal), "Private credential/ticket data must not enter diagnostics.");
    }
}
