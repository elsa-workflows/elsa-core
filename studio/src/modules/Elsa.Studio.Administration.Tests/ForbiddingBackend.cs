using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Elsa.Studio.Contracts;
using Refit;

namespace Elsa.Studio.Administration.Tests;

/// <summary>
/// Real Refit clients over a transport that answers like core does for a caller without the endpoint's permission:
/// 403 with an empty body. With <paramref name="readsSucceed"/> only writes are refused, and reads see an empty list.
/// </summary>
internal sealed class ForbiddingBackend(bool readsSucceed = false) : IBackendApiClientProvider
{
    public Uri Url => new("https://elsa.example.test/");

    public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class =>
        new(RestService.For<T>(new HttpClient(new ForbiddingHandler(readsSucceed)) { BaseAddress = Url }));

    private sealed class ForbiddingHandler(bool readsSucceed) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(readsSucceed && request.Method == HttpMethod.Get
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"items":[],"count":0}""", Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.Forbidden) { RequestMessage = request });
    }
}
