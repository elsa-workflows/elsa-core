using System.Net;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Http.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.UnitTests;

public class HttpClientResponseBodyLimitsTests
{
    [Theory]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    public async Task OutboundClientsRejectBodiesOverTheLimit(bool fileDownloader, bool hasKnownLength, bool useShell)
    {
        await using var services = CreateServices(new SmallContent(5, hasKnownLength), useShell);

        await Assert.ThrowsAsync<HttpRequestException>(() => SendAsync(services, fileDownloader));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutboundClientsExposeACompleteBodyAtTheLimit(bool fileDownloader)
    {
        await using var services = CreateServices(new SmallContent(4, hasKnownLength: false));

        using var response = await SendAsync(services, fileDownloader);

        Assert.Equal(4, (await response.Content.ReadAsByteArrayAsync()).Length);
    }

    [Fact]
    public async Task CancellationStillInterruptsBufferingAnOutboundBody()
    {
        var content = new PendingContent();
        await using var services = CreateServices(content);
        using var cancellation = new CancellationTokenSource();
        using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(SendHttpRequestBase));
        var responseTask = client.GetAsync("https://example.test/file", cancellation.Token);
        await content.Buffering.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask);
    }

    private static ServiceProvider CreateServices(HttpContent content, bool useShell = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => new ResponseHandler(content)));
        if (useShell)
        {
            new ShellFeatures.HttpFeature { HttpActivityOptions = new() { MaxResponseBodySize = 4 } }.ConfigureServices(services);
        }
        else
        {
            var module = services.CreateModule();
            module.UseHttp(feature => feature.ConfigureHttpOptions = options => options.MaxResponseBodySize = 4);
            module.Apply();
        }
        return services.BuildServiceProvider();
    }

    private static async Task<HttpResponseMessage> SendAsync(IServiceProvider services, bool fileDownloader)
    {
        var uri = new Uri("https://example.test/file");
        if (fileDownloader)
            return await services.GetRequiredService<IFileDownloader>().DownloadAsync(uri);

        using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(SendHttpRequestBase));
        return await client.GetAsync(uri);
    }

    private sealed class ResponseHandler(HttpContent content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
    }

    private sealed class PendingContent : HttpContent
    {
        private readonly TaskCompletionSource _buffering = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Buffering => _buffering.Task;

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            _buffering.SetResult();
            return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class SmallContent(int size, bool hasKnownLength) : HttpContent
    {
        protected override bool TryComputeLength(out long length)
        {
            length = size;
            return hasKnownLength;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            stream.WriteAsync(new byte[size]).AsTask();
    }
}
