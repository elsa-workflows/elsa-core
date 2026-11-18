using System.Net;
using System.Reflection;
using Elsa.Http.DownloadableContentHandlers;
using Elsa.Http.Options;
using Elsa.Http.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Http;

public class RemoteFileDownloadTests
{
    [Fact]
    public void LinkTextRemainsTextAndOnlyExplicitUriIsDownloadable()
    {
        var handler = new UrlDownloadableContentHandler(Substitute.For<Elsa.Http.IFileDownloader>(), new FileExtensionContentTypeProvider());

        Assert.False(handler.GetSupportsContent("https://files.example/content"));
        Assert.True(handler.GetSupportsContent(new Uri("https://files.example/content")));
    }

    [Fact]
    public async Task UnapprovedDestinationDoesNotReachTransport()
    {
        using var transport = new RecordingHandler(HttpStatusCode.OK);
        using var client = new HttpClient(transport);
        var downloader = CreateDownloader(client, "files.example");

        await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync(new Uri("https://elsewhere.example/content")));

        Assert.Empty(transport.Requested);
    }

    [Fact]
    public async Task AllowedDestinationReachesTransportButRedirectIsRefused()
    {
        using var transport = new RecordingHandler(HttpStatusCode.Redirect);
        using var client = new HttpClient(transport);
        var downloader = CreateDownloader(client, "files.example");
        var destination = new Uri("https://files.example/content");

        await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync(destination));

        Assert.Equal(destination, Assert.Single(transport.Requested));
    }

    [Fact]
    public async Task DirectTransportRejectsLocalResolutionBeforeOpeningConnection()
    {
        var createHandler = typeof(HttpClientFileDownloader).GetMethod("CreateHandler", BindingFlags.Static | BindingFlags.NonPublic)!;
        using var handler = (SocketsHttpHandler)createHandler.Invoke(null, null)!;
        using var client = new HttpClient(handler);
        var downloader = CreateDownloader(client, "localhost");

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => downloader.DownloadAsync(new Uri("https://localhost/file")));

        Assert.IsType<InvalidOperationException>(error.InnerException);
        Assert.Equal("The remote download destination is not allowed.", error.InnerException!.Message);
    }

    private static HttpClientFileDownloader CreateDownloader(HttpClient client, params string[] hosts) =>
        new(client, Microsoft.Extensions.Options.Options.Create(new HttpActivityOptions { RemoteDownloadHosts = hosts }));

    private sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public List<Uri> Requested { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requested.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Headers = { Location = new Uri("https://elsewhere.example/destination") }
            });
        }
    }
}
