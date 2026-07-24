using System.Net;
using System.Net.Sockets;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Validation;
using Microsoft.Extensions.Options;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.ExternalAuthentication.UnitTests.Security;

public class OutboundProviderHttpTests
{
    [Theory]
    [InlineData("http://provider.example")]
    [InlineData("https://unapproved.example")]
    public async Task RejectsNonHttpsAndUnapprovedDestinations(string value)
    {
        var validator = CreateValidator(new ExternalAuthenticationOptions
        {
            ProviderEgress = new ProviderEgressOptions { AllowedHosts = ["provider.example"] }
        });

        await Assert.ThrowsAsync<OutboundDestinationException>(() => validator.ValidateAsync(new Uri(value)).AsTask());
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("192.0.2.1")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("2001:db8::1")]
    public async Task RejectsLoopbackPrivateLinkLocalAndReservedAddresses(string address)
    {
        var validator = CreateValidator(address);

        await Assert.ThrowsAsync<OutboundDestinationException>(() => validator.ValidateAsync(new Uri("https://provider.example")).AsTask());
    }

    [Fact]
    public async Task RejectsHostWhenAnyResolvedAddressIsUnsafe()
    {
        var validator = CreateValidator("8.8.8.8", "127.0.0.1");

        await Assert.ThrowsAsync<OutboundDestinationException>(() => validator.ValidateAsync(new Uri("https://provider.example")).AsTask());
    }

    [Fact]
    public async Task ConnectsToTheValidatedAddress()
    {
        var validator = CreateValidator("8.8.8.8");
        var connector = new RecordingConnector();
        var connectionFactory = new ValidatedOutboundConnectionFactory(validator, connector);

        await using var stream = await connectionFactory.ConnectAsync(new DnsEndPoint("provider.example", 443));

        Assert.Equal([IPAddress.Parse("8.8.8.8")], connector.Addresses);
    }

    [Fact]
    public async Task RejectsDnsRebindingBeforeConnect()
    {
        var resolver = new SequencedDnsResolver([IPAddress.Parse("8.8.8.8")], [IPAddress.Loopback]);
        var validator = new OutboundDestinationValidator(OptionsFactory.Create(new ExternalAuthenticationOptions()), resolver);
        var connector = new RecordingConnector();
        var connectionFactory = new ValidatedOutboundConnectionFactory(validator, connector);

        await validator.ValidateAsync(new Uri("https://provider.example"));
        await Assert.ThrowsAsync<OutboundDestinationException>(() => connectionFactory.ConnectAsync(new DnsEndPoint("provider.example", 443), CancellationToken.None).AsTask());

        Assert.Empty(connector.Addresses);
    }

    [Fact]
    public async Task FollowsOnlyValidatedGetRedirectsAndRejectsUnsafeRedirectTargets()
    {
        var handler = new SequenceHandler(
            new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("http://127.0.0.1/metadata") } });
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ProviderHttpException>(() => client.GetAsync(new Uri("https://provider.example/metadata"), ProviderResponseKind.Discovery).AsTask());

        Assert.Equal(ProviderHttpFailure.DestinationRejected, exception.Failure);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task RejectsUnexpectedRedirectsForCredentialBearingRequests()
    {
        var handler = new SequenceHandler(new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("https://provider.example/token-two") } });
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ProviderHttpException>(() => client.PostFormAsync(new Uri("https://provider.example/token"), new Dictionary<string, string>(), ProviderResponseKind.Token).AsTask());

        Assert.Equal(ProviderHttpFailure.RedirectRejected, exception.Failure);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task AppliesRequestTimeoutWithoutLeakingTransportDetails()
    {
        var options = new ExternalAuthenticationOptions { ProviderEgress = new ProviderEgressOptions { RequestTimeout = TimeSpan.FromMilliseconds(20), ConnectTimeout = TimeSpan.FromSeconds(1) } };
        var client = CreateClient(new DelayingHandler(), options);

        var exception = await Assert.ThrowsAsync<ProviderHttpException>(() => client.GetAsync(new Uri("https://provider.example/metadata"), ProviderResponseKind.Discovery).AsTask());

        Assert.Equal(ProviderHttpFailure.Timeout, exception.Failure);
        Assert.DoesNotContain("internal", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CapsDiscoveryTokenAndUserInfoResponseBodies()
    {
        var options = new ExternalAuthenticationOptions
        {
            ProviderEgress = new ProviderEgressOptions
            {
                MaximumDiscoveryResponseBytes = 3,
                MaximumTokenResponseBytes = 3,
                MaximumUserInfoResponseBytes = 3
            }
        };
        var client = CreateClient(new SequenceHandler(
            Response("1234"),
            Response("1234"),
            Response("1234")), options);

        foreach (var kind in new[] { ProviderResponseKind.Discovery, ProviderResponseKind.Token, ProviderResponseKind.UserInfo })
        {
            var exception = await Assert.ThrowsAsync<ProviderHttpException>(() => client.GetAsync(new Uri("https://provider.example/content"), kind).AsTask());
            Assert.Equal(ProviderHttpFailure.ResponseTooLarge, exception.Failure);
        }
    }

    [Fact]
    public void TreatsConfiguredProxyAsAnExplicitApprovedEgressGateway()
    {
        var options = new ExternalAuthenticationOptions { ProviderEgress = new ProviderEgressOptions { ProxyUri = new Uri("http://proxy.internal:8080") } };
        var factory = new ProviderHttpClientFactory(OptionsFactory.Create(options), CreateValidator(options), new ValidatedOutboundConnectionFactory(CreateValidator(options), new RecordingConnector()));

        Assert.True(factory.UsesApprovedProxy);
    }

    [Fact]
    public async Task CollapsesTransportFailuresToSafeExceptionCategories()
    {
        var client = CreateClient(new ThrowingHandler());

        var exception = await Assert.ThrowsAsync<ProviderHttpException>(() => client.GetAsync(new Uri("https://provider.example/metadata"), ProviderResponseKind.Discovery).AsTask());

        Assert.Equal(ProviderHttpFailure.TransportFailure, exception.Failure);
        Assert.DoesNotContain("super-secret-response", exception.Message, StringComparison.Ordinal);
        Assert.Null(exception.InnerException);
    }

    private static ProviderHttpClient CreateClient(HttpMessageHandler handler, ExternalAuthenticationOptions? options = null)
    {
        options ??= new ExternalAuthenticationOptions();
        return new ProviderHttpClient(new HttpMessageInvoker(handler), CreateValidator(options), OptionsFactory.Create(options));
    }

    private static OutboundDestinationValidator CreateValidator(params string[] addresses) => CreateValidator(new ExternalAuthenticationOptions(), addresses);

    private static OutboundDestinationValidator CreateValidator(ExternalAuthenticationOptions options, params string[] addresses) => new(OptionsFactory.Create(options), new StaticDnsResolver((addresses.Length == 0 ? ["8.8.8.8"] : addresses).Select(IPAddress.Parse).ToArray()));

    private static HttpResponseMessage Response(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body) };

    private sealed class StaticDnsResolver(IReadOnlyCollection<IPAddress> addresses) : IOutboundDnsResolver
    {
        public ValueTask<IReadOnlyCollection<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken) => ValueTask.FromResult(addresses);
    }

    private sealed class SequencedDnsResolver(params IReadOnlyCollection<IPAddress>[] addresses) : IOutboundDnsResolver
    {
        private int index;

        public ValueTask<IReadOnlyCollection<IPAddress>> ResolveAsync(string host, CancellationToken cancellationToken) => ValueTask.FromResult(addresses[Math.Min(Interlocked.Increment(ref index) - 1, addresses.Length - 1)]);
    }

    private sealed class RecordingConnector : IValidatedAddressConnector
    {
        public ICollection<IPAddress> Addresses { get; } = new List<IPAddress>();

        public ValueTask<Stream> ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken)
        {
            Addresses.Add(address);
            return ValueTask.FromResult<Stream>(new MemoryStream());
        }
    }

    private sealed class SequenceHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses = new(responses);
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(responses.Dequeue());
        }
    }

    private sealed class DelayingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw new HttpRequestException("super-secret-response internal details");
    }
}
