namespace Elsa.Activities.UnitTests.Http.Helpers;

/// <summary>
/// Custom test HTTP message handler that allows full control over HTTP responses for testing.
/// Use this class to simulate different HTTP scenarios without making actual network calls.
/// </summary>
/// <param name="sendAsyncFunc">Function that defines how to handle HTTP requests and generate responses</param>
public class TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsyncFunc) : HttpMessageHandler
{
    /// <summary>
    /// Handles HTTP requests using the provided function.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return sendAsyncFunc(request, cancellationToken);
    }
}
