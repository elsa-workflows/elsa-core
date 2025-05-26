using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Elsa.Resilience.IntegrationTests;

public class SequentialStatusHandler(IEnumerable<HttpStatusCode> codes) : HttpMessageHandler
{
    private readonly Queue<HttpStatusCode> _codes = new(codes);
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var code = _codes.Count > 0 ? _codes.Dequeue() : _codes.Last();
        var response = new HttpResponseMessage(code)
        {
            Content = new StringContent(string.Empty)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        return Task.FromResult(response);
    }
}
