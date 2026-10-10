using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Elsa.IO.Http.Services.Strategies;
using Elsa.IO.Http.ShellFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

var services = new ServiceCollection();
new HttpIOShellFeature().ConfigureServices(services);
using var provider = services.BuildServiceProvider();
if (provider.GetService<IHttpClientFactory>() is null)
{
    throw new InvalidOperationException("HTTP shell feature did not register IHttpClientFactory.");
}

using var handler = new FakeHandler();
using var client = new HttpClient(handler);
var strategy = new UrlContentStrategy(NullLogger<UrlContentStrategy>.Instance, new FakeFactory(client));
if (!strategy.CanResolve("https://example.invalid/input.txt") || !strategy.CanResolve("http://example.invalid/input.txt") ||
    strategy.CanResolve("file:///input.txt") || strategy.CanResolve(new Uri("https://example.invalid/input.txt")))
{
    throw new InvalidOperationException("URL content scheme/type contract failed.");
}
var content = await strategy.ResolveAsync("https://example.invalid/input.txt");
using var stream = content.Stream;
using var reader = new StreamReader(stream);
if (await reader.ReadToEndAsync() != "archived-http-content" || content.Name != "input.txt" ||
    content.ContentType != "text/plain" || handler.Calls != 1)
{
    throw new InvalidOperationException("URL binary content contract failed.");
}

Console.WriteLine("SELECTED_CONSUMER_PROOF=" + JsonSerializer.Serialize(new
{
    httpFactoryRegistered = true,
    urlContentResolved = true,
    networkRequests = 0,
    loadedAssemblies = SelectedAssemblyProof.Snapshot()
}));

sealed class FakeFactory(HttpClient client) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => client;
}

sealed class FakeHandler : HttpMessageHandler
{
    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != new Uri("https://example.invalid/input.txt"))
        {
            throw new InvalidOperationException("Unexpected HTTP request.");
        }
        Calls++;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("archived-http-content", Encoding.UTF8, "text/plain")
        };
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "input.txt" };
        return Task.FromResult(response);
    }
}
