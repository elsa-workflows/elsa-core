namespace Elsa.Activities.UnitTests.Helpers;

/// <summary>
/// Custom test HTTP message handler that allows full control over HTTP responses for testing.
/// Use this class to simulate different HTTP scenarios without making actual network calls.
/// </summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsyncFunc;

    /// <summary>
    /// Initializes a new instance of TestHttpMessageHandler.
    /// </summary>
    /// <param name="sendAsyncFunc">Function that defines how to handle HTTP requests and generate responses</param>
    public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsyncFunc)
    {
        _sendAsyncFunc = sendAsyncFunc;
    }

    /// <summary>
    /// Handles HTTP requests using the provided function.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _sendAsyncFunc(request, cancellationToken);
    }
}
